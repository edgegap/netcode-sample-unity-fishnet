// Edgegap dedicated server lifecycle. Requires com.edgegap.unity-sdk 3.5.5+.
// On the server build only: starts the netcode server headless, reads the variables Edgegap and
// the matchmaker inject, and stops its own deployment when the match is over (empty servers cost money).
// Netcode-specific code lives only in the "NETCODE ADAPTER" region at the bottom.
// Adapted from the end-to-end verified edgegap/mirror-webgl sample.
using System;
using System.Collections;
using Edgegap;
using Edgegap.Matchmaking;
using FishNet;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.Networking;

[DisallowMultipleComponent]
public class EdgegapServerLifecycle : MonoBehaviour
{
    [Tooltip("Start the netcode server automatically in server builds / -batchmode.")]
    public bool AutoStartServer = true;

    [Tooltip("Internal port the transport listens on. Must equal the app version's internal port.")]
    public ushort ListenPort = 7777;

    [Tooltip("Stop the deployment if nobody connects within this time (matched players gave up).")]
    public float FirstPlayerTimeoutSeconds = 120f;

    [Tooltip("Stop the deployment once all players have been gone for this long.")]
    public float EmptyServerTimeoutSeconds = 30f;

    public DeploymentEnvironmentDTO DeploymentEnv { get; private set; }
    public MatchEnvironmentDTO<GameTicketAttributes> MatchEnv { get; private set; }

    /// <summary>Number of players the matchmaker put in this match (0 when not started by a matchmaker).</summary>
    public int ExpectedPlayers => MatchEnv?.TicketIds?.Count ?? 0;

    private SafeHttpRequest request;
    private float startedAt;
    private float emptySince = -1f;
    private bool anyPlayerJoined;
    private bool stopping;
    private bool onEdgegap;

    private void Start()
    {
#if UNITY_SERVER
        bool isServerBuild = true;
#else
        bool isServerBuild = Application.isBatchMode;
#endif
        if (!isServerBuild)
        {
            enabled = false;
            return;
        }

        // Headless server: don't render or tick faster than needed.
        Application.targetFrameRate = 60;

        if (AutoStartServer)
        {
            Debug.Log($"Edgegap Server | Starting server on 0.0.0.0:{ListenPort}.");
            StartDedicatedServer(ListenPort);
        }

        IDictionary env = Environment.GetEnvironmentVariables();

        // Check before parsing: SDK 3.5.5's DeploymentEnvironmentDTO throws a NullReferenceException
        // when ARBITRIUM_PORTS_MAPPING is missing, which is the case outside Edgegap (local runs).
        if (string.IsNullOrEmpty(env["ARBITRIUM_REQUEST_ID"] as string))
        {
            Debug.LogWarning("Edgegap Server | Not running on Edgegap, self-stop disabled.");
            return;
        }

        onEdgegap = true;
        DeploymentEnv = new DeploymentEnvironmentDTO(env);
        MatchEnv = new MatchEnvironmentDTO<GameTicketAttributes>(env);
        request = new SafeHttpRequest(this);
        startedAt = Time.realtimeSinceStartup;

        Debug.Log(
            $"Edgegap Server | Deployment '{DeploymentEnv.RequestID}' started for match "
                + $"'{MatchEnv.MatchId ?? "n/a"}', profile '{MatchEnv.MatchProfile ?? "n/a"}', "
                + $"{ExpectedPlayers} matched player(s)."
        );
    }

    private void Update()
    {
        if (!onEdgegap || stopping || !IsServerRunning())
            return;

        float now = Time.realtimeSinceStartup;

        if (ConnectedPlayerCount() > 0)
        {
            anyPlayerJoined = true;
            emptySince = -1f;
            return;
        }

        if (!anyPlayerJoined)
        {
            if (now - startedAt > FirstPlayerTimeoutSeconds)
                SelfStop("no player connected in time");
            return;
        }

        if (emptySince < 0f)
            emptySince = now;
        else if (now - emptySince > EmptyServerTimeoutSeconds)
            SelfStop("all players left");
    }

    /// <summary>Call this from game code when the match ends (e.g. after showing results).</summary>
    public void SelfStop(string reason)
    {
        if (stopping)
            return;
        stopping = true;

        Debug.Log($"Edgegap Server | Stopping deployment: {reason}.");

        if (DeploymentEnv == null
            || string.IsNullOrEmpty(DeploymentEnv.SelfStopURL)
            || string.IsNullOrEmpty(DeploymentEnv.SelfStopToken))
        {
            Debug.LogError("Edgegap Server | Self-stop URL or token missing, quitting process instead.");
            Application.Quit();
            return;
        }

        // Edgegap's self-stop endpoint for this deployment. URL and one-time key are injected by Edgegap
        // into this server container and parsed by the SDK; they never leave Edgegap's infrastructure.
        request.Delete(
            DeploymentEnv.SelfStopURL,
            DeploymentEnv.SelfStopToken,
            (string response, UnityWebRequest req) => Debug.Log("Edgegap Server | Self-stop requested."),
            (string error, UnityWebRequest req) =>
            {
                Debug.LogError($"Edgegap Server | Self-stop failed, quitting process.\n{error}");
                Application.Quit();
            },
            new RetryParameters { MaxAttempts = 10, RemainingAttempts = 10 }
        );
    }

    #region NETCODE ADAPTER
    // FishNet 4.x. Leave "Start On Headless" off on the ServerManager: this component starts the server.

    private void StartDedicatedServer(ushort port)
    {
        NetworkManager nm = InstanceFinder.NetworkManager;
        nm.TransportManager.Transport.SetPort(port);
        nm.ServerManager.StartConnection();
    }

    private bool IsServerRunning() => InstanceFinder.NetworkManager != null && InstanceFinder.NetworkManager.ServerManager.Started;

    private int ConnectedPlayerCount() => InstanceFinder.NetworkManager.ServerManager.Clients.Count;
    #endregion
}
