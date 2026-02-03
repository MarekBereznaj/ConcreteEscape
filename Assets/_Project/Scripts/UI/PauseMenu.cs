using UnityEngine;

namespace _Project.Scripts.UI
{
    public class PauseMenu : MonoBehaviour
    {
        public GameObject pausePanel;

        bool isPaused = false;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused)
                    Resume();
                else
                    Pause();
            }
        }

        public void Pause()
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
            isPaused = true;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
            isPaused = false;
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
