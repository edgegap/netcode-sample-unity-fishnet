using Edgegap;
using Edgegap.Matchmaking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using L = Edgegap.Logger;
using MyGroupUpRequestDTO = Edgegap.Matchmaking.SimpleGroupUpRequestDTO;
using MyTicketsAttributes = Edgegap.Matchmaking.LatenciesAttributesDTO;

// todo replace LatenciesAttributesDTO with CustomTicketsAttributes
// todo replace SimpleGroupUpRequestDTO with CustomGroupUpRequestDTO

public class GroupUpClientHandlerExample : MonoBehaviour
{
    public static GroupUpClientHandlerExample Instance { get; private set; }

    #region Matchmaking Configuration

    [Header("Matchmaker Instance")]
    public string BaseUrl;
    public string AuthToken;

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

    #region Group Up UI
    public Button CreateGroupButton;
    public Button DeleteGroupButton;
    public Button SetReadyButton;
    public InputField IdInputField;
    public Button JoinGroupButton;
    public Button LeaveGroupButton;
    public Text StatusDisplay;

    private string CreateGroupButtonDefaultPath = "/Canvas/Btns/Owner/CreateGroupBtn";
    private string DeleteGroupButtonDefaultPath = "/Canvas/Btns/Owner/DeleteGroupBtn";
    private string SetReadyButtonDefaultPath = "/Canvas/Btns/Owner/SetReadyBtn";
    private string IdInputFieldDefaultPath = "/Canvas/Btns/Member/InputID";
    private string JoinGroupButtonDefaultPath = "/Canvas/Btns/Member/JoinGroupBtn";
    private string LeaveGroupButtonDefaultPath = "/Canvas/Btns/Member/LeaveGroupBtn";
    private string StatusDisplayDefaultPath = "/Canvas/StatusTxt";
    #endregion

    private Dictionary<string, float> Pings;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;

            if (CreateGroupButton == null)
            {
                L.Log("MM ClientHandler | No Create Group Button provided, using default.");
                CreateGroupButton = GameObject.Find(CreateGroupButtonDefaultPath)?.GetComponent<Button>();

                if (CreateGroupButton == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default component {CreateGroupButtonDefaultPath} in scene.");
                }
                else
                {
                    CreateGroupButton.GetComponent<Button>().onClick.AddListener(() => StartMatchmaking(Pings, false));
                }
            }

            if (DeleteGroupButton == null)
            {
                L.Log("MM ClientHandler | No Delete Group Button provided, using default.");
                DeleteGroupButton = GameObject.Find(DeleteGroupButtonDefaultPath)?.GetComponent<Button>();

                if (DeleteGroupButton == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default component {DeleteGroupButtonDefaultPath} in scene.");
                }
                else
                {
                    DeleteGroupButton.GetComponent<Button>().onClick.AddListener(StopMatchmaking);
                    DeleteGroupButton.gameObject.SetActive(false);
                }
            }

            if (SetReadyButton == null)
            {
                L.Log("MM ClientHandler | No Set Ready Button provided, using default.");
                SetReadyButton = GameObject.Find(SetReadyButtonDefaultPath)?.GetComponent<Button>();

                if (SetReadyButton == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default component {SetReadyButtonDefaultPath} in scene.");
                }
                else
                {
                    SetReadyButton.GetComponent<Button>().onClick.AddListener(SetReady);
                    SetReadyButton.gameObject.SetActive(false);
                }
            }

            if (IdInputField == null)
            {
                L.Log("MM ClientHandler | No Group ID Input Field provided, using default.");
                IdInputField = GameObject.Find(IdInputFieldDefaultPath)?.GetComponent<InputField>();

                if (IdInputField == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default component {IdInputFieldDefaultPath} in scene.");
                }
                else
                {
                    IdInputField.onValueChanged.AddListener(OnInputUpdate);
                }
            }

            if (JoinGroupButton == null)
            {
                L.Log("MM ClientHandler | No Join Group Button provided, using default.");
                JoinGroupButton = GameObject.Find(JoinGroupButtonDefaultPath)?.GetComponent<Button>();

                if (JoinGroupButton == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default component {JoinGroupButtonDefaultPath} in scene.");
                }
                else
                {
                    JoinGroupButton.GetComponent<Button>().onClick.AddListener(() => StartMatchmaking(Pings, true, IdInputField.text));
                }
            }

            if (LeaveGroupButton == null)
            {
                L.Log("MM ClientHandler | No Join Group Button provided, using default.");
                LeaveGroupButton = GameObject.Find(LeaveGroupButtonDefaultPath)?.GetComponent<Button>();

                if (LeaveGroupButton == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default component {LeaveGroupButtonDefaultPath} in scene.");
                }
                else
                {
                    LeaveGroupButton.GetComponent<Button>().onClick.AddListener(StopMatchmaking);
                    LeaveGroupButton.gameObject.SetActive(false);
                }
            }

