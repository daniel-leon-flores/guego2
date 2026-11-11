using System;
using GeometryDash.Game;
using GeometryDash.Obstacles;
using UnityEngine;

namespace GeometryDash.Player
{
    /// <summary>
    /// Classifies contacts: obstacles kill, the goal completes the level.
    /// Ground contacts are ignored here (handled by PlayerController's ground check).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCollision : MonoBehaviour
    {
        public event Action Died;
        public event Action GoalReached;

        private bool outcomeResolved;

        private void OnCollisionEnter2D(Collision2D collision) => HandleContact(collision.collider);

        private void OnTriggerEnter2D(Collider2D other) => HandleContact(other);

        private void HandleContact(Collider2D other)
        {
            if (outcomeResolved) return;

            if (other.GetComponentInParent<Obstacle>() != null)
            {
                outcomeResolved = true;
                Died?.Invoke();
            }
            else if (other.GetComponentInParent<LevelGoal>() != null)
            {
                outcomeResolved = true;
                GoalReached?.Invoke();
            }
        }
    }
}
