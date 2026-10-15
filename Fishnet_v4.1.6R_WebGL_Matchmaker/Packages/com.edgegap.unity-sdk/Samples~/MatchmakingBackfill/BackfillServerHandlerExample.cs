using System;
using System.Collections;
using System.Collections.Generic;
using Edgegap;
using Edgegap.Matchmaking;
using UnityEngine;
using UnityEngine.Networking;
using L = Edgegap.Logger;
using MyTicketsAttributes = Edgegap.Matchmaking.BackfillTicketAttributesDTO;

// todo replace BackfillTicketAttributesDTO with CustomTicketsAttributes

public class BackfillServerHandlerExample : MonoBehaviour
{
    public static BackfillServerHandlerExample Instance { get; private set; }

    #region Matchmaking Configuration

    [Header("Matchmaker Instance")]
    public string BaseUrl;
    public string AuthToken;
    public int TargetPlayerCount = -1;

    [Header("Exponential Retry")]
    public int RequestTimeoutSeconds = 3;
    public float PollingBackoffSeconds = 1f;

    [Header("Expiration and Grace Period")]
    public int MaxConsecutivePollingErrors = 10;
    public float ConnectionGracePeriodSeconds = 60f;
    public float AdmissionGracePeriodSeconds = -1f;

    [Header("Logging")]
    public bool LogBackfillUpdates = true;
    public bool LogPollingUpdates = false;
    #endregion

    public ServerAgent<MyTicketsAttributes> MatchmakingServer;

    [Header("Environment")]
    public bool MockEnv = false;
    public DeploymentEnvironmentDTO DeploymentEnvs { get; private set; }

    public MatchEnvironmentDTO<MyTicketsAttributes> MatchEnvs { get; private set; }

    private BackfillAttributes BackfillAttributes;
    private SafeHttpRequest Request;

    struct BackfillRunningStruct
    {
        private bool _value;
        public bool Value
        {
            get { return _value; }
            set
            {
                _value = value;

                if (value)
                {
                    Instance.StartCoroutine(Instance.OnEnableBackfillRoutine());
                }
            }
        }
    }

    private BackfillRunningStruct BackfillRunning;

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
        BackfillRunning.Value = false;
        IDictionary envs = Environment.GetEnvironmentVariables();

        #region mock data

#if UNITY_EDITOR
        MockEnv = true;
#endif
        MockEnv = MockEnv || !string.IsNullOrEmpty(envs["ARBITRIUM_MOCK_ENV"]?.ToString());

        if (MockEnv)
        {
            // define mock env variables here
            envs["MM_MATCH_PROFILE"] = "backfill-example";
            envs["MM_TICKET_IDS"] = "[\"cusfn10msflc73beiik0\",\"cusfn18msflc73beiil0\"]";
            envs["MM_TICKET_cusfn10msflc73beiik0"] =
                "{\"id\":\"cusfn10msflc73beiik0\",\"created_at\":\"2025-02-21T22:17:42.3886970Z\",\"player_ip\":\"174.93.233.25\",\"group_id\":\"b2080c27-19c9-4fb0-8fe7-4bf1e5d285d1\",\"team_id\":\"cusfn1gmsflc73beiim0\",\"attributes\":{\"beacons\":{\"Chicago\":12.3,\"LosAngeles\":145.6,\"Tokyo\":233.2},\"backfill_group_size\":[\"new\",\"1\"]}}";
            envs["MM_TICKET_cusfn18msflc73beiil0"] =
                "{\"id\":\"cusfn18msflc73beiil0\",\"created_at\":\"2025-02-21T22:17:42.2548390Z\",\"player_ip\":\"174.93.233.23\",\"group_id\":\"015d4dc8-6c79-4b5c-bbc6-f309b9787c8f\",\"team_id\":\"cusfn1gmsflc73beiim0\",\"attributes\":{\"beacons\":{\"Chicago\":87.3,\"LosAngeles\":32.4,\"Tokyo\":253.2},\"backfill_group_size\":[\"new\",\"1\"]}}";
            envs["MM_GROUPS"] =
                "{\"b2080c27-19c9-4fb0-8fe7-4bf1e5d285d1\":[\"cusfn10msflc73beiik0\"],\"015d4dc8-6c79-4b5c-bbc6-f309b9787c8f\":[\"cusfn18msflc73beiil0\"]}";
            envs["MM_TEAMS"] =
                "{\"cusfn1gmsflc73beiim0\":[\"b2080c27-19c9-4fb0-8fe7-4bf1e5d285d1\",\"015d4dc8-6c79-4b5c-bbc6-f309b9787c8f\"]}";

            envs["ARBITRIUM_REQUEST_ID"] = "editor";
            envs["ARBITRIUM_PUBLIC_IP"] = "localhost";
            envs["ARBITRIUM_DEPLOYMENT_TAGS"] = "tag1,tag2";
            envs["ARBITRIUM_HOST_BASE_CLOCK_FREQUENCY"] = "2000";
            envs["ARBITRIUM_DEPLOYMENT_VCPU_UNITS"] = "1536";
            envs["ARBITRIUM_DEPLOYMENT_MEMORY_MB"] = "3072";
            envs["ARBITRIUM_DEPLOYMENT_LOCATION"] =
                "{\"city\":\"Chicago\",\"country\":\"United States of America\",\"continent\":\"North America\",\"administrative_division\":\"Illinois\",\"timezone\":\"Central Time\"}";

            // todo edit external port value
            envs["ARBITRIUM_PORTS_MAPPING"] =
                "{\"ports\":{\"gameport\":{\"name\":\"GamePort\",\"internal\":7777,\"external\":31504,\"protocol\":\"UDP\"}}}";
        }
        #endregion

