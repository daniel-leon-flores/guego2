using UnityEngine;

namespace GeometryDash.Game
{
    /// <summary>
    /// Horizontal follow locked to the target (no lag = no jitter) with an
    /// optional smoothed vertical follow.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Positive X keeps the player on the left side of the screen.")]
        [SerializeField] private float horizontalOffset = 5f;
        [SerializeField] private bool followVertical;
        [SerializeField] private float verticalOffset = 2f;
        [SerializeField, Min(0f)] private float verticalSmoothTime = 0.25f;

        private float fixedY;
        private float verticalVelocity;

        private void Awake()
        {
            fixedY = transform.position.y;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 position = transform.position;
            position.x = target.position.x + horizontalOffset;
            if (followVertical)
            {
                position.y = Mathf.SmoothDamp(position.y, target.position.y + verticalOffset,
                                              ref verticalVelocity, verticalSmoothTime);
            }
            else
            {
                position.y = fixedY;
            }
            transform.position = position;
        }
    }
}
