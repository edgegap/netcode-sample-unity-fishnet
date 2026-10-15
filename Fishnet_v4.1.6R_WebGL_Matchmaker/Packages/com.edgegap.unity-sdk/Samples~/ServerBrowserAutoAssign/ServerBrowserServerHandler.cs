using System;
using System.Collections;
using System.Collections.Generic;
using Edgegap;
using Edgegap.ServerBrowser;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using L = Edgegap.Logger;
using MyInstanceMetadata = Edgegap.ServerBrowser.SimpleInstanceMetadataDTO;
using MySlotMetadata = Edgegap.ServerBrowser.SimpleSlotMetadataDTO;

// todo replace SimpleInstanceMetadataDTO with custom class
// todo replace SimpleSlotMetadataDTO with custom class
public class ServerBrowserServerHandler : MonoBehaviour
{
    [Header("Server Browser Instance")]
    public string BaseUrl;
    public string ServerToken;

    [Header("Updates")]
    public bool AcceptExpiredReservations = false;

    [EnumButtons]
    public UpdateMode UpdateMode = UpdateMode.Heartbeat;

    [Header("Heartbeat")]
    public int RequestTimeoutSeconds = 10;
    public float HeartbeatIntervalSeconds = 10f;
    public int HeartbeatMaxConsecutiveErrors = 10;

    [Header("Environment")]
    public bool MockEnv = false;
    public DeploymentEnvironmentDTO DeploymentEnv { get; private set; }
    public string PolicyName { get; private set; }

    public static ServerBrowserServerHandler Instance { get; private set; }
    private ServerAgent<MyInstanceMetadata, MySlotMetadata> ServerAgent;
    private SafeHttpRequest Request;

