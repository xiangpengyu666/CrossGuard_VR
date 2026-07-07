using UnityEngine;

namespace CrossGuard
{
    /// Player damage sink. Every hit routes through here and raises OnPlayerHit
    /// (SEAM #1), so the game UI and Cindy's haptic band react to the same signal.
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health")]
        public float maxHealth = 100f;
        [SerializeField] float _current;

        [Header("Hit classification")]
        [Tooltip("Damage at or above this counts as a Heavy hit.")]
        public float heavyThreshold = 20f;

        void Awake()
        {
            _current = maxHealth;
            GameEvents.RaisePlayerHealthChanged(_current, maxHealth);
        }

        /// Called by enemy attacks / projectiles.
        public void TakeDamage(float damage, Vector3 sourcePosition)
        {
            _current = Mathf.Max(0f, _current - damage);

            HitType type = damage >= heavyThreshold ? HitType.Heavy : HitType.Light;
            Vector3 dir = (transform.position - sourcePosition).normalized;

            GameEvents.RaisePlayerHit(new HitInfo(type, damage, transform.position, dir));
            GameEvents.RaisePlayerHealthChanged(_current, maxHealth);

            if (_current <= 0f) OnDeath();
        }

        /// Warning telegraph (no damage) fired just before an incoming attack.
        public void SignalWarning(Vector3 sourcePosition)
        {
            Vector3 dir = (transform.position - sourcePosition).normalized;
            GameEvents.RaisePlayerHit(new HitInfo(HitType.Warning, 0f, transform.position, dir));
        }

        void OnDeath()
        {
            Debug.Log("[CrossGuard] Player down.");
            // TODO: round-end / respawn hook (Lewis's user-study protocol will define this).
        }
    }
}
