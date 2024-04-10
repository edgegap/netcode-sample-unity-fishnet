using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceEdge
{
    public class ScoreBoard : MonoBehaviour
    {
        public Action OnSpawnRequested;

        [SerializeField] private Button spawnButton;

        [SerializeField] private TextMeshProUGUI scoresText;

        private CanvasGroup group;

        public void SetActive(bool active)
        {

            if (active)
            {
                var scoreList = FindObjectsOfType<PlayerShipScore>();

                foreach (var score in scoreList)
                    scoresText.text += score.GetPlayerScore() + "\n";

                group.alpha = 1f;
            }
            else
            {
                group.alpha = 0f;
                scoresText.text = "";
            }

        }

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;

            spawnButton.onClick.AddListener(RequestSpawn);
        }

        private void RequestSpawn()
        {
            OnSpawnRequested?.Invoke();
            SetActive(false);
        }

    }
}