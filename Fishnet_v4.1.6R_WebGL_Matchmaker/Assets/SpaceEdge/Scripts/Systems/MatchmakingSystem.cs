using System;
using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Bayou;
using UnityEngine;

namespace SpaceEdge
{
    /// <summary>
    ///     Connects the main menu's "Start Game" button to a game server, in one of two modes:
    ///     - Direct connection (default): connects to the address and port below. Use it to test a local server
    ///       (Docker or a server build) or a deployment you started from the Edgegap dashboard.
    ///     - Edgegap Matchmaker (opt-in): creates a matchmaking ticket with the matchmaker's client-safe Auth Token
    ///       (see "EdgegapMatchmakingClient"). When players are matched, Edgegap deploys a dedicated server near them
    ///       and every client connects to it automatically.
    ///     The game client never calls the Edgegap API and holds no API token in either mode.
    ///     The dedicated server stops its own Edgegap deployment once the match is empty (see "EdgegapServerLifecycle").
    /// </summary>
    public class MatchmakingSystem : MonoBehaviour
    {
        private const string InMatchStatus = "In match.";

        [Header("Mode")]
        [Tooltip("Off: connect directly to the server below. On: find a match with the Edgegap Matchmaker "
            + "(set Base Url and Auth Token on the Edgegap object's EdgegapMatchmakingClient first).")]
        [SerializeField] private bool useEdgegapMatchmaker = false;

        [Header("Direct Connection (matchmaker off)")]
        [Tooltip("Server host: localhost for a local server, or a deployment's FQDN, e.g. 1a2b3c4d5e6f.pr.edgegap.net.")]
        [SerializeField] private string directAddress = "localhost";

        [Tooltip("Server port: 7770 locally, or the deployment's external port.")]
        [SerializeField] private ushort directPort = 7770;

        [Tooltip("Use wss://. Required for Edgegap deployments (TLS Upgrade) and HTTPS pages; off for a local server.")]
        [SerializeField] private bool directUseWss = false;

        private EdgegapMatchmakingClient _matchmaker;
        private string _lastStatus;
        private bool _searching;
        private bool _directConnecting;

        public Action OnStartGameFailed;

        //These public events are used to communicate with the "MainMenuSystem".
        public Action<string, bool> OnStatusUpdate;

        private void Awake()
        {
            if (Application.isBatchMode)
            {
                // the dedicated server never matchmakes
                enabled = false;
                return;
            }

            _matchmaker = FindObjectOfType<EdgegapMatchmakingClient>();

            if (!useEdgegapMatchmaker)
            {
                // Runs before the client's Start, so it never contacts the matchmaker.
                if (_matchmaker != null)
                    _matchmaker.enabled = false;
                _matchmaker = null;
                return;
            }

            if (_matchmaker == null)
            {
                Debug.LogError("MatchmakingSystem | No EdgegapMatchmakingClient in the scene.");
                return;
            }

            _matchmaker.enabled = true;
            _matchmaker.ShowGUI = false; // the main menu shows the status instead
            // The matchmaking client survives scene changes: after leaving a match, don't replay "In match."
            if (_matchmaker.Status == InMatchStatus)
                _lastStatus = InMatchStatus;
        }

        private void OnDestroy()
        {
            if (_directConnecting && InstanceFinder.ClientManager != null)
                InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }

        /// <summary>
        ///     Starts the game: a direct connection, or a matchmaking ticket. Progress is reported through OnStatusUpdate.
        /// </summary>
        public void FindMatch()
        {
            if (!useEdgegapMatchmaker)
            {
                ConnectDirectly();
                return;
            }

            if (_matchmaker == null || !_matchmaker.IsMatchmakerHealthy)
            {
                OnStatusUpdate?.Invoke("Matchmaker is not available, check its URL and Auth Token.", true);
                OnStartGameFailed?.Invoke();
                return;
            }

            _searching = true;
            _matchmaker.FindMatch();
        }

        public void CancelMatch() => _matchmaker?.CancelMatchmaking();

        private void ConnectDirectly()
        {
            if (InstanceFinder.TransportManager.Transport is Bayou bayou)
                bayou.SetUseWSS(directUseWss);

            string scheme = directUseWss ? "wss" : "ws";
            OnStatusUpdate?.Invoke($"Connecting to {scheme}://{directAddress}:{directPort}...", false);
            _directConnecting = true;
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
            InstanceFinder.ClientManager.StartConnection(directAddress, directPort);
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                StopWatchingConnection();
                OnStatusUpdate?.Invoke(InMatchStatus, false);
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                StopWatchingConnection();
                OnStatusUpdate?.Invoke($"Couldn't connect to {directAddress}:{directPort}. Is the server running?", true);
                OnStartGameFailed?.Invoke();
            }
        }

        private void StopWatchingConnection()
        {
            _directConnecting = false;
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }

        private void Update()
        {
            if (_matchmaker == null || _matchmaker.Status == _lastStatus)
                return;

            _lastStatus = _matchmaker.Status;
            bool failed = _searching && !_matchmaker.IsSearching && !_matchmaker.IsConnecting
                && _lastStatus != InMatchStatus;
            OnStatusUpdate?.Invoke(_lastStatus, failed);

            if (failed)
            {
                _searching = false;
                OnStartGameFailed?.Invoke();
            }
            else if (_lastStatus == InMatchStatus)
            {
                _searching = false;
            }
        }
    }
}