    public void Awake()
    {
        // if there is an instance, and it's not me, delete myself.
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
        DontDestroyOnLoad(this.gameObject);

        Request = new SafeHttpRequest(this);
        IDictionary env = Environment.GetEnvironmentVariables();

#if UNITY_EDITOR
        MockEnv = true;
#endif

        #region mock data
        MockEnv = MockEnv || !string.IsNullOrEmpty(env["ARBITRIUM_MOCK_ENV"]?.ToString());
        if (MockEnv)
        {
            // define mock env variables here
            env["ARBITRIUM_REQUEST_ID"] = "editor";
            env["ARBITRIUM_PUBLIC_IP"] = "172.236.117.196";
            env["ARBITRIUM_DEPLOYMENT_TAGS"] = "tag1,tag2";
            env["ARBITRIUM_HOST_BASE_CLOCK_FREQUENCY"] = "2000";
            env["ARBITRIUM_DEPLOYMENT_VCPU_UNITS"] = "1536";
            env["ARBITRIUM_DEPLOYMENT_MEMORY_MB"] = "3072";
            env["ARBITRIUM_DEPLOYMENT_LOCATION"] =
                "{\"city\":\"Chicago\",\"country\":\"United States of America\",\"continent\":\"North America\",\"administrative_division\":\"Illinois\",\"timezone\":\"Central Time\"}";
            env["ARBITRIUM_PORTS_MAPPING"] =
                "{\"ports\":{\"gameport\":{\"name\":\"GamePort\",\"internal\":7777,\"external\":31504,\"protocol\":\"UDP\"}}}";
        }
        #endregion

        DeploymentEnv = new DeploymentEnvironmentDTO(env);
        PolicyName = env["SB_SCALING_POLICY_NAME"]?.ToString() ?? "on-demand";
        BaseUrl ??= env["SB_BASE_URL"]?.ToString();
        ServerToken ??= env["SB_SERVER_TOKEN"]?.ToString();

        if (!MockEnv && !Application.isBatchMode)
        {
            L.Log("SB ServerHandler | Destroying self in client environment.");
            Destroy(this.gameObject);
        }

        ServerAgent = new ServerAgent<MyInstanceMetadata, MySlotMetadata>(
            this,
            BaseUrl,
            ServerToken,
            AcceptExpiredReservations,
            UpdateMode,
            RequestTimeoutSeconds,
            HeartbeatIntervalSeconds,
            HeartbeatMaxConsecutiveErrors
        );

        ServerAgent.Initialize(
            (Observable<MonitorResponseDTO> monitor, ObservableActionType action, string message) =>
            {
                if (action == ObservableActionType.Update && message == "healthy")
                {
                    ServerAgent.DiscoverInstance(
                        new InstanceDTO<MyInstanceMetadata, MySlotMetadata>()
                        {
                            RequestID = DeploymentEnv.RequestID,
                            Metadata = new MyInstanceMetadata()
                            {
                                PolicyName = PolicyName,
                                Name = DeploymentEnv.RequestID,
                            },
                            Server = new DeploymentDTO()
                            {
                                Fqdn = DeploymentEnv.Fqdn,
                                PublicIP = DeploymentEnv.PublicIP,
                                Ports = DeploymentEnv.PortMapping,
                                Location = DeploymentEnv.Location,
                            },
                            Slots = new List<SlotDTO<MySlotMetadata>>
                            {
                                new SlotDTO<MySlotMetadata>()
                                {
                                    Name = "main",
                                    AvailableSeats = 10,
                                },
                            },
                        }
                    );
                }
                else if (action == ObservableActionType.Error || message == "unhealthy")
                {
                    // todo handle outage/maintenance
                    L.Log($"SB ServerHandler | Service is unhealthy.\n{monitor.Current}");
                    // SelfStopDeployment(); // optionally self-stop deployment if not discoverable
                }
            },
            (
                Observable<InstanceDTO<MyInstanceMetadata, MySlotMetadata>> instance,
                ObservableActionType action,
                string message
            ) =>
            {
                if (action == ObservableActionType.Error && message.Contains("discovery failed"))
                {
                    ServerAgent.DeleteInstance();
                }

                if (message.Contains("instance delete"))
                {
                    SelfStopDeployment();
                }
            },
            (
                Observable<ConfirmReservationsResponseDTO> confirmations,
                ObservableActionType action,
                string message
            ) =>
            {
                if (action == ObservableActionType.Log && message.StartsWith("enqueued"))
                {
                    // todo optionally authenticate against third party
                }

                if (action == ObservableActionType.Update && message == "confirmed")
                {
                    // todo handle accepted, expired, and unknown connections
                }
            }
        );

        L.Log(
            $"SB ServerHandler | Started successfully for deployment '{DeploymentEnv.RequestID}'."
        );
    }

    public void Update()
    {
        ServerAgent.UpdateMode = UpdateMode;
        ServerAgent.AcceptExpiredReservations = AcceptExpiredReservations;
    }

    public void OnPlayerJoined(string playerID)
    {
        ServerAgent.ConfirmReservation(playerID);
    }

    public void OnPlayerAbandoned(string slotName)
    {
        ServerAgent.UpdateSlot(
            new SlotUpdateDTO<MySlotMetadata>(
                slotName,
                1,
                ServerAgent.Instance.Current.Slots.Find(s => s.Name == slotName).Metadata
            )
        );
    }

    public void OnApplicationQuit()
    {
        if (enabled)
        {
            ServerAgent.DeleteInstance();
        }
    }

    public void SelfStopDeployment()
    {
        if (MockEnv)
        {
            L.Log("SB ServerHandler | Invoking Application.Quit() in mock environment.");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        if (
            string.IsNullOrEmpty(DeploymentEnv.SelfStopURL)
            || string.IsNullOrEmpty(DeploymentEnv.SelfStopToken)
        )
        {
            L.Error("SB ServerHandler | Self-Stop URL or Token not set, unable to self-stop.");
            return;
        }

        Request.Delete(
            DeploymentEnv.SelfStopURL,
            DeploymentEnv.SelfStopToken,
            (string response, UnityWebRequest request) =>
            {
                L.Log($"SB ServerHandler | Successfully called Self-Stop API.\n{response}");
            },
            (string error, UnityWebRequest request) =>
            {
                L.Error($"SB ServerHandler | Couldn't reach Self-Stop API.\n{error}");
            },
            new RetryParameters { MaxAttempts = 10 }
        );
    }
}
