using UnityEngine;
using UnityEngine.UI;

public class BeaconHubButton : MonoBehaviour
{
    public float GoodThreshold = 50;
    public float MidThreshold = 100;
    public float PoorThreshold = 200;
    public Text BtnLabel;
    public GameObject GoodLatencyIcon;
    public GameObject MidLatencyIcon;
    public GameObject PoorLatencyIcon;

    private string LabelComponentDefaultPath = "BtnLabel";
    private string GoodIconComponentDefaultPath = "LatencyIcon/LatencyGood";
    private string MidIconComponentDefaultPath = "LatencyIcon/LatencyMid";
    private string PoorIconComponentDefaultPath = "LatencyIcon/LatencyPoor";
    private float _ping;

    private void Awake()
    {
        if (BtnLabel == null)
        {
            BtnLabel = transform.Find(LabelComponentDefaultPath).GetComponent<Text>();

            if (BtnLabel == null)
            {
                Debug.LogWarning($"Unable to find default component {LabelComponentDefaultPath} in gameObject hierarchy.");
            }
        }

        if (GoodLatencyIcon == null)
        {
            GoodLatencyIcon = transform.Find(GoodIconComponentDefaultPath).gameObject;

            if (GoodLatencyIcon == null)
            {
                Debug.LogWarning($"Unable to find default component {GoodIconComponentDefaultPath} in gameObject hierarchy.");
            }
        }

        if (MidLatencyIcon == null)
        {
            MidLatencyIcon = transform.Find(MidIconComponentDefaultPath).gameObject;

            if (MidLatencyIcon == null)
            {
                Debug.LogWarning($"Unable to find default component {MidIconComponentDefaultPath} in gameObject hierarchy.");
            }
        }

        if (PoorLatencyIcon == null)
        {
            PoorLatencyIcon = transform.Find(PoorIconComponentDefaultPath).gameObject;

            if (PoorLatencyIcon == null)
            {
                Debug.LogWarning($"Unable to find default component {PoorIconComponentDefaultPath} in gameObject hierarchy.");
            }
        }
    }

    public float GetPing()
    {
        return _ping;
    }

    public void SetLatencyIcon(float ping)
    {
        _ping = ping;

        if (ping < GoodThreshold)
        {
            GoodLatencyIcon.SetActive(true);
            MidLatencyIcon.SetActive(false);
            PoorLatencyIcon.SetActive(false);
        }
        else if (ping < MidThreshold)
        {
            GoodLatencyIcon.SetActive(false);
            MidLatencyIcon.SetActive(true);
            PoorLatencyIcon.SetActive(false);
        }
        else if (ping < PoorThreshold)
        {
            GoodLatencyIcon.SetActive(false);
            MidLatencyIcon.SetActive(false);
            PoorLatencyIcon.SetActive(true);
        }
        else
        {
            GoodLatencyIcon.SetActive(false);
            MidLatencyIcon.SetActive(false);
            PoorLatencyIcon.SetActive(false);
            GetComponent<Button>().interactable = false;
        }
    }

    public void DisableLatencyBtn()
    {
        _ping = -1;
        GoodLatencyIcon.SetActive(false);
        MidLatencyIcon.SetActive(false);
        PoorLatencyIcon.SetActive(false);
        GetComponent<Button>().interactable = false;
    }
}
