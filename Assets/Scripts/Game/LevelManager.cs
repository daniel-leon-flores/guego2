using System;
using GeometryDash.Obstacles;
using GeometryDash.Player;
using UnityEngine;

namespace GeometryDash.Game
{
    /// <summary>
    /// Level configuration: spawn point, goal, scroll speed, progress tracking
    /// and progressive reveal of the obstacles placed under obstaclesRoot.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform startPoint;
        [SerializeField] private LevelGoal goal;
        [SerializeField, Min(0.1f)] private float scrollSpeed = 10f;

        [Header("Progressive Reveal")]
        [SerializeField] private Transform obstaclesRoot;
        [Tooltip("Obstacles appear when they are this far ahead of the player (world units).")]
        [SerializeField, Min(0f)] private float revealDistanceAhead = 12f;

        private Obstacle[] obstacles = Array.Empty<Obstacle>();
        private int nextObstacleToReveal;
        private float levelLength;

        /// <summary>Normalized progress in [0, 1]; never decreases.</summary>
        public float Progress01 { get; private set; }

        public int ProgressPercent => Mathf.FloorToInt(Progress01 * 100f);

        private void Awake()
        {
            player.PlaceAt(startPoint.position);
            player.MoveSpeed = scrollSpeed;
            levelLength = Mathf.Max(0.01f, goal.transform.position.x - startPoint.position.x);
        }

        private void Start()
        {
            if (obstaclesRoot == null) return;

            obstacles = obstaclesRoot.GetComponentsInChildren<Obstacle>(true);
            Array.Sort(obstacles, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            foreach (Obstacle obstacle in obstacles) obstacle.Hide();
            RevealUpcomingObstacles(instant: true);
        }

        private void Update()
        {
            float progress = (player.transform.position.x - startPoint.position.x) / levelLength;
            Progress01 = Mathf.Max(Progress01, Mathf.Clamp01(progress));
            RevealUpcomingObstacles(instant: false);
        }

        public void MarkCompleted()
        {
            Progress01 = 1f;
        }

        private void RevealUpcomingObstacles(bool instant)
        {
            float revealX = player.transform.position.x + revealDistanceAhead;
            while (nextObstacleToReveal < obstacles.Length &&
                   obstacles[nextObstacleToReveal].transform.position.x <= revealX)
            {
                Obstacle obstacle = obstacles[nextObstacleToReveal++];
                if (instant) obstacle.gameObject.SetActive(true);
                else obstacle.Reveal();
            }
        }
    }
}
