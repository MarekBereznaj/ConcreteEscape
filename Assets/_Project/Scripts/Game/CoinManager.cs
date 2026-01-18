using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    public int requiredCoins { get; private set; }
    public int collectedCoins { get; private set; }

    // pokud requiredCoins == 0, nic se nezamyká
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
        Debug.Log($"Coins: {collectedCoins}/{requiredCoins}");
    }

    public void CollectOne()
    {
        collectedCoins++;
        Debug.Log($"Coins: {collectedCoins}/{requiredCoins}");
    }
}
