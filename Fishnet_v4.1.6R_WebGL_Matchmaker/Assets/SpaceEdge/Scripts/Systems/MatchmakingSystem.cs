using System;
using UnityEngine;

namespace SpaceEdge
{
    /// <summary>
    ///     Bridges the main menu and the Edgegap matchmaker.
    ///     The game client never calls the Edgegap API and holds no API token: it creates a matchmaking ticket
    ///     with the matchmaker's client-safe Auth Token (see "EdgegapMatchmakingClient"). When players are matched,
    ///     Edgegap deploys a dedicated server near them and every client connects to it automatically.
    ///     The dedicated server stops its own deployment once the match is empty (see "EdgegapServerLifecycle").
    /// </summary>
    public class MatchmakingSystem : MonoBehaviour
    {
        private const string InMatchStatus = "In match.";

        private EdgegapMatchmakingClient _matchmaker;
        private string _lastStatus;
        private bool _searching;

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
            if (_matchmaker == null)
                Debug.LogError("MatchmakingSystem | No EdgegapMatchmakingClient in the scene.");
            else
            {
                _matchmaker.ShowGUI = false; // the main menu shows the status instead
                // The matchmaking client survives scene changes: after leaving a match, don't replay "In match."
                if (_matchmaker.Status == InMatchStatus)
                    _lastStatus = InMatchStatus;
            }
        }

        /// <summary>
        ///     Creates a matchmaking ticket. Progress is reported through OnStatusUpdate.
        /// </summary>
        public void FindMatch()
        {
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
