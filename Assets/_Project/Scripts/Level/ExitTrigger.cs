using System.Collections;
using _Project.Scripts.Game;
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

            Debug.Log("🎉 YOU WIN!");

            // Zobrazit win screen
            ShowWinScreen();

            // Zmrazit čas
            if (freezeTimeOnWin)
            {
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Načíst další level
            if (loadNextLevel && !string.IsNullOrEmpty(nextLevelName))
            {
                StartCoroutine(LoadNextLevelRoutine());
            }
        }

        private void ShowWinScreen()
        {
            // Skrýt info text
            if (infoText != null)
                infoText.gameObject.SetActive(false);

            // Zobrazit win panel
            if (winPanel != null)
            {
                winPanel.SetActive(true);
                
                if (winText != null)
                {
                    if (CoinManager.Instance != null)
                    {
                        winText.text = $"YOU WIN!\n\nSCORE: {CoinManager.Instance.collectedCoins}/{CoinManager.Instance.requiredCoins}";
                    }
                    else
                    {
                        winText.text = "YOU WIN!";
                    }
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