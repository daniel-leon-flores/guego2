using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeometryDash.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private string firstLevelSceneName = "Level01";

        private void Awake()
        {
            Time.timeScale = 1f;
            playButton.onClick.AddListener(Play);
            exitButton.onClick.AddListener(Exit);
        }

        private void Play()
        {
            SceneManager.LoadScene(firstLevelSceneName);
        }

        private static void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
