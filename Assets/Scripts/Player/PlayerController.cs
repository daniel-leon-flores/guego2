using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GeometryDash.Player
{
    /// <summary>
    /// Auto-runner movement: constant horizontal speed, grounded-only jump and
    /// a visual spin while airborne that snaps to 90° steps on landing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 10f;

        [Header("Jump")]
        [SerializeField, Min(0.1f)] private float jumpHeight = 2.1f;
        [SerializeField, Min(0.1f)] private float gravityScale = 8f;
        [Tooltip("Holding the jump input jumps again as soon as the cube lands.")]
        [SerializeField] private bool holdToRepeatJump = true;
        [Tooltip("A press slightly before landing is still honored (seconds).")]
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayers;
        [SerializeField, Min(0.001f)] private float groundCheckDistance = 0.05f;

        [Header("Visual Rotation")]
        [Tooltip("Child transform holding the sprite; only the visual rotates, never the collider.")]
        [SerializeField] private Transform visual;
        [SerializeField] private float degreesPerJump = 180f;
        [SerializeField, Min(0f)] private float landingSnapSpeed = 1080f;

        private Rigidbody2D body;
        private BoxCollider2D boxCollider;
        private float jumpBufferCounter;
        private bool isGrounded;
        private bool controlsEnabled = true;

        public bool IsGrounded => isGrounded;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        private float Gravity => Mathf.Abs(Physics2D.gravity.y) * gravityScale;
        private float JumpVelocity => Mathf.Sqrt(2f * Gravity * jumpHeight);
        private float AirSpinSpeed => degreesPerJump / (2f * JumpVelocity / Gravity);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();
            body.gravityScale = gravityScale;
            body.freezeRotation = true;
            if (visual == null) visual = transform;
        }

        private void Update()
        {
            if (!controlsEnabled) return;

            jumpBufferCounter = IsJumpInputActive() ? jumpBufferTime : jumpBufferCounter - Time.deltaTime;
            UpdateVisualRotation();
        }

        private void FixedUpdate()
        {
            if (!controlsEnabled) return;

            Vector2 velocity = body.linearVelocity;
            isGrounded = CheckGrounded(velocity.y);
            velocity.x = moveSpeed;

            if (isGrounded && jumpBufferCounter > 0f)
            {
                velocity.y = JumpVelocity;
                jumpBufferCounter = 0f;
                isGrounded = false;
            }

            body.linearVelocity = velocity;
        }

        /// <summary>Freezes the player in place (death / level complete).</summary>
        public void Stop()
        {
            controlsEnabled = false;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        /// <summary>Safe to call before Awake: physics syncs from the transform.</summary>
        public void PlaceAt(Vector3 position)
        {
            transform.position = position;
        }

        private bool CheckGrounded(float verticalVelocity)
        {
            // Moving upwards means we just jumped; prevents re-jumping on the same frame.
            if (verticalVelocity > 0.01f) return false;

            Bounds bounds = boxCollider.bounds;
            Vector2 size = new Vector2(bounds.size.x * 0.9f, bounds.size.y);
            RaycastHit2D hit = Physics2D.BoxCast(bounds.center, size, 0f, Vector2.down, groundCheckDistance, groundLayers);
            return hit.collider != null;
        }

        private bool IsJumpInputActive()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            bool keyPressed = keyboard != null &&
                              (holdToRepeatJump ? keyboard.spaceKey.isPressed : keyboard.spaceKey.wasPressedThisFrame);
            bool clickPressed = mouse != null &&
                                (holdToRepeatJump ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame) &&
                                !IsPointerOverUI();
            return keyPressed || clickPressed;
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void UpdateVisualRotation()
        {
            float z = visual.localEulerAngles.z;
            if (isGrounded)
            {
                float snapped = Mathf.Round(z / 90f) * 90f;
                z = Mathf.MoveTowardsAngle(z, snapped, landingSnapSpeed * Time.deltaTime);
            }
            else
            {
                z -= AirSpinSpeed * Time.deltaTime;
            }
            visual.localRotation = Quaternion.Euler(0f, 0f, z);
        }
    }
}
