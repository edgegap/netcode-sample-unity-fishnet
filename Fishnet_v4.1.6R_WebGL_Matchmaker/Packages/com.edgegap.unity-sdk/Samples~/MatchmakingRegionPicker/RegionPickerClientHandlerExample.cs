using Edgegap;
using Edgegap.Matchmaking;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using L = Edgegap.Logger;
using MyTicketsAttributes = Edgegap.Matchmaking.LatenciesAttributesDTO;
using MyGroupUpRequestDTO = Edgegap.Matchmaking.SimpleGroupUpRequestDTO;

// todo replace LatenciesAttributesDTO with CustomTicketsAttributes
// todo replace SimpleGroupUpRequestDTO with CustomGroupUpRequestDTO

public class RegionPickerClientHandlerExample : MonoBehaviour
{
    public static RegionPickerClientHandlerExample Instance { get; private set; }

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
    public bool DeleteTicketOnPause = false;
    public bool DeleteTicketOnQuit = true;

    [Header("Logging")]
    public bool LogTicketUpdates = true;
    public bool LogAssignmentUpdates = true;
    public bool LogPollingUpdates = false;
    #endregion

    public GroupClient<
            MyGroupUpRequestDTO, 
            MyTicketsAttributes
        > MatchmakingClient;

    #region Region Picker UI
    public GameObject ScrollListContainer;
    public GameObject DisconnectButton;
    public Text StatusDisplay;
    public GameObject HubBtnPrefab;

    private string ScrollListContainerDefaultPath = "/Canvas/Scroll View/Viewport/Content";
    private string DisconnectButtonDefaultPath = "/Canvas/DisconnectBtn";
    private string StatusDisplayDefaultPath = "/Canvas/StatusTxt";
    #endregion

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

            if (ScrollListContainer == null)
            {
                L.Log("MM ClientHandler | No Scroll List Container provided, using default.");
                ScrollListContainer = GameObject.Find(ScrollListContainerDefaultPath);

                if (ScrollListContainer == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default component {ScrollListContainerDefaultPath} in scene.");
                }
            }

            if (DisconnectButton == null)
            {
                L.Log("MM ClientHandler | No Disconnect Button provided, using default.");
                DisconnectButton = GameObject.Find(DisconnectButtonDefaultPath);

                if (DisconnectButton == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default component {DisconnectButtonDefaultPath} in scene.");
                }
                else
                {
                    DisconnectButton.GetComponent<Button>().onClick.AddListener(Disconnect);
                    DisconnectButton.SetActive(false);
                }
            }

            if (StatusDisplay == null)
            {
                L.Log("MM ClientHandler | No Status Display provided, using default.");
                StatusDisplay = GameObject.Find(StatusDisplayDefaultPath).GetComponent<Text>();

                if (StatusDisplay == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default component {StatusDisplayDefaultPath} in scene.");
                }
            }

