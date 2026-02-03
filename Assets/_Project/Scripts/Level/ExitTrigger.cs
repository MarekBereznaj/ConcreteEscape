using System.Collections;
using _Project.Scripts.Game;
using _Project.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Level
{
    public class ExitTrigger : MonoBehaviour
    {
        [Header("Settings")]
        public string playerTag = "Player";
        public bool freezeTimeOnWin = true;
        
        [Header("UI")]
        public TextMeshProUGUI infoText;
        public GameObject winPanel; // Panel s nápisem "YOU WIN"
        public TextMeshProUGUI winText; // Text na win panelu
        public float messageDuration = 2f;

        [Header("Win Actions")]
        public bool loadNextLevel = false;
        public string nextLevelName = "";
        public float delayBeforeNextLevel = 3f;

        private bool triggered = false;
        private bool showingMessage = false;
        
        [SerializeField] private MonoBehaviour playerController;
        [SerializeField] private GameTimer gameTimer;

        private void Start()
        {
            // Ujisti se, že win panel je vypnutý na začátku
            if (winPanel != null)
                winPanel.SetActive(false);
                
            if (infoText != null)
                infoText.gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            // Kontrola CoinManager
            if (CoinManager.Instance == null)
            {
                Debug.LogWarning("CoinManager not found! Winning without coin check.");
                TriggerWin();
                return;
            }

            // Pokud jsou vyžadovány coiny
            if (CoinManager.Instance.requiredCoins > 0)
            {
                // Pokud nejsou všechny coiny sebrané
                if (!CoinManager.Instance.AllCollected)
                {
                    if (!showingMessage)
                        StartCoroutine(ShowLockedMessage());
                    return;
                }
            }

            // VÝHRA!
            TriggerWin();
        }

        private void TriggerWin()
        {
            if (triggered) return;
            triggered = true;

            if (infoText != null)
                infoText.gameObject.SetActive(false);

            Debug.Log("🎉 YOU WIN!");

            if (gameTimer == null)
            {
                Debug.LogError("GameTimer NOT assigned in ExitTrigger!");
                return;
            }

            gameTimer.ShowEndScreen(true);
        }

        
        private void QuitGame()
        {
            Time.timeScale = 1f;
            Application.Quit();
        }
        private IEnumerator QuitAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2f);
            QuitGame();
        }

        public void SetGameTimer(GameTimer timer)
        {
            gameTimer = timer;
        }
        
        public void SetInfoText(TextMeshProUGUI text)
        {
            infoText = text;
        }

        private void ShowWinScreen()
        {
            if (infoText != null)
                infoText.gameObject.SetActive(false);

            if (winPanel != null)
                winPanel.SetActive(true);

            if (winText != null)
            {
                winText.gameObject.SetActive(true);
                winText.color = Color.red;
                winText.fontSizeMax = 50;
                winText.fontStyle = FontStyles.Bold;
                winText.alpha = 1f;

                if (CoinManager.Instance != null)
                {
                    winText.text =
                        "<b>YOU WIN!</b>\n\n" +
                        $"SCORE: {CoinManager.Instance.collectedCoins}/{CoinManager.Instance.requiredCoins}";
                }
                else
                {
                    winText.text = "YOU WIN!";
                }
            }
        }

        private IEnumerator ShowLockedMessage()
        {
            showingMessage = true;

            if (infoText != null && CoinManager.Instance != null)
            {
                infoText.text = $"Collect all coins first!\n{CoinManager.Instance.collectedCoins}/{CoinManager.Instance.requiredCoins}";
                infoText.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(messageDuration);

            if (infoText != null)
                infoText.gameObject.SetActive(false);

            showingMessage = false;
        }

        private IEnumerator LoadNextLevelRoutine()
        {
            yield return new WaitForSecondsRealtime(delayBeforeNextLevel);
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextLevelName);
        }

        // Pro button v UI
        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Pro button v UI
        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}