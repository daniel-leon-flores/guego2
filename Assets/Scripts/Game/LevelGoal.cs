using UnityEngine;

namespace GeometryDash.Game
{
    /// <summary>Finish line. Its collider must be a trigger.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }
    }
}
