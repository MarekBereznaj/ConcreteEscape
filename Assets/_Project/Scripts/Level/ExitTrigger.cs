using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    [Tooltip("Tag, který má mít hráč (doporučeno: Player).")]
    public string playerTag = "Player";

    [Tooltip("Zastaví hru (Time.timeScale = 0) po dosažení cíle.")]
    public bool freezeTimeOnWin = false;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        // 🔒 zamkni jen když jsou coiny opravdu aktivní
        if (CoinManager.Instance != null &&
            CoinManager.Instance.requiredCoins > 0 &&
            !CoinManager.Instance.AllCollected)
        {
            Debug.Log($"EXIT LOCKED: {CoinManager.Instance.collectedCoins}/{CoinManager.Instance.requiredCoins} coins collected");
            return;
        }

        triggered = true;
        Debug.Log("YOU WIN! Player reached Exit.");

        if (freezeTimeOnWin)
            Time.timeScale = 0f;
    }
}
