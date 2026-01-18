using TMPro;
using UnityEngine;

namespace _Project.Scripts.UI
{
    public class GameTimer : MonoBehaviour
    {
        public float timeLeft = 120f;
        public TextMeshProUGUI timerText;
        public GameObject gameOverPanel;

        bool gameEnded = false;

        void Update()
        {
            if (gameEnded)
                return;

            timeLeft -= Time.deltaTime;
            timerText.text = "Time: " + Mathf.Ceil(timeLeft);

            if (timeLeft <= 0f)
            {
                GameOver();
            }
        }

        void GameOver()
        {
            gameEnded = true;
            Time.timeScale = 0f;
            gameOverPanel.SetActive(true);
        }
    }
}
