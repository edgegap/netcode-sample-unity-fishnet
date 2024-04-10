using TMPro;
using UnityEngine;

namespace SpaceEdge
{
    public class NotificationSystem : MonoBehaviour
    {
        public static NotificationSystem Instance;

        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private float fadeSpeed;


        private void Awake()
        {
            if (Instance != null)
                Destroy(Instance);

            Instance = this;
        }

        private void Update()
        {
            if (notificationText.alpha > 0f)
                notificationText.alpha -= fadeSpeed * Time.deltaTime;
        }

        public void ShowNotification(string text)
        {
            notificationText.text = text;
            notificationText.alpha = 1;
        }
    }
}
