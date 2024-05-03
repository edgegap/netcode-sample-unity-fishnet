using System;
using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceEdge
{
    /// <summary>
    /// This class acts as the entry point of the game.
    /// It communicates with the "MatchmakingSystem" to show status updates
    /// and send start game request to the "MatchmakingSystem". This class also provides
    /// the capability to the change player name.
    /// </summary>
    public class MainMenuSystem : MonoBehaviour
    {
        [SerializeField] private Button startGameButton;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_Text serverStatus;


        private MatchmakingSystem _matchmakingSystem;

        private void Awake()
        {
            _matchmakingSystem = FindObjectOfType<MatchmakingSystem>();

            _matchmakingSystem.OnStatusUpdate += ServerStatusUpdate;
            _matchmakingSystem.OnStartGameFailed += StartGameFailed;

            startGameButton.onClick.AddListener(StartGame);
            playerNameInput.onValueChanged.AddListener(NameChanged);
            if (PlayerPrefs.HasKey("PlayerName"))
                playerNameInput.text = PlayerPrefs.GetString("PlayerName");
        }


        /// <summary>
        /// This function calls the "FindDeployedServers" method of the "MatchmakingSystem".
        /// Once the request is sent, we disable the startGameButton so the user can not
        /// interrupt the matchmaking process by spoofing requests repeatedly. 
        /// </summary>
        private void StartGame()
        {
            startGameButton.gameObject.SetActive(false);
            serverStatus.text = String.Empty;
            _matchmakingSystem.FindDeployedServers();
        }

        /// <summary>
        /// This method is invoked by the "MatchmakingSystem" upon any status updates.
        /// It shows the status updates on the GUI and color code them based on their type (error or success)
        /// </summary>
        /// <param name="status"></param>
        /// <param name="isError"></param>
        private void ServerStatusUpdate(string status, bool isError)
        {
            serverStatus.text += "\n" + status;
            serverStatus.color = isError ? Color.red : Color.green;
            Debug.Log(status);
        }

        /// <summary>
        /// This method is invoked by the "MatchmakingSystem" upon failure to find any servers
        /// and deploying a new one. This condition can only happen upon system errors such as
        /// no internet connection or container image not starting due to build or configuration errors
        /// </summary>
        private void StartGameFailed()
        {
            serverStatus.text += "\n" + "Start Game Failed. Please Retry...";
            startGameButton.gameObject.SetActive(true);
        }

        private void NameChanged(string text) => PlayerPrefs.SetString("PlayerName", text);
    }
}