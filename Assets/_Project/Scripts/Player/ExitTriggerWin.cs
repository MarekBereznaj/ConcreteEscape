using UnityEngine;

namespace _Project.Scripts.Player
{
    public class ExitTriggerWin : MonoBehaviour
    {
        [SerializeField] private GameObject winCanvas;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            var movement = other.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (winCanvas != null)
                winCanvas.SetActive(true);
        }
    }
}