            if (StatusDisplay == null)
            {
                L.Log("MM ClientHandler | No Status Display provided, using default.");
                StatusDisplay = GameObject.Find(StatusDisplayDefaultPath)?.GetComponent<Text>();

                if (StatusDisplay == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default component {StatusDisplayDefaultPath} in scene.");
                }
            }
        }
    }

    private void Start()
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
            LogGroupUpdates,
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
                        StatusDisplay.text = "Fetching beacons...";
                        DisableButtons();

                        MatchmakingClient.Beacons(
                            (BeaconsResponseDTO beacons) =>
                            {
                                Debug.Log($"beacons: {beacons}");

                                MatchmakingClient.MeasureBeaconsRoundTripTime(
                                    beacons.Beacons,
                                    (Dictionary<string, float> pings) =>
                                    {
                                        Pings = pings;
                                        StatusDisplay.text = "";
                                        ResetButtons();
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
                    action == ObservableActionType.Error
                    && group.Current is null
                )
                {
                    ResetButtons();
                }

                if (
                    action == ObservableActionType.Warn
                    && message.Contains("member update failed")
                )
                {
                    SetReadyButton.interactable = false;
                    EnableStopButton();
                }

                if (
                    action == ObservableActionType.Update
                    && message.Contains("updated")
                )
                {
                    // todo update UI
                }

                if (
                    action == ObservableActionType.Update
                    && (
                            message.Contains("abandon")
                            || message.Contains("removed")
                        )
                )
                {
                    StatusDisplay.text = "Disconnected from group.";
                    MatchmakingClient.Status();
                }

                if (
                    action == ObservableActionType.Update
                    && message.Contains("created")
                )
                {
                    StatusDisplay.text = $"Group created, awaiting members.\nID: {group.Current.GroupID}";
                    Debug.Log($"Group created.\nID: {group.Current.GroupID}");

                    CreateGroupButton.gameObject.SetActive(false);

                    SetReadyButton.gameObject.SetActive(true);
                    SetReadyButton.interactable = true;

                    DeleteGroupButton.gameObject.SetActive(true);
                    DeleteGroupButton.interactable = true;
                }

                if (
                    action == ObservableActionType.Update
                    && message.Contains("joined")
                )
                {
                    StatusDisplay.text = "Group joined, waiting for group owner to set ready.";

                    JoinGroupButton.gameObject.SetActive(false);
                    IdInputField.gameObject.SetActive(false);

                    LeaveGroupButton.gameObject.SetActive(true);
                    LeaveGroupButton.interactable = true;
                }

                if (
                    action == ObservableActionType.Update
                    && message.Contains("member updated")
                )
                {
                    SetReadyButtonText();
                    SetReadyButton.interactable = true;
                    EnableStopButton();
                }

                if (
                    action == ObservableActionType.Update
                    && message.Contains("updated")
                    && group.Current.Status == "SEARCHING"
                )
                {
                    StatusDisplay.text = "Matchmaking started, searching...";
                    SetReadyButton.interactable = false;
                }

                if (
                    action == ObservableActionType.Update
                    && message.Contains("updated")
                    && group.Current.Status == "MATCH_FOUND"
                )
                {
                    StatusDisplay.text = "Match found, awaiting assignment.";
                    DisableButtons();
                }

                if (
                    action == ObservableActionType.Update
                    && message.Contains("updated")
                    && group.Current.Status == "HOST_ASSIGNED"
                )
                {
                    // todo join game on pre-defined game port
                    StatusDisplay.text = "Host assigned, joining game.";
                    Debug.Log(
                        $"joining game: {group.Current.Assignment.Ports["gameport"].Link}"
                    );
                }
            }
        );
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

    public void StartMatchmaking(Dictionary<string, float> pings, bool isReady, string groupID = null)
    {
        DisableButtons();

        if (groupID is null)
        {
            MatchmakingClient.CreateGroup(new MyGroupUpRequestDTO(pings, isReady));
        }
        else
        {
            MatchmakingClient.JoinGroup(new MyGroupUpRequestDTO(pings, isReady), groupID);
        }
    }

    public void SetReady()
    {
        DisableButtons();
        bool newState = !MatchmakingClient.Group.Current.IsReady;
        MatchmakingClient.SetReady(newState);
    }

    public void StopMatchmaking()
    {
        if (MatchmakingClient.Owner)
        {
            StatusDisplay.text = "Deleting group.";
        }
        else
        {
            StatusDisplay.text = "Leaving group.";
        }

        DisableButtons();
        MatchmakingClient.StopMatchmaking();
    }

    private void OnInputUpdate(string newVal)
    {
        if (string.IsNullOrEmpty(newVal))
        {
            JoinGroupButton.interactable = false;
        }
        else
        {
            JoinGroupButton.interactable = true;
        }
    }

    private void DisableButtons()
    {
        CreateGroupButton.interactable = false;
        JoinGroupButton.interactable = false;
        IdInputField.interactable = false;
        SetReadyButton.interactable = false;
        DeleteGroupButton.interactable = false;
        LeaveGroupButton.interactable = false;
    }

    private void EnableStopButton()
    {
        if (MatchmakingClient.Owner)
        {
            DeleteGroupButton.interactable = true;
        }
        else
        {
            LeaveGroupButton.interactable = true;
        }
    }

    private void ResetButtons()
    {
        SetReadyButton.GetComponentInChildren<Text>().text = "Set Ready";
        SetReadyButton.interactable = false;
        SetReadyButton.gameObject.SetActive(false);

        DeleteGroupButton.interactable = false;
        DeleteGroupButton.gameObject.SetActive(false);

        LeaveGroupButton.interactable = false;
        LeaveGroupButton.gameObject.SetActive(false);


        CreateGroupButton.gameObject.SetActive(true);
        CreateGroupButton.interactable = true;

        IdInputField.gameObject.SetActive(true);
        IdInputField.interactable = true;

        JoinGroupButton.gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(IdInputField.text))
        {
            JoinGroupButton.interactable = true;
        }
    }

    private void SetReadyButtonText()
    {
        if (MatchmakingClient.Group.Current.IsReady)
        {
            SetReadyButton.GetComponentInChildren<Text>().text = "Waiting";
        }
        else
        {
            SetReadyButton.GetComponentInChildren<Text>().text = "Set Ready";
        }
    }
}
