// Edgegap matchmaking client (Group Up flow, solo group). Requires com.edgegap.unity-sdk 3.5.5+.
// Flow: Find Match -> ticket -> SEARCHING -> MATCH_FOUND (server starting) -> HOST_ASSIGNED -> connect.
// Netcode-specific code lives only in the "NETCODE ADAPTER" region at the bottom.
// Adapted from the end-to-end verified edgegap/mirror-webgl sample.
using System.Collections;
using System.Collections.Generic;
using Edgegap;
using Edgegap.Matchmaking;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Bayou;
using UnityEngine;
using UnityEngine.Networking;

[DisallowMultipleComponent]
public class EdgegapMatchmakingClient : MonoBehaviour
{
    [Header("Matchmaker (from the Edgegap dashboard)")]
    [Tooltip("Matchmaker API URL from the Edgegap dashboard (no trailing slash needed).")]
    public string BaseUrl;

    [Tooltip("Matchmaker Auth Token. Safe to ship in clients: it grants no Edgegap account access.")]
    public string AuthToken;

    [Tooltip("Profile name from your matchmaker configuration.")]
    public string Profile = "casual";

    [Header("Game Server Connection")]
    [Tooltip("Port name defined on your Edgegap app version. Must match exactly.")]
    public string PortName = "gameport";

    [Tooltip("WebSocket transports only: use wss:// (required with TLS Upgrade and on HTTPS pages).")]
    public bool UseSecureWebSocket = true;

    [Tooltip("The server may still be booting when the assignment arrives, so retry.")]
    public int ConnectionAttempts = 10;
    public float ConnectionRetryDelaySeconds = 2f;
    public float ConnectionAttemptTimeoutSeconds = 10f;

    [Header("Latency Beacons")]
    [Tooltip("Ping Edgegap beacons and send results in tickets. Needs a `latencies` rule in the profile. Forced off in WebGL.")]
    public bool UseLatencyBeacons = false;

    [Header("UI")]
    public bool ShowGUI = true;

    [Header("Logging")]
    public bool LogGroupUpdates = true;
    public bool LogPollingUpdates = false;

    public GroupClient<GameGroupUpRequestDTO, GameTicketAttributes> MatchmakingClient { get; private set; }
    public string Status { get; private set; } = "Matchmaker not initialized.";
    public bool IsMatchmakerHealthy { get; private set; }
    public bool IsSearching { get; private set; }
    public bool IsConnecting { get; private set; }

    /// <summary>Ticket ID of the current match. Send it to the server on connect if you validate players.</summary>
    public string TicketId { get; private set; }

    private Coroutine connectRoutine;

