using UnityEngine;

namespace CrossGuard
{
    /// First-person WASD + mouse-look controller for the pure-Unity prototype.
    ///
    /// VR NOTE (work plan: "keyboard-to-VR conversion"):
    /// All input is isolated in ReadMoveInput()/ReadLookInput(). To go to Quest 3
    /// later you replace only those two methods (thumbstick + head pose) and swap
    /// the camera for an XR Origin. Movement, collision and arena bounds stay
    /// engine-driven so they're identical on desktop and on-headset.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 4.5f;
        [Tooltip("Speed multiplier while the sprint key is held.")]
        public float sprintMultiplier = 1.8f;
        public float gravity = -9.81f;
        [Tooltip("Upward launch velocity for a jump (m/s).")]
        public float jumpSpeed = 5f;

        [Header("Look")]
        public float mouseSensitivity = 2f;
        public Transform cameraPivot;   // child camera; pitch applied here
        public float maxPitch = 85f;

        CharacterController _cc;
        float _pitch;
        Vector3 _velocity;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            HandleLook();
            HandleMove();
        }

        // --- input isolation: the methods XR replaces (thumbstick / buttons) ---
        Vector2 ReadMoveInput() => new Vector2(Input.GetAxisRaw("Horizontal"),
                                               Input.GetAxisRaw("Vertical"));
        Vector2 ReadLookInput() => new Vector2(Input.GetAxis("Mouse X"),
                                               Input.GetAxis("Mouse Y"));
        bool ReadSprintInput() => Input.GetKey(KeyCode.LeftShift);
        bool ReadJumpInput()   => Input.GetButtonDown("Jump");   // Space by default

        void HandleLook()
        {
            Vector2 look = ReadLookInput() * mouseSensitivity;
            transform.Rotate(Vector3.up, look.x);                 // yaw on the body
            _pitch = Mathf.Clamp(_pitch - look.y, -maxPitch, maxPitch);
            if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void HandleMove()
        {
            Vector2 m = ReadMoveInput();
            Vector3 move = transform.right * m.x + transform.forward * m.y;
            if (move.sqrMagnitude > 1f) move.Normalize();

            float speed = moveSpeed * (ReadSprintInput() ? sprintMultiplier : 1f);
            _velocity.x = move.x * speed;
            _velocity.z = move.z * speed;

            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2f;   // stick to the ground
                if (ReadJumpInput()) _velocity.y = jumpSpeed;
            }
            _velocity.y += gravity * Time.deltaTime;

            _cc.Move(_velocity * Time.deltaTime);
        }
    }
}
