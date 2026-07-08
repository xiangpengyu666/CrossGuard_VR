using UnityEngine;

namespace CrossGuard
{
    /// Melee boss brain: chases the player across the ground and plays an attack
    /// animation when close enough. Drives the Animator built for the Shyvana rig
    /// (Speed float -> Idle/Run blend, Attack trigger -> attack state).
    ///
    /// Deliberately NavMesh-free (direct steering) so it works on any flat arena
    /// without baking. When the real robot / NavMesh pathing lands it can be
    /// swapped behind IEnemyPoseSource; the animation-driving half stays the same.
    [RequireComponent(typeof(Animator))]
    public class BossChaser : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Left empty: auto-finds the GameObject tagged 'Player'.")]
        public Transform player;

        [Header("Movement")]
        public float moveSpeed = 3.5f;
        [Tooltip("How fast the body turns to face the player (deg/sec).")]
        public float turnSpeed = 360f;

        [Header("Attack")]
        [Tooltip("Distance at which the boss stops and attacks (world meters).")]
        public float attackRange = 2.5f;
        [Tooltip("Seconds between attack swings while in range.")]
        public float attackCooldown = 2f;

        Animator _anim;
        float _cooldown;

        static readonly int SpeedHash  = Animator.StringToHash("Speed");
        static readonly int AttackHash = Animator.StringToHash("Attack");

        void Awake()
        {
            _anim = GetComponent<Animator>();
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
        }

        void Update()
        {
            if (player == null) { _anim.SetFloat(SpeedHash, 0f); return; }

            // horizontal vector to the player (ignore height difference)
            Vector3 to = player.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;

            FacePlayer(to);

            _cooldown -= Time.deltaTime;

            if (dist > attackRange)
            {
                // chase: step straight toward the player, stay grounded (y unchanged)
                transform.position += to.normalized * moveSpeed * Time.deltaTime;
                _anim.SetFloat(SpeedHash, moveSpeed);
            }
            else
            {
                // in range: hold and swing on cooldown
                _anim.SetFloat(SpeedHash, 0f);
                if (_cooldown <= 0f)
                {
                    _anim.SetTrigger(AttackHash);
                    _cooldown = attackCooldown;
                }
            }
        }

        void FacePlayer(Vector3 to)
        {
            if (to.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