    private void Start()
    {
#if UNITY_SERVER
        bool isServerBuild = true;
#else
        bool isServerBuild = Application.isBatchMode;
#endif
        if (isServerBuild)
        {
            // the dedicated server never matchmakes
            enabled = false;
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        if (UseLatencyBeacons)
        {
            Debug.LogWarning("Edgegap MM | Latency beacons are not available in WebGL, disabling.");
            UseLatencyBeacons = false;
        }
#endif

        if (string.IsNullOrWhiteSpace(BaseUrl) || string.IsNullOrWhiteSpace(AuthToken))
        {
            Status = "Set matchmaker Base URL and Auth Token.";
            Debug.LogError($"Edgegap MM | {Status}");
            return;
        }

        MatchmakingClient = new GroupClient<GameGroupUpRequestDTO, GameTicketAttributes>(
            this,
            BaseUrl.Trim().TrimEnd('/'), // SDK builds "{BaseUrl}/path"
            AuthToken.Trim(), // sent raw as the Authorization header, no "Bearer"
            3,
            1f,
            10,
            30f,
            LogGroupUpdates,
            LogPollingUpdates
        );

        Status = "Checking matchmaker...";
        MatchmakingClient.Initialize(OnMonitorUpdate, OnGroupUpdate);
    }

    private void OnApplicationQuit()
    {
        // Best effort: browsers may close without calling this; unmatched tickets also expire on their own.
        if (IsSearching)
            MatchmakingClient?.StopMatchmaking();
    }

    public void FindMatch()
    {
        if (MatchmakingClient == null || !IsMatchmakerHealthy || IsSearching || IsConnecting)
            return;

        if (IsClientActive())
        {
            Debug.LogWarning("Edgegap MM | Already connected to a server, disconnect before matchmaking.");
            return;
        }

        IsSearching = true;

        if (!UseLatencyBeacons)
        {
            CreateTicket(null);
            return;
        }

        Status = "Measuring latency...";
        MatchmakingClient.Beacons(
            (BeaconsResponseDTO beacons) =>
                MatchmakingClient.MeasureBeaconsRoundTripTime(beacons.Beacons, CreateTicket),
            (string error, UnityWebRequest request) =>
            {
                IsSearching = false;
                Status = "Couldn't retrieve latency beacons.";
            }
        );
    }

    public void CancelMatchmaking()
    {
        if (MatchmakingClient == null || !IsSearching)
            return;

        Status = "Cancelling...";
        MatchmakingClient.StopMatchmaking(() =>
        {
            GroupUpResponseDTO current = MatchmakingClient.Group.Current;
            if (current?.GroupID != null)
            {
                // DELETE was rejected (409) because a match was already found. The SDK stopped
                // polling, so resume to receive the assignment instead of stranding the player.
                Status = "Match already found, joining...";
                MatchmakingClient.ResumeMatchmaking(current, abandon: true);
                return;
            }

            IsSearching = false;
            Status = "Matchmaking cancelled.";
        });
    }

    public void RetryMatchmakerStatus()
    {
        if (MatchmakingClient == null)
            return;
        Status = "Checking matchmaker...";
        MatchmakingClient.Status();
    }

    private void CreateTicket(Dictionary<string, float> beacons)
    {
        if (beacons != null)
        {
            // 0 ms means "unmeasured" (ping failed), not "closest": drop those values.
            var measured = new Dictionary<string, float>();
            foreach (var kv in beacons)
                if (kv.Value > 0f)
                    measured[kv.Key] = kv.Value;
            beacons = measured.Count > 0 ? measured : null;
        }

        Status = "Searching for a match...";

        // abandon: true is required: the SDK's Observable starts with an empty (non-null) group,
        // so the first CreateGroup without it fails with "conflict, abandon and restart".
        MatchmakingClient.CreateGroup(new GameGroupUpRequestDTO(Profile, beacons, isReady: true), abandon: true);
    }

    private void OnMonitorUpdate(Observable<MonitorResponseDTO> monitor, ObservableActionType action, string message)
    {
        if (action == ObservableActionType.Update && message == "healthy")
        {
            IsMatchmakerHealthy = true;
            Status = "Ready to matchmake.";
        }
        else if (action == ObservableActionType.Update || action == ObservableActionType.Error)
        {
            IsMatchmakerHealthy = false;
            Status = "Matchmaker unavailable.";
            if (IsSearching)
                MatchmakingClient.StopMatchmaking(() => IsSearching = false);
        }
    }

    private void OnGroupUpdate(Observable<GroupUpResponseDTO> group, ObservableActionType action, string message)
    {
        if (action == ObservableActionType.Error)
        {
            IsSearching = false;
            Status = "Matchmaking failed, try again.";
            return;
        }

        if (action != ObservableActionType.Update)
            return;

        GroupUpResponseDTO current = group.Current;

        // group abandoned, or removed 30 s after assignment
        if (current?.GroupID == null)
        {
            if (!IsConnecting && !IsClientActive())
                IsSearching = false;
            return;
        }

        switch (current.Status)
        {
            case "SEARCHING":
                Status = "Searching for players...";
                break;
            case "TEAM_FOUND":
                Status = "Team found...";
                break;
            case "MATCH_FOUND":
                Status = "Match found, starting server...";
                break;
            case "HOST_ASSIGNED":
                // Read the assignment now: the SDK clears the group 30 s later.
                IsSearching = false;
                TicketId = current.TicketID;
                ConnectToAssignment(current.Assignment);
                break;
            case "CANCELLED":
                IsSearching = false;
                Status = "No match found in time, try again.";
                break;
        }
    }

    private void ConnectToAssignment(DeploymentDTO assignment)
    {
        if (assignment?.Ports == null || !assignment.Ports.TryGetValue(PortName, out PortMappingDTO port))
        {
            Status = $"Assignment has no port named '{PortName}'.";
            Debug.LogError($"Edgegap MM | {Status} Check the port name on the app version.\n{assignment}");
            return;
        }

        if (!int.TryParse(port.External, out int externalPort))
        {
            Status = "Assignment has an invalid external port.";
            Debug.LogError($"Edgegap MM | {Status}\n{port}");
            return;
        }

        if (connectRoutine != null)
            StopCoroutine(connectRoutine);
        connectRoutine = StartCoroutine(ConnectWithRetry(assignment.Fqdn, assignment.PublicIP, (ushort)externalPort));
    }

    private IEnumerator ConnectWithRetry(string fqdn, string publicIp, ushort port)
    {
        IsConnecting = true;

        for (int attempt = 1; attempt <= ConnectionAttempts; attempt++)
        {
            Status = $"Joining server (attempt {attempt}/{ConnectionAttempts})...";
            Debug.Log($"Edgegap MM | Connecting to {fqdn} ({publicIp}) port {port}, attempt {attempt}.");

            StartClientConnection(fqdn, publicIp, port);

            float started = Time.realtimeSinceStartup;
            yield return new WaitUntil(() =>
                !IsClientConnecting() || Time.realtimeSinceStartup - started > ConnectionAttemptTimeoutSeconds
            );

            if (IsClientConnected())
            {
                IsConnecting = false;
                Status = "In match.";
                connectRoutine = null;
                yield break;
            }

            StopClientConnection(); // reset client state before the next attempt
            yield return new WaitForSeconds(ConnectionRetryDelaySeconds);
        }

        IsConnecting = false;
        connectRoutine = null;
        Status = "Couldn't connect to the game server.";
        Debug.LogError($"Edgegap MM | Failed to connect to {fqdn}:{port} after {ConnectionAttempts} attempts.");
    }

    private void OnGUI()
    {
        if (!ShowGUI || IsClientConnected())
            return;

        const float width = 260f;
        GUILayout.BeginArea(new Rect(Screen.width - width - 10f, 10f, width, 200f), GUI.skin.box);
        GUILayout.Label("Online Match");
        GUILayout.Label(Status);

        if (MatchmakingClient != null)
        {
            if (!IsMatchmakerHealthy)
            {
                if (GUILayout.Button("Retry"))
                    RetryMatchmakerStatus();
            }
            else if (IsSearching)
            {
                if (GUILayout.Button("Cancel"))
                    CancelMatchmaking();
            }
            else if (!IsConnecting)
            {
                if (GUILayout.Button("Find Match"))
                    FindMatch();
            }
        }

        GUILayout.EndArea();
    }

    #region NETCODE ADAPTER
    // FishNet 4.x with Bayou (WebSocket). With TLS Upgrade on the app version port, Edgegap
    // terminates TLS, so the client connects with wss:// to the fqdn (the certificate matches it).
    // Tugboat (UDP) would connect to publicIp instead.

    private void StartClientConnection(string fqdn, string publicIp, ushort port)
    {
        NetworkManager nm = InstanceFinder.NetworkManager;
        Transport transport = nm.TransportManager.Transport;
        if (transport is Bayou bayou)
        {
            bayou.SetUseWSS(UseSecureWebSocket);
            nm.ClientManager.StartConnection(fqdn, port);
            return;
        }
        nm.ClientManager.StartConnection(publicIp, port);
    }

    private bool IsClientConnecting() =>
        InstanceFinder.NetworkManager.TransportManager.Transport.GetConnectionState(false) == LocalConnectionState.Starting;

    private bool IsClientConnected() => InstanceFinder.NetworkManager.ClientManager.Started;

    private bool IsClientActive() =>
        InstanceFinder.NetworkManager.TransportManager.Transport.GetConnectionState(false) != LocalConnectionState.Stopped;

    private void StopClientConnection() => InstanceFinder.NetworkManager.ClientManager.StopConnection();
    #endregion
}
