using System.Collections.Generic;
using Edgegap;
using Edgegap.ServerBrowser;
using UnityEngine;
using L = Edgegap.Logger;
using MyInstanceMetadata = Edgegap.ServerBrowser.SimpleInstanceMetadataDTO;
using MySlotMetadata = Edgegap.ServerBrowser.SimpleSlotMetadataDTO;

// todo replace SimpleInstanceMetadataDTO with custom class
// todo replace SimpleSlotMetadataDTO with custom class

public class ServerBrowserClientHandler : MonoBehaviour
{
    [Header("Server Browser Instance")]
    public string BaseUrl;
    public string ClientToken;

    [Header("Exponential Retry")]
    public int RequestTimeoutSeconds = 3;

    public static ServerBrowserClientHandler Instance { get; private set; }
    private ClientAgent<MyInstanceMetadata, MySlotMetadata> ClientAgent;

    private InstanceDTO<MyInstanceMetadata, MySlotMetadata> SelectedInstance;

    public void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void Start()
    {
        if (Application.isBatchMode)
        {
            L.Log("SB ClientHandler | Destroying self in server environment.");
            Destroy(this.gameObject);
        }

        ClientAgent = new ClientAgent<MyInstanceMetadata, MySlotMetadata>(
            this,
            BaseUrl,
            ClientToken,
            RequestTimeoutSeconds
        );
        ClientAgent.Initialize(
            (Observable<MonitorResponseDTO> monitor, ObservableActionType action, string message) =>
            {
                if (action == ObservableActionType.Update && message == "healthy")
                {
                    // todo move ListInstances to a UI trigger
                    ClientAgent.ListInstances(
                        new FilterCompiler(
                            new List<FilterBase>()
                            {
                                new IntFilter()
                                {
                                    Field = "total_joinable_seats",
                                    Operator = IntOperator._GreaterThanOrEqualTo,
                                    Value = 1,
                                },
                            }
                        ),
                        new SortCompiler(
                            new List<SortBase>()
                            {
                                new SimpleSort("total_joinable_seats", SortDirection._Descending),
                            }
                        )
                    );
                }
                else if (action == ObservableActionType.Error || message == "unhealthy")
                {
                    // todo handle outage/maintenance
                    L.Log($"SB ClientHandler | Service is unhealthy.\n{monitor.Current}");
                }
            },
            (
                Observable<InstanceListResponseDTO<MyInstanceMetadata, MySlotMetadata>> instances,
                ObservableActionType action,
                string message
            ) =>
            {
                if (action == ObservableActionType.Log && message == "seats reserved")
                {
                    // todo join game on pre-defined game port
                    L.Log(
                        $"SB ClientHandler | Joining game: {SelectedInstance.Server.Ports["gameport"].Link}"
                    );
                }
                else if (action == ObservableActionType.Update)
                {
                    if (message.Contains("instance list") || message.Contains("cache deleted"))
                    {
                        // todo store joinable instances in a list for UI selection & update UI
                        int joinableInstances = instances.Current.ServerInstances.Count;
                        L.Log(
                            $"SB ClientHandler | Found total '{joinableInstances}' instances with capacity."
                        );

                        if (joinableInstances == 0)
                        {
                            L.Log("SB ClientHandler | No instances with capacity found.");
                            return;
                        }

                        // picks first instance - alternatively select instance with a UI trigger (player choice)
                        SelectedInstance = instances.Current.ServerInstances[0];
                        L.Log(
                            $"SB ClientHandler | Retrieving instance details for '{SelectedInstance.RequestID}'"
                        );
                        ClientAgent.GetInstanceDetails(SelectedInstance.RequestID);
                    }
                    else if (message == "instance details retrieved")
                    {
                        // picks first joinable slot - alternatively let user choose a slot from the instance

                        // update selected instance with the latest details
                        SelectedInstance = instances.Current.ServerInstances.Find(instance =>
                            instance.RequestID == SelectedInstance.RequestID
                        );
                        L.Log(
                            $"SB ClientHandler | Instance details retrieved for '{SelectedInstance.RequestID}'"
                        );
                        string slotName = SelectedInstance
                            .Slots.Find(slot => slot.JoinableSeats >= 1)
                            .Name;
                        L.Log(
                            $"SB ClientHandler | Attempting seat reservation for instance '{SelectedInstance.RequestID}'"
                        );
                        ClientAgent.ReserveSeats(
                            SelectedInstance.RequestID,
                            slotName,
                            new List<string> { "player1" } // todo replace with dynamic player IDs
                        );
                    }
                }
                else if (action == ObservableActionType.Error)
                {
                    // todo convey errors through a UI notification
                    L.Error($"SB ClientHandler | Unexpected error.");
                }
            }
        );
    }
}
