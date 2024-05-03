using FishNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MatchmakerManager;

public class MatchmakerUI : MonoBehaviour
{
    [SerializeField] private Button createBtn;
    [SerializeField] private Button deleteBtn;
    [SerializeField] private List<Toggle> modeToggles;
    [SerializeField] private TMP_Text serverStatus;
    [SerializeField] private TMP_InputField scoreInputField;
    [SerializeField] private TMP_InputField playerNameInput;

    private MatchmakerManager _matchmaker;
    private TicketData? currentTicket = null;
    private bool isWaiting = true;
    private bool isReady = false;
    private float waitingTimeSec = 5;
    private string mode = "casual";
    private int score = 0;
    private int defaultScore = 2000;

    // Start is called before the first frame update
    void Start()
    {
        _matchmaker = (MatchmakerManager)ScriptableObject.CreateInstance("MatchmakerManager");
        _matchmaker.OnStatusUpdate += ServerStatusUpdate;
        scoreInputField.onValueChanged.AddListener(delegate(string input) { ValidateScore(input); });

        createBtn.onClick.AddListener(CreateTicketClick);
        deleteBtn.onClick.AddListener(DeleteTicketClick);

        foreach (Toggle t in modeToggles)
        {
            ToggleValueChanged(t);

            t.onValueChanged.AddListener(delegate {
                ToggleValueChanged(t);
            });
        }

        playerNameInput.onValueChanged.AddListener(NameChanged);
        if (PlayerPrefs.HasKey("PlayerName"))
            playerNameInput.text = PlayerPrefs.GetString("PlayerName");
    }

    // Update is called once per frame
    void Update()
    {
        if (currentTicket is not null)
        {
            if (currentTicket?.Connection?.Address is not null && !isReady)
            {
                isReady = true;
                ConnectMatch();
            }

            if (!isWaiting && !isReady)
            {
                RefreshTicket();
                StartCoroutine(Waiting());
            }
        }
        else
        {
            deleteBtn.interactable = false;
        }

        if (string.IsNullOrEmpty(scoreInputField.text))
        {
            createBtn.interactable = false;
        }
        else
        {
            createBtn.interactable = true;
        }
    }

    private void NameChanged(string text) => PlayerPrefs.SetString("PlayerName", text);

    /// <summary>
    /// This method is invoked by the "MatchmakingSystem" upon any status updates.
    /// It shows the status updates on the GUI and color code them based on their type (error or success)
    /// </summary>
    /// <param name="status"></param>
    /// <param name="isError"></param>
    private void ServerStatusUpdate(string status, bool isError)
    {
        serverStatus.text = status;
        serverStatus.color = isError ? Color.red : Color.green;
        Debug.Log(status);
    }

    public async void CreateTicketClick()
    {
        try
        {
            createBtn.interactable = false;
            ChangeTogglesInteractState(false);

            currentTicket = await _matchmaker.CreateTicket(score, mode);
            isWaiting = false;
            deleteBtn.interactable = true;
        }
        catch (HttpRequestException httpEx)
        {
            _matchmaker.OnStatusUpdate?.Invoke($"Failed To Create Ticket, with message: \n{httpEx.Message}", true);

            currentTicket = null;
            createBtn.interactable = true;
            deleteBtn.interactable = false;
            ChangeTogglesInteractState(true);
        }
    }

    public async void DeleteTicketClick()
    {
        try
        {
            deleteBtn.interactable = false;
            await _matchmaker.DeleteTicket(currentTicket?.Id);
        }
        catch (HttpRequestException httpEx)
        {
            _matchmaker.OnStatusUpdate?.Invoke($"Failed To Delete Ticket, with message: \n{httpEx.Message}", true);
        }
        finally
        {
            currentTicket = null;
            createBtn.interactable = true;
            ChangeTogglesInteractState(true);
        }
    }

    public void ToggleValueChanged(Toggle toggle)
    {
        if (toggle.isOn)
        {
            string value = toggle.gameObject.GetComponentInChildren<TextMeshProUGUI>().text;
            mode = value.ToLower();
            Debug.Log($"Changed mode to {value.ToLower()};");
        }
    }

    public void ChangeTogglesInteractState(bool state)
    {
        foreach (Toggle t in modeToggles)
        {
            t.interactable = state;
        }
    }

    public async void RefreshTicket()
    {
        try
        {
            Debug.Log("refresh");
            currentTicket = await _matchmaker.GetTicket(currentTicket?.Id);
        }
        catch (HttpRequestException httpEx)
        {
            _matchmaker.OnStatusUpdate?.Invoke($"Failed To Refresh Ticket, with message: \n{httpEx.Message}", true);
            currentTicket = null;
        }
    }

    public void ConnectMatch()
    {
        try
        {
            var assignment = currentTicket?.Connection?.Address;

            _matchmaker.OnStatusUpdate?.Invoke("Server Is Ready For Connection, Starting The Game...", false);

            string[] networkComponents = assignment.Split(':');
            InstanceFinder.TransportManager.Transport.SetClientAddress(networkComponents[0]);

            if (ushort.TryParse(networkComponents[1], out ushort port))
            {
                InstanceFinder.TransportManager.Transport.SetPort(port);
            }
            else
            {
                throw new Exception("port couldn't be parsed");
            }

            InstanceFinder.ClientManager.StartConnection();
        }
        catch (Exception e)
        {
            _matchmaker.OnStatusUpdate?.Invoke($"Failed To Connect To Server, with message: \n{e.Message}", true);
            currentTicket = null;
            isWaiting = true;
            isReady = false;
            deleteBtn.interactable = false;
            createBtn.interactable = true;
            ChangeTogglesInteractState(true);
        }
    }

    private void ValidateScore(string scoreStr)
    {
        if (int.TryParse(scoreStr, out int result))
        {
            score = result;
        }
        else if (!string.IsNullOrEmpty(scoreStr))
        {
            Debug.Log("using default value");
            scoreInputField.text = defaultScore.ToString();
        }
    }

    IEnumerator Waiting()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitingTimeSec);
        isWaiting = false;
    }
}