            if (HubBtnPrefab == null)
            {
                L.Log("MM ClientHandler | No Hub Button Prefab provided, using default.");
                string guid = AssetDatabase.FindAssets($"t:Script {nameof(RegionPickerClientHandlerExample)}")[0];
                string currentAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                string hubBtnPrefabDefaultPath = currentAssetPath.Split(nameof(RegionPickerClientHandlerExample))[0] + "BeaconHubButton.prefab";

                HubBtnPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(hubBtnPrefabDefaultPath, typeof(GameObject));

                if (HubBtnPrefab == null)
                {
                    L.Warn($"MM ClientHandler | Unable to find default prefab {hubBtnPrefabDefaultPath} in assets.");
                }
            }
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
                if (action == ObservableActionType.Update && message == "healthy")
                {
                    // todo update UI

                    StatusDisplay.text = "Fetching beacons...";

                    MatchmakingClient.Beacons(
                        (BeaconsResponseDTO beacons) =>
                        {
                            MatchmakingClient.MeasureBeaconsRoundTripTime(
                                beacons.Beacons,
                                (Dictionary<string, float> pings) =>
                                {
                                    StatusDisplay.text = "";

                                    if (ScrollListContainer.transform.childCount > 0)
                                    {
                                        Dictionary<string, float> newPings = new Dictionary<string, float>(pings);

                                        foreach (Transform child in ScrollListContainer.transform)
                                        {
                                            child.gameObject.SetActive(true);
                                            string label = child.gameObject.GetComponent<BeaconHubButton>().BtnLabel.text;

                                            if (pings.ContainsKey(label))
                                            {
                                                child.gameObject.GetComponent<BeaconHubButton>().SetLatencyIcon(pings[label]);
                                                newPings.Remove(label);
                                            }
                                            else
                                            {
                                                child.gameObject.GetComponent<BeaconHubButton>().DisableLatencyBtn();
                                            }
                                        }

                                        foreach (KeyValuePair<string, float> entry in newPings)
                                        {
                                            GameObject btn = Instantiate(HubBtnPrefab, ScrollListContainer.transform);
                                            btn.GetComponent<BeaconHubButton>().BtnLabel.text = entry.Key;
                                            btn.GetComponent<BeaconHubButton>().SetLatencyIcon(entry.Value);
                                            btn.GetComponent<Button>().onClick.AddListener(() => OnHubBtnClick(entry.Key, entry.Value));
                                        }

                                        BeaconHubButton[] btns = ScrollListContainer.GetComponentsInChildren<BeaconHubButton>();
                                        btns.OrderBy(x => x.GetPing() == -1 ? 1 : 0).ThenBy(x => x.GetPing());

                                        for (int i = 0; i < btns.Length; ++i)
                                        {
                                            btns[i].transform.SetSiblingIndex(i);
                                        }
                                    }
                                    else
                                    {
                                        foreach (KeyValuePair<string, float> entry in pings.OrderBy(key => key.Value))
                                        {
                                            GameObject btn = Instantiate(HubBtnPrefab, ScrollListContainer.transform);
                                            btn.GetComponent<BeaconHubButton>().BtnLabel.text = entry.Key;
                                            btn.GetComponent<BeaconHubButton>().SetLatencyIcon(entry.Value);
                                            btn.GetComponent<Button>().onClick.AddListener(() => OnHubBtnClick(entry.Key, entry.Value));
                                        }
                                    }
                                }
                            );
                        },
                        (string error, UnityWebRequest request) =>
                        {
                            // todo handle beacon downtime, create tickets without beacons?
                            StatusDisplay.text = "Beacon error, see logs";
                            L.Log($"MM ClientHandler |  Beacon error.\n{error}");
                        }
                    );
                }
                else if (message != "healthy")
                {
                    // todo handle outage/maintenance
                    L.Error($"MM ClientHandler | Service is unhealthy.\n{monitor.Current}");
                    MatchmakingClient.StopMatchmaking();
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
                    && group.Current.Status == "MATCH_FOUND"
                )
                {
                    StatusDisplay.text = "Match found, awaiting assignment";
                    DisconnectButton.SetActive(false);
                }

                if (
                    (
                        action == ObservableActionType.Update
                        && message.Contains("updated")
                        && group.Current.Status == "HOST_ASSIGNED"
                    )
                    || (
                        action == ObservableActionType.Log
                        && message.Contains("reconnect suggested")
                    )
                )
                {
                    foreach (Transform child in ScrollListContainer.transform)
                    {
                        Destroy(child.gameObject);
                    }

                    // todo join game on pre-defined game port
                    StatusDisplay.text = "Host assigned, joining game";
                    L.Log(
                        $"MM ClientHandler | Joining game: {group.Current.Assignment.Ports["gameport"].Link}"
                    );

                    DisconnectButton.GetComponentInChildren<Text>().text = "Disconnect";
                    DisconnectButton.SetActive(true);
                    DisconnectButton.GetComponent<Button>().interactable = false;
                }

                if (
                    action == ObservableActionType.Update
                        && message.Contains("removed")
                        && group.Previous?.Status == "HOST_ASSIGNED"
                )
                {
                    DisconnectButton.GetComponent<Button>().interactable = true;
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

    public void StopMatchmaking()
    {
        MatchmakingClient.StopMatchmaking();
        MatchmakingClient.Status();
    }

    public void OnHubBtnClick(string cityName, float ping)
    {
        StartMatchmaking(new Dictionary<string, float> { { cityName, ping } }, true);

        foreach (Transform child in ScrollListContainer.transform)
        {
            child.gameObject.SetActive(false);
        }

        StatusDisplay.text = $"Selected region: {cityName}\nMatchmaking in progress...";

        DisconnectButton.GetComponentInChildren<Text>().text = "Cancel";
        DisconnectButton.SetActive(true);
        DisconnectButton.GetComponent<Button>().interactable = true;
    }

    public void Disconnect()
    {
        DisconnectButton.SetActive(false);

        if (DisconnectButton.GetComponentInChildren<Text>().text == "Cancel")
        {
            StatusDisplay.text = "Cancelling ticket, returning to matchmaking";
            L.Log("MM ClientHandler | Cancelling ticket, returning to matchmaking.");
        }
        else
        {
            StatusDisplay.text = "Disconnecting from server, returning to matchmaking";
            L.Log("MM ClientHandler | Disconnecting from server, returning to matchmaking.");
        }

        StopMatchmaking();
    }
}
