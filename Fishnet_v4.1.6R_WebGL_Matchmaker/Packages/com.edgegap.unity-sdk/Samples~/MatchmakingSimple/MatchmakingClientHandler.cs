using Edgegap;
using Edgegap.Matchmaking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using MyGroupUpRequestDTO = Edgegap.Matchmaking.SimpleGroupUpRequestDTO;
using MyTicketsAttributes = Edgegap.Matchmaking.LatenciesAttributesDTO;

// todo replace LatenciesAttributesDTO with CustomTicketsAttributes
// todo replace SimpleGroupUpRequestDTO with CustomGroupUpRequestDTO

public class MatchmakingClientHandler : MonoBehaviour
{
    public static MatchmakingClientHandler Instance { get; private set; }

    [Header("Matchmaker Instance")]
    public string BaseUrl;
    public string AuthToken;

    [Header("Exponential Retry")]
    public int RequestTimeoutSeconds = 3;
    public float PollingBackoffSeconds = 1f;
    public int MaxConsecutivePollingErrors = 10;

    [Header("Automatic Cleanup")]
    public float RemoveAssignmentSeconds = 30f;
    public bool DeleteTicketOnPause = false;
    public bool DeleteTicketOnQuit = true;

    [Header("Logging")]
    public bool LogAssignmentUpdates = true;
    public bool LogPollingUpdates = false;

    public GroupClient<
            MyGroupUpRequestDTO,
            MyTicketsAttributes
        > MatchmakingClient;

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
        // configure Matchmaking
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
            LogAssignmentUpdates,
            LogPollingUpdates
        );

        // initialize Matchmaking
        MatchmakingClient.Initialize(
            // handle service monitoring
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
                                Debug.Log($"beacon error: {request}");
                            }
                        );
                    }
                    else if (message != "healthy")
                    {
                        // todo handle outage/maintenance
                        Debug.LogError($"Matchmaking error.\n{monitor.Current}");
                        MatchmakingClient.StopMatchmaking();
                    }
                }
            },
            // handle group assignment
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
                    // todo join game on pre-defined game port
                    Debug.Log(
                        $"joining game: {group.Current.Assignment.Ports["gameport"].Link}"
                    );
                }
            }
        );
    }

    public void OnApplicationPause(bool pause)
    {
        if (!DeleteTicketOnPause || MatchmakingClient.Group.Current is null)
            return;
        StopMatchmaking();
    }

    public void OnApplicationQuit()
    {
        if (!DeleteTicketOnQuit)
            return;
        StopMatchmaking();
    }

    public void StartMatchmaking(Dictionary<string, float> pings, bool isReady)
    {
        MatchmakingClient.CreateGroup(new MyGroupUpRequestDTO(pings, isReady));
    }

    public void StartMatchmaking(Dictionary<string, float> pings, bool isReady, string groupID)
    {
        MatchmakingClient.JoinGroup(new MyGroupUpRequestDTO(pings, isReady), groupID);
    }

    public void StopMatchmaking()
    {
        if (enabled)
        {
            MatchmakingClient.StopMatchmaking();
        }
    }
}
