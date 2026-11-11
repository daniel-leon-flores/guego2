using System;
using GeometryDash.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GeometryDash.Game
{
    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        LevelComplete
    }

    /// <summary>
    /// Owns the match state and scene flow. Listens to the player's outcome
    /// events and notifies the UI through StateChanged.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerCollision playerCollision;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        public event Action<GameState> StateChanged;

        public GameState State { get; private set; } = GameState.Playing;

        private void OnEnable()
        {
            playerCollision.Died += HandlePlayerDied;
            playerCollision.GoalReached += HandleGoalReached;
        }

        private void OnDisable()
        {
            playerCollision.Died -= HandlePlayerDied;
            playerCollision.GoalReached -= HandleGoalReached;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void HandlePlayerDied()
        {
            if (State != GameState.Playing) return;
            player.Stop();
            SetState(GameState.GameOver);
        }

        private void HandleGoalReached()
        {
            if (State != GameState.Playing) return;
            levelManager.MarkCompleted();
            player.Stop();
            SetState(GameState.LevelComplete);
        }

        private void SetState(GameState newState)
        {
            State = newState;
            StateChanged?.Invoke(newState);
        }
    }
}
