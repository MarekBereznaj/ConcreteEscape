using TMPro;
using UnityEngine;

namespace _Project.Scripts.UI
{
    public class GameTimer : MonoBehaviour
    {
        public float timeLeft = 120f;
        public TextMeshProUGUI timerText;

        [Header("End Game UI")]
        public GameObject gameOverPanel;
        public TextMeshProUGUI gameOverText;

        bool gameEnded = false;

        void Update()
        {
            if (gameEnded)
                return;

            timeLeft -= Time.deltaTime;
            timerText.text = "Time: " + Mathf.Ceil(timeLeft);

            if (timeLeft <= 0f)
            {
                ShowEndScreen(false);
            }
        }

        public void ShowEndScreen(bool won)
        {
            if (gameEnded)
                return;

            gameEnded = true;

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            gameOverPanel.SetActive(true);

            if (won)
            {
                gameOverText.text = "YOU WIN";
                gameOverText.color = Color.green;
            }
            else
            {
                gameOverText.text = "GAME OVER";
                gameOverText.color = Color.white;
            }
        }
    }
}