using UnityEngine;

namespace CrossGuard
{
    /// Attack + hit logic for the enemy robot. Pose/movement is owned by an
    /// IEnemyPoseSource on the same GameObject (LocalAiPoseSource now, network
    /// later), so this script is identical for the virtual and co-localized builds.
    ///
    /// Attack cycle:  in range -> warning telegraph (haptic pulse) -> fire projectile.
    public class EnemyRobot : MonoBehaviour
    {
        [Header("Refs")]
        public Transform player;
        public Transform muzzle;                 // where projectiles spawn
        public Projectile projectilePrefab;

        [Header("Ranged attack")]
        public float attackRange = 10f;
        public float fireInterval = 2f;
        public float telegraphTime = 0.5f;       // warning window before firing
        public float projectileDamage = 12f;

        IEnemyPoseSource _pose;                  // reserved for aim/pose queries
        PlayerHealth _playerHealth;
        float _cooldown;
        bool _telegraphing;
        float _telegraphTimer;

        void Awake()
        {
            _pose = GetComponent<IEnemyPoseSource>();
            if (player) _playerHealth = player.GetComponent<PlayerHealth>();
        }

        void Update()
        {
            if (!player) return;
            float dist = Vector3.Distance(transform.position, player.position);

            if (_telegraphing)
            {
                _telegraphTimer -= Time.deltaTime;
                if (_telegraphTimer <= 0f) Fire();
                return;
            }

            _cooldown -= Time.deltaTime;
            if (dist <= attackRange && _cooldown <= 0f)
                BeginTelegraph();
        }

        void BeginTelegraph()
        {
            _telegraphing = true;
            _telegraphTimer = telegraphTime;
            // Warning haptic: tell the player an attack is incoming (SEAM #1).
            if (_playerHealth) _playerHealth.SignalWarning(transform.position);
        }

        void Fire()
        {
            _telegraphing = false;
            _cooldown = fireInterval;

            if (!projectilePrefab || !muzzle) return;

            Vector3 dir = (player.position - muzzle.position).normalized;
            Projectile p = Instantiate(projectilePrefab, muzzle.position,
                                       Quaternion.LookRotation(dir));
            p.Launch(dir, projectileDamage, gameObject);
        }
    }
}
