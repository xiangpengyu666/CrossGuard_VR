using UnityEngine;

namespace CrossGuard
{
    /// Procedural downward slash for the static sword viewmodel. This component sits
    /// on a pivot that parents the sword (+ placeholder arm); left-click rotates the
    /// pivot down and back so the blade chops. Purely visual for now — hooking the
    /// swing to real damage/hit detection comes later.
    ///
    /// VR NOTE: in VR this whole procedural swing is replaced by the real controller
    /// pose driving the weapon; keep the trigger isolated in ReadAttackInput().
    public class SwordSwing : MonoBehaviour
    {
        [Header("Swing shape")]
        [Tooltip("Downward pitch (deg) at the bottom of the slash.")]
        public float swingAngle = 80f;
        [Tooltip("Seconds from ready to full-down (the fast strike).")]
        public float strikeTime = 0.10f;
        [Tooltip("Seconds from full-down back to ready.")]
        public float recoverTime = 0.22f;
        [Tooltip("Seconds before another swing can start.")]
        public float cooldown = 0.35f;

        [Header("Melee hit (first-person: cast from the camera, not the viewmodel)")]
        public bool dealDamage = true;
        [Tooltip("Where the hit is cast from. Defaults to the main camera.")]
        public Transform aimFrom;
        public float meleeRange = 4f;
        public float meleeRadius = 0.7f;
        public float damage = 60f;
        public LayerMask hitMask = ~0;

        Quaternion _rest;
        float _phase = -1f;   // -1 = idle; otherwise elapsed seconds into the swing
        float _cd;
        bool _hitDone;

        void Awake()
        {
            _rest = transform.localRotation;
            if (aimFrom == null && Camera.main != null) aimFrom = Camera.main.transform;
        }

        // input isolation (VR replaces this)
        bool ReadAttackInput() => Input.GetMouseButtonDown(0);

        void Update()
        {
            if (_cd > 0f) _cd -= Time.deltaTime;
            if (_phase < 0f && _cd <= 0f && ReadAttackInput()) { _phase = 0f; _hitDone = false; }

            if (_phase < 0f) return;

            _phase += Time.deltaTime;

            // resolve the hit at the bottom of the swing (once per swing)
            if (!_hitDone && _phase >= strikeTime) { _hitDone = true; TryHit(); }

            float ang;
            if (_phase <= strikeTime)
                ang = Mathf.SmoothStep(0f, swingAngle, _phase / strikeTime);
            else if (_phase <= strikeTime + recoverTime)
                ang = Mathf.SmoothStep(swingAngle, 0f, (_phase - strikeTime) / recoverTime);
            else
            {
                transform.localRotation = _rest;
                _phase = -1f;
                _cd = cooldown;
                return;
            }
            transform.localRotation = _rest * Quaternion.Euler(ang, 0f, 0f);
        }

        void TryHit()
        {
            if (!dealDamage || aimFrom == null) return;
            var hits = Physics.SphereCastAll(aimFrom.position, meleeRadius, aimFrom.forward,
                                             meleeRange, hitMask, QueryTriggerInteraction.Ignore);
            BossHealth best = null;
            float bestDist = Mathf.Infinity;
            foreach (var h in hits)
            {
                var bh = h.collider.GetComponentInParent<BossHealth>();
                if (bh != null && !bh.IsDead && h.distance < bestDist)
                {
                    best = bh; bestDist = h.distance;
                }
            }
            if (best != null) best.TakeDamage(damage);
        }
    }
}
