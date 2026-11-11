using System.Collections;
using UnityEngine;

namespace GeometryDash.Obstacles
{
    public enum ObstacleType
    {
        Block,
        Spike
    }

    /// <summary>
    /// Marks a lethal object. Place prefabs manually in the level; LevelManager
    /// reveals them progressively as the player approaches.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Obstacle : MonoBehaviour
    {
        [SerializeField] private ObstacleType type = ObstacleType.Block;
        [SerializeField, Min(0f)] private float revealDuration = 0.2f;

        private Vector3 originalScale;

        public ObstacleType Type => type;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Reveal()
        {
            gameObject.SetActive(true);
            if (revealDuration > 0f) StartCoroutine(ScaleIn());
        }

        private IEnumerator ScaleIn()
        {
            for (float t = 0f; t < revealDuration; t += Time.deltaTime)
            {
                transform.localScale = originalScale * Mathf.SmoothStep(0f, 1f, t / revealDuration);
                yield return null;
            }
            transform.localScale = originalScale;
        }
    }
}
