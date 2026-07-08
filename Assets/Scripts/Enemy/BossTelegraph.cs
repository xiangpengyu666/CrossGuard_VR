using UnityEngine;

namespace CrossGuard
{
    /// Ground attack-range telegraphs for the boss's Q attacks. Follows the boss
    /// along the ground (position + yaw) and, while an attack is active, animates a
    /// windup on the matching shape: a faint danger zone that fills up toward the
    /// strike (CrossGuard/Telegraph shader, _Fill), a bright sweeping edge, and a
    /// full flash at impact (_Flash).
    ///   shape 0 = forward rectangle (Q1)   1 = forward arc (Q2)   2 = ring (Q3)
    public class BossTelegraph : MonoBehaviour
    {
        [Header("Follow")]
        public Transform boss;
        public LayerMask groundMask = ~0;
        public float groundLift = 0.03f;

        [Header("Indicators")]
        public GameObject rectIndicator;    // Q1
        public GameObject arcIndicator;     // Q2
        public GameObject circleIndicator;  // Q3

        [Header("Sync")]
        [Tooltip("Boss Animator the windup reads its progress from (kept in lock-step " +
                 "with the actual attack animation). Falls back to a timer if unset.")]
        public Animator bossAnimator;
        [Tooltip("Fraction of the attack animation where the hit lands: the fill " +
                 "reaches full and the flash fires here. Nudge to match each swing.")]
        [Range(0.1f, 0.95f)] public float impactFraction = 0.5f;
        [Tooltip("Half-width of the impact flash, in animation-progress units.")]
        public float flashWidth = 0.09f;

        static readonly int FillID  = Shader.PropertyToID("_Fill");
        static readonly int FlashID = Shader.PropertyToID("_Flash");

        GameObject _active;
        Renderer _activeRenderer;
        MaterialPropertyBlock _mpb;
        float _elapsed, _duration;
        int _attackHash;

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            HideAll();
        }

        /// Show the telegraph for the given attack shape. `attackState` is the
        /// Animator state name being played so the windup can track its progress.
        public void Show(int shape, float duration, string attackState)
        {
            HideAll();
            _active = shape == 0 ? rectIndicator
                    : shape == 1 ? arcIndicator
                                 : circleIndicator;
            if (_active != null)
            {
                _active.SetActive(true);
                _activeRenderer = _active.GetComponent<Renderer>();
            }
            _elapsed = 0f;
            _duration = Mathf.Max(0.01f, duration);
            _attackHash = Animator.StringToHash(attackState);
            ApplyWindup(0f, 0f);
        }

        void HideAll()
        {
            if (rectIndicator)   rectIndicator.SetActive(false);
            if (arcIndicator)    arcIndicator.SetActive(false);
            if (circleIndicator) circleIndicator.SetActive(false);
            _active = null;
            _activeRenderer = null;
        }

        void ApplyWindup(float fill, float flash)
        {
            if (_activeRenderer == null) return;
            _activeRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(FillID, fill);
            _mpb.SetFloat(FlashID, flash);
            _activeRenderer.SetPropertyBlock(_mpb);
        }

        void LateUpdate()
        {
            if (boss != null)
            {
                Vector3 p = boss.position;
                if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down,
                                    out RaycastHit hit, 20f, groundMask))
                    p.y = hit.point.y + groundLift;
                transform.position = p;
                transform.rotation = Quaternion.Euler(0f, boss.eulerAngles.y, 0f);
            }

            if (_active == null) return;

            // progress from the actual attack animation (shared clock), so the
            // windup can't drift out of sync with the swing.
            float nt;
            if (bossAnimator != null)
            {
                var si = bossAnimator.GetCurrentAnimatorStateInfo(0);
                nt = si.shortNameHash == _attackHash
                        ? Mathf.Clamp01(si.normalizedTime)   // still swinging
                        : 1f;                                // swing already ended
            }
            else
            {
                nt = Mathf.Clamp01(_elapsed / _duration);
            }

            float fill = Mathf.Clamp01(nt / Mathf.Max(0.01f, impactFraction));
            float dToImpact = Mathf.Abs(nt - impactFraction);
            float flash = dToImpact < flashWidth ? 1f - dToImpact / flashWidth : 0f;
            ApplyWindup(fill, flash);

            // The flash instant IS the attack's resolution moment. Once it has fired
            // (progress passes the impact point), the telegraph has done its job and
            // is removed immediately — it does not linger through the follow-through.
            if (nt >= impactFraction + flashWidth) { HideAll(); return; }

            _elapsed += Time.deltaTime;
            if (_elapsed >= _duration) HideAll();   // safety fallback
        }
    }
}
