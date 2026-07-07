using UnityEngine;

namespace CrossGuard
{
    /// Physics-based WASD mover for TestCube. Drives an existing Rigidbody with
    /// AddForce so the cube accelerates, coasts (inertia) and collides/bounces off
    /// arena geometry using the real physics solver — unlike CharacterController.
    ///
    /// Input is read in Update (no missed key events) and applied in FixedUpdate
    /// (physics runs on the fixed timestep). Movement is relative to `cameraPivot`
    /// if assigned, otherwise world axes.
    [RequireComponent(typeof(Rigidbody))]
    public class TestCubeMover : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Acceleration force applied while a key is held.")]
        public float acceleration = 40f;
        [Tooltip("Horizontal speed cap; above this no more force is added.")]
        public float maxSpeed = 6f;
        [Tooltip("Higher = snappier stop when keys are released (sets Rigidbody linear damping).")]
        public float damping = 4f;

        [Header("Orientation (optional)")]
        [Tooltip("Move relative to this transform's facing (e.g. camera). Leave empty for world axes.")]
        public Transform cameraPivot;

        Rigidbody _rb;
        Vector2 _input;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.linearDamping = damping;                 // gives the coast/stop feel
            _rb.constraints |= RigidbodyConstraints.FreezeRotationX
                             | RigidbodyConstraints.FreezeRotationZ; // don't tip over
        }

        void Update()
        {
            _input.x = Input.GetAxisRaw("Horizontal");
            _input.y = Input.GetAxisRaw("Vertical");
        }

        void FixedUpdate()
        {
            // Build a horizontal move direction from the reference frame.
            Vector3 fwd = cameraPivot ? cameraPivot.forward : Vector3.forward;
            Vector3 right = cameraPivot ? cameraPivot.right : Vector3.right;
            fwd.y = 0f; right.y = 0f;
            fwd.Normalize(); right.Normalize();

            Vector3 dir = right * _input.x + fwd * _input.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            // Only add force below the speed cap (measured on the horizontal plane).
            Vector3 horizVel = _rb.linearVelocity;
            horizVel.y = 0f;
            if (horizVel.magnitude < maxSpeed)
                _rb.AddForce(dir * acceleration, ForceMode.Acceleration);
        }
    }
}
