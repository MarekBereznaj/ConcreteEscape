using TMPro;
using UnityEngine;

namespace _Project.Scripts.Game
{
    public class CoinManager : MonoBehaviour
    {
        public static CoinManager Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI pointsText;

        public int requiredCoins { get; private set; }
        public int collectedCoins { get; private set; }

        public bool AllCollected => requiredCoins > 0 && collectedCoins >= requiredCoins;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ResetRun(int required)
        {
            requiredCoins = Mathf.Max(0, required);
            collectedCoins = 0;
            UpdateUI();
            Debug.Log($"Coins: {collectedCoins}/{requiredCoins}");
        }

        public void CollectOne()
        {
            collectedCoins++;
            UpdateUI();
            Debug.Log($"Coins: {collectedCoins}/{requiredCoins}");
        }
        private void UpdateUI()
        {
            if (pointsText != null)
                pointsText.text = $"SCORE: {collectedCoins}";
        }
    }
}
