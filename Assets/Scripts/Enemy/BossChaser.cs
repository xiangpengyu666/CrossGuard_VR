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
        [Tooltip("Damage dealt to the player if they're inside the attack shape at the " +
                 "impact (flash) moment.")]
        public float attackDamage = 15f;

        [Header("Telegraph")]
        [Tooltip("Optional: ground attack-range indicators shown per attack.")]
        public BossTelegraph telegraph;

        [Header("Transform (demon form — triggered when life 1 is depleted)")]
        [Tooltip("Length of the transform intro (Taunt_Intro, played at 0.5x). Boss is " +
                 "locked & invulnerable while it plays, then enters the demon form.")]
        public float transformDuration = 5.6f;
        [Tooltip("Lower the boss by this many meters once the demon form settles.")]
        public float transformHeightDrop = 0.3f;

        [Header("Phase-2 buff (applied when the demon form settles)")]
        public float buffDamageMul = 1.8f;
        public float buffSpeedMul = 1.3f;
        public float buffCooldownMul = 0.65f;

        Animator _anim;
        PlayerHealth _playerHealth;
        BossHealth _bossHealth;
        float _cooldown;
        bool _attacking;
        float _attackTimer;
        bool _hitResolved;
        float _impactAt;
        int _pendingShape;
        bool _transforming;
        bool _transformed;
        float _transformTimer;

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
            if (player != null) _playerHealth = player.GetComponent<PlayerHealth>();
            _bossHealth = GetComponent<BossHealth>();
        }

        void OnEnable()
        {
            if (_bossHealth != null) _bossHealth.OnPhaseAdvance += StartTransform;
        }

        void OnDisable()
        {
            if (_bossHealth != null) _bossHealth.OnPhaseAdvance -= StartTransform;
        }

        // Life 1 depleted -> play the transform, locked & invulnerable.
        void StartTransform()
        {
            if (_transforming || _transformed) return;
            _anim.Play(TransformStateHash, 0, 0f);
            _anim.SetFloat(SpeedHash, 0f);
            _transforming = true;
            _transformTimer = transformDuration;
            _attacking = false;
            if (_bossHealth != null) _bossHealth.Invulnerable = true;
        }

        void ApplyPhase2Buff()
        {
            attackDamage   *= buffDamageMul;
            moveSpeed      *= buffSpeedMul;
            attackCooldown *= buffCooldownMul;
        }

        void Update()
        {
            if (player == null) { _anim.SetFloat(SpeedHash, 0f); return; }

            // While attacking, the boss is frozen where the swing started: no moving,
            // no turning, until the attack animation finishes.
            if (_attacking)
            {
                _anim.SetFloat(SpeedHash, 0f);
                // Resolve the hit exactly at the impact/flash moment (the telegraph's
                // strike point). If the player is inside the attack shape, deal damage
                // -> PlayerHealth raises OnPlayerHit (SEAM #1 -> future haptic band).
                float elapsed = attackDuration - _attackTimer;
                if (!_hitResolved && elapsed >= _impactAt)
                {
                    _hitResolved = true;
                    ResolveAttackHit();
                }
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f) _attacking = false;
                return;
            }

            // Transforming: locked & invulnerable through Taunt_Intro, then enter the
            // buffed demon form and refill the bar for phase 2.
            if (_transforming)
            {
                _anim.SetFloat(SpeedHash, 0f);
                _transformTimer -= Time.deltaTime;
                if (_transformTimer <= 0f)
                {
                    _transforming = false;
                    _transformed = true;
                    transform.position -= new Vector3(0f, transformHeightDrop, 0f);
                    ApplyPhase2Buff();
                    if (_bossHealth != null)
                    {
                        _bossHealth.Invulnerable = false;
                        _bossHealth.BeginNextPhase();   // refill to full for life 2
                    }
                }
                return;
            }

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
                // in range: hold and swing on cooldown, picking a random attack for
                // the current form (normal Q1/2/3 or demon ULT_Q1/2/3).
                _anim.SetFloat(SpeedHash, 0f);
                if (_cooldown <= 0f)
                {
                    string[] pool = _transformed ? DemonAttacks : NormalAttacks;
                    string atk = pool[Random.Range(0, pool.Length)];
                    _anim.Play(atk, 0, 0f);
                    // Q1/ULT_Q1 -> rect, Q2 -> arc, Q3 -> circle
                    _pendingShape = atk.EndsWith("Q1") ? 0 : atk.EndsWith("Q2") ? 1 : 2;
                    if (telegraph != null) telegraph.Show(_pendingShape, attackDuration, atk);
                    _cooldown = attackCooldown;
                    _attacking = true;          // lock position for the swing
                    _attackTimer = attackDuration;
                    _hitResolved = false;
                    _impactAt = attackDuration *
                                (telegraph != null ? telegraph.impactFraction : 0.45f);
                }
            }
        }

        void ResolveAttackHit()
        {
            if (_playerHealth == null) return;
            bool hit;
            if (telegraph != null)
                hit = telegraph.IsPointInShape(_pendingShape, player.position);
            else
                hit = Vector3.Distance(transform.position, player.position) <= attackRange;

            if (hit)
                _playerHealth.TakeDamage(attackDamage, transform.position);
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
