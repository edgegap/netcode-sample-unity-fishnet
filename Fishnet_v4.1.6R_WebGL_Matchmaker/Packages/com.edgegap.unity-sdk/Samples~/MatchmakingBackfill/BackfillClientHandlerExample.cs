using System.Collections.Generic;
using Edgegap;
using Edgegap.Matchmaking;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using L = Edgegap.Logger;
using MyGroupUpRequestDTO = Edgegap.Matchmaking.BackfillGroupUpRequestDTO;
using MyTicketsAttributes = Edgegap.Matchmaking.BackfillTicketAttributesDTO;

// todo replace BackfillTicketAttributesDTO with CustomTicketsAttributes
// todo replace BackfillGroupUpRequestDTO with CustomGroupUpRequestDTO

public class BackfillClientHandlerExample : MonoBehaviour
{
    public static BackfillClientHandlerExample Instance { get; private set; }

    #region Matchmaking Configuration

    [Header("Matchmaker Instance")]
    public string BaseUrl;
    public string AuthToken;
    public string[] BackfillGroupSize = { "new", "1" };

    [Header("Exponential Retry")]
    public int RequestTimeoutSeconds = 3;
    public float PollingBackoffSeconds = 1f;
    public int MaxConsecutivePollingErrors = 10;

    [Header("Expiration and Cleanup")]
    public float RemoveAssignmentSeconds = 30f;
    public bool DeleteGroupOnPause = false;
    public bool DeleteGroupOnQuit = true;

    [Header("Logging")]
    public bool LogGroupUpdates = true;
    public bool LogPollingUpdates = false;
    #endregion

    public GroupClient<
            MyGroupUpRequestDTO,
            MyTicketsAttributes
        > MatchmakingClient;

    private string TicketID;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (Application.isBatchMode)
        {
            L.Log("MM ClientHandler | Destroying self in server environment.");
            Destroy(this.gameObject);
        }
        else
        {
            MatchmakingClient = new GroupClient<
                MyGroupUpRequestDTO,
                MyTicketsAttributes
            >(
                this,
                BaseUrl,
                AuthToken,
                RequestTimeoutSeconds,
                PollingBackoffSeconds,
                MaxConsecutivePollingErrors,
                RemoveAssignmentSeconds,
                LogGroupUpdates,
                LogPollingUpdates
            );

            MatchmakingClient.Initialize(
                (
                    Observable<MonitorResponseDTO> monitor,
                    ObservableActionType action,
                    string message
                ) =>
                {
                    if (action == ObservableActionType.Update)
                    {
                        if (message == "healthy")
                        {
                            // todo update UI

                            MatchmakingClient.Beacons(
                                (BeaconsResponseDTO beacons) =>
                                {
                                    Debug.Log($"beacons: {beacons}");

                                    MatchmakingClient.MeasureBeaconsRoundTripTime(
                                        beacons.Beacons,
                                        (Dictionary<string, float> pings) =>
                                        {
                                            StartMatchmaking(pings, true);
                                        }
                                    );
                                },
                                (string error, UnityWebRequest request) =>
                                {
                                    // todo handle beacon downtime, create tickets without beacons?
                                    L.Log($"beacon error: {request}");
                                }
                            );
                        }
                        else if (message != "healthy")
                        {
                            // todo handle outage/maintenance
                            L.Error($"Matchmaking error.\n{monitor.Current}");
                            MatchmakingClient.StopMatchmaking();
                        }
                    }
                },
                (
                    Observable<GroupUpResponseDTO> group,
                    ObservableActionType action,
                    string message
                ) =>
                {
                    if (
                        action == ObservableActionType.Update
                        && (
                            message.Contains("created")
                            || message.Contains("joined")
                            || message.Contains("updated")
                            || message.Contains("abandon")
                        )
                    )
                    {
                        // todo update UI
                    }

                    if (
                        action == ObservableActionType.Update
                        && message.Contains("updated")
                        && group.Current.Status == "HOST_ASSIGNED"
                    )
                    {
                        // todo join game on pre-defined game port & send ticketID to server during connection
                        TicketID = group.Current.TicketID;
                        L.Log($"joining game: {group.Current.Assignment.Ports["gameport"].Link}");
                    }
                }
            );
        }
    }

    public void OnApplicationPause(bool pause)
    {
        if (!DeleteGroupOnPause || MatchmakingClient.Group.Current is null)
            return;
        StopMatchmaking();
    }

    public void OnApplicationQuit()
    {
        if (!DeleteGroupOnQuit)
            return;
        StopMatchmaking();
    }

    public void StartMatchmaking(Dictionary<string, float> pings, bool isReady)
    {
        MatchmakingClient.CreateGroup(new MyGroupUpRequestDTO(pings, BackfillGroupSize, isReady), true);
    }

    public void StopMatchmaking()
    {
        if (enabled)
        {
            MatchmakingClient.StopMatchmaking();
        }
    }
}