        DeploymentEnvs = new DeploymentEnvironmentDTO(envs);
        MatchEnvs = new MatchEnvironmentDTO<MyTicketsAttributes>(envs);
        BaseUrl ??= envs["MM_BASE_URL"]?.ToString();
        AuthToken ??= envs["MM_AUTH_TOKEN"]?.ToString();

        BackfillAssignment deployment = new BackfillAssignment()
        {
            RequestID = DeploymentEnvs.RequestID,
            Fqdn = DeploymentEnvs.Fqdn,
            PublicIP = DeploymentEnvs.PublicIP,
            Ports = DeploymentEnvs.PortMapping,
            Location = DeploymentEnvs.Location,
        };
        BackfillAttributes = new BackfillAttributes(deployment);

        Request = new SafeHttpRequest(this);

        if (!MockEnv && !Application.isBatchMode)
        {
            L.Log("MM ServerHandler | Destroying self in client environment.");
            Destroy(gameObject);
        }
        else
        {
            MatchmakingServer = new ServerAgent<MyTicketsAttributes>(
                this,
                BaseUrl,
                AuthToken,
                MatchEnvs.MatchProfile,
                BackfillAttributes,
                TargetPlayerCount,
                RequestTimeoutSeconds,
                PollingBackoffSeconds,
                MaxConsecutivePollingErrors,
                ConnectionGracePeriodSeconds,
                LogBackfillUpdates,
                LogPollingUpdates
            );

            MatchmakingServer.Initialize(
                MatchEnvs.Tickets,
                (
                    Observable<MonitorResponseDTO> monitor,
                    ObservableActionType action,
                    string message
                ) =>
                {
                    if (message == "healthy")
                    {
                        if (!BackfillRunning.Value)
                        {
                            BackfillRunning.Value = true;
                            MatchmakingServer.AddBackfills();
                        }
                    }
                    else
                    {
                        // todo handle outage/maintenance
                        L.Error($"Matchmaking error.\n{monitor.Current}");
                        StopBackfill();
                    }
                },
                (
                    Observable<
                        Dictionary<string, BackfillResponseDTO<MyTicketsAttributes>>
                    > backfills,
                    ObservableActionType action,
                    string message
                ) =>
                {
                    if (action == ObservableActionType.Update && message.Contains("created"))
                    {
                        // todo handling new backfill
                    }

                    if (action == ObservableActionType.Update && message.Contains("assigned"))
                    {
                        // todo handling assigned ticket to backfill
                    }

                    if (message.Contains("abandon"))
                    {
                        // todo handling abandoned backfill
                    }
                }
            );

            // todo listen for joining players & their ticketID => OnPlayerJoined
            // todo listen for leaving players => OnPlayerAbandoned

            L.Log(
                $"MM ServerHandler | Started successfully for deployment '{DeploymentEnvs.RequestID}'."
            );
        }
    }

    public void OnApplicationQuit()
    {
        if (!enabled)
            return;
        StopBackfill();
    }

    public void StopBackfill(Action onCompletedDelegate = null)
    {
        BackfillRunning.Value = false;
        MatchmakingServer.AbandonAllBackfills(onCompletedDelegate);
    }

    public void StopServer()
    {
        Request.Delete(
            DeploymentEnvs.SelfStopURL,
            DeploymentEnvs.SelfStopToken,
            (string response, UnityWebRequest request) =>
            {
                L.Log($"MM ServerHandler | Successfully called Self-Stop API.\n{response}");
            },
            (string error, UnityWebRequest request) =>
            {
                L.Error($"MM ServerHandler | Couldn't reach Self-Stop API.\n{error}");
            },
            new RetryParameters { MaxAttempts = 10 }
        );
    }

    public void OnPlayerJoined(string ticketID)
    {
        BackfillAssignedTicket<MyTicketsAttributes> ticket = MatchmakingServer.PlayerConnected(
            ticketID
        );

        if (ticket is null)
        {
            // todo can't be mapped to backfill => kick/ban/reject connection through netcode-specific methods
        }
    }

    public void OnPlayerAbandoned(string ticketID)
    {
        MatchmakingServer.AbandonPlayer(ticketID);
    }

    private IEnumerator OnEnableBackfillRoutine()
    {
        if (AdmissionGracePeriodSeconds <= 0)
        {
            yield break;
        }

        yield return new WaitForSecondsRealtime(AdmissionGracePeriodSeconds);

        StopBackfill(() => {
            // todo optionally AbandonPlayer all assignments to immediately end connection grace period
        });
    }
}
