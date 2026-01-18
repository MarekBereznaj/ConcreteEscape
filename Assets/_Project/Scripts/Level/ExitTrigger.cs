using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    [Tooltip("Tag, který má mít hráč (doporučeno: Player).")]
    public string playerTag = "Player";

    [Tooltip("Zastaví hru (Time.timeScale = 0) po dosažení cíle.")]
    public bool freezeTimeOnWin = true;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        // varianta 1: přes tag
        if (!other.CompareTag(playerTag)) return;

        triggered = true;

        Debug.Log("YOU WIN! Player reached Exit.");

        if (freezeTimeOnWin)
            Time.timeScale = 0f;
    }
}
