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
                    ClientAgent.ReserveSeats(
                        "on-demand", // todo replace with dynamic policy name
                        new List<string> { "player1" } // todo replace with dynamic player IDs
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
                    SelectedInstance = instances.Current.ServerInstances[0];
                    L.Log(
                        $"SB ClientHandler | Joining game: {SelectedInstance.Server.Ports["gameport"].Link}"
                    );
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
