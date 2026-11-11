using GeometryDash.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryDash.UI
{
    /// <summary>
    /// In-game UI. Shows the panel matching the GameManager state and wires
    /// button clicks by code so no OnClick setup is needed in the Inspector.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LevelManager levelManager;

        [Header("Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject levelCompletePanel;

        [Header("Texts")]
        [SerializeField] private TMP_Text hudProgressText;
        [SerializeField] private TMP_Text gameOverProgressText;
        [SerializeField] private TMP_Text levelCompleteProgressText;

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button[] retryButtons;
        [SerializeField] private Button[] mainMenuButtons;

        private int displayedPercent = -1;

        private void Awake()
        {
            pauseButton.onClick.AddListener(gameManager.Pause);
            resumeButton.onClick.AddListener(gameManager.Resume);
            foreach (Button button in retryButtons) button.onClick.AddListener(gameManager.Retry);
            foreach (Button button in mainMenuButtons) button.onClick.AddListener(gameManager.LoadMainMenu);
        }

        private void OnEnable() => gameManager.StateChanged += ShowState;

        private void OnDisable() => gameManager.StateChanged -= ShowState;

        private void Start() => ShowState(gameManager.State);

        private void Update()
        {
            int percent = levelManager.ProgressPercent;
            if (percent == displayedPercent) return;

            displayedPercent = percent;
            hudProgressText.text = FormatPercent(percent);
        }

        private void ShowState(GameState state)
        {
            hudPanel.SetActive(state == GameState.Playing || state == GameState.Paused);
            pausePanel.SetActive(state == GameState.Paused);
            gameOverPanel.SetActive(state == GameState.GameOver);
            levelCompletePanel.SetActive(state == GameState.LevelComplete);
            pauseButton.interactable = state == GameState.Playing;

            string result = FormatPercent(levelManager.ProgressPercent);
            if (state == GameState.GameOver) gameOverProgressText.text = $"Progress: {result}";
            if (state == GameState.LevelComplete) levelCompleteProgressText.text = $"Completed: {result}";
        }

        private static string FormatPercent(int percent) => $"{percent}%";
    }
}
