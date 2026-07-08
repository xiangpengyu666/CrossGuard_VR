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
        [Tooltip("How long the attack animation runs. The boss is locked in place " +
                 "(no move, no turn) from the moment the attack fires until this elapses.")]
        public float attackDuration = 1f;

        [Header("Telegraph")]
        [Tooltip("Optional: ground attack-range indicators shown per attack.")]
        public BossTelegraph telegraph;

        [Header("Transform (demon form)")]
        [Tooltip("If true, the boss transforms once after being engaged this long.")]
        public bool canTransform = true;
        [Tooltip("Seconds of combat before the one-time transformation triggers.")]
        public float transformAfterSeconds = 6f;
        [Tooltip("Length of the transform intro (Taunt_Intro). Boss is locked in " +
                 "place while it plays, then holds in the demon idle.")]
        public float transformDuration = 2.8f;
        [Tooltip("Lower the boss by this many meters once the demon form settles " +
                 "(the demon idle pose sits lower).")]
        public float transformHeightDrop = 0.3f;

        Animator _anim;
        float _cooldown;
        bool _attacking;
        float _attackTimer;
        bool _transforming;
        bool _transformed;
        float _transformTimer;
        float _engaged;

        static readonly int SpeedHash          = Animator.StringToHash("Speed");
        static readonly int TransformStateHash = Animator.StringToHash("Transform");

        // attack states to pick from at random, by form
        static readonly string[] NormalAttacks = { "Q1", "Q2", "Q3" };
        static readonly string[] DemonAttacks  = { "ULT_Q1", "ULT_Q2", "ULT_Q3" };

        void Awake()
        {
            _anim = GetComponent<Animator>();
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            if (telegraph != null && telegraph.bossAnimator == null)
                telegraph.bossAnimator = _anim;
        }

        void Update()
        {
            if (player == null) { _anim.SetFloat(SpeedHash, 0f); return; }

            // While attacking, the boss is frozen where the swing started: no moving,
            // no turning, until the attack animation finishes.
            if (_attacking)
            {
                _anim.SetFloat(SpeedHash, 0f);
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f) _attacking = false;
                return;
            }

            // Transforming: locked in place through the Taunt_Intro, then flip to the
            // demon form (holds in ULT_Idle).
            if (_transforming)
            {
                _anim.SetFloat(SpeedHash, 0f);
                _transformTimer -= Time.deltaTime;
                if (_transformTimer <= 0f)
                {
                    _transforming = false;
                    _transformed = true;
                    transform.position -= new Vector3(0f, transformHeightDrop, 0f);
                }
                return;
            }

            // horizontal vector to the player (ignore height difference)
            Vector3 to = player.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;

            FacePlayer(to);

            // one-time transformation after enough time engaged
            if (canTransform && !_transformed)
            {
                _engaged += Time.deltaTime;
                if (_engaged >= transformAfterSeconds)
                {
                    // Play the state directly (robust) rather than a trigger; the
                    // Transform state auto-advances to the looping demon idle.
                    _anim.Play(TransformStateHash, 0, 0f);
                    _transforming = true;
                    _transformTimer = transformDuration;
                    _anim.SetFloat(SpeedHash, 0f);
                    return;
                }
            }

            _cooldown -= Time.deltaTime;

            if (dist > attackRange)
            {
                // chase: step straight toward the player, stay grounded (y unchanged)
                transform.position += to.normalized * moveSpeed * Time.deltaTime;
                _anim.SetFloat(SpeedHash, moveSpeed);
            }
            else
            {
                // in range: hold and swing on cooldown, picking a random attack for
                // the current form (normal Q1/2/3 or demon ULT_Q1/2/3).
                _anim.SetFloat(SpeedHash, 0f);
                if (_cooldown <= 0f)
                {
                    string[] pool = _transformed ? DemonAttacks : NormalAttacks;
                    string atk = pool[Random.Range(0, pool.Length)];
                    _anim.Play(atk, 0, 0f);
                    if (telegraph != null)
                    {
                        // Q1/ULT_Q1 -> rect, Q2 -> arc, Q3 -> circle
                        int shape = atk.EndsWith("Q1") ? 0 : atk.EndsWith("Q2") ? 1 : 2;
                        telegraph.Show(shape, attackDuration, atk);
                    }
                    _cooldown = attackCooldown;
                    _attacking = true;          // lock position for the swing
                    _attackTimer = attackDuration;
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
