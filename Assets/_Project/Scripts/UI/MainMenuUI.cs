using UnityEngine;

namespace _Project.Scripts.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public GameObject mainMenuPanel;
        public GameObject gameUIPanel;

        public void StartGame()
        {
            mainMenuPanel.SetActive(false);
            gameUIPanel.SetActive(true);
            Time.timeScale = 1f;
        }
    }
}
