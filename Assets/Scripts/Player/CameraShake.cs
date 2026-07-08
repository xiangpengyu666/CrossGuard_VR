using UnityEngine;

namespace CrossGuard
{
    /// Screen shake on player hit (flat-screen feedback). Subscribes to
    /// GameEvents.OnPlayerHit (SEAM #1) and adds a decaying offset in LateUpdate,
    /// layered on top of PlayerController's mouse-look so it doesn't fight it.
    ///
    /// VR NOTE: DO NOT use camera shake in VR — moving the HMD independently of the
    /// user's head causes motion sickness. In VR, disable this component and rely on
    /// DamageFlashUI (screen vignette) + the haptic band + controller rumble, which
    /// all hang off the same OnPlayerHit event.
    public class CameraShake : MonoBehaviour
    {
        [Header("Trauma added per hit type (0..1)")]
        public float lightTrauma = 0.35f;
        public float heavyTrauma = 0.7f;
        public float warningTrauma = 0.12f;

        [Header("Shake")]
        public float maxAngle = 6f;       // degrees at full trauma
        public float maxOffset = 0.10f;   // metres at full trauma
        public float frequency = 22f;
        public float decay = 1.8f;        // trauma lost per second

        Vector3 _basePos;
        float _trauma;
        float _seed;

        void Awake()
        {
            _basePos = transform.localPosition;
            _seed = Random.value * 100f;
        }

        void OnEnable()  => GameEvents.OnPlayerHit += OnHit;
        void OnDisable() => GameEvents.OnPlayerHit -= OnHit;

        void OnHit(HitInfo info)
        {
            float t = info.Type == HitType.Heavy   ? heavyTrauma
                    : info.Type == HitType.Warning ? warningTrauma
                                                   : lightTrauma;
            _trauma = Mathf.Clamp01(Mathf.Max(_trauma, t));
        }

        void LateUpdate()
        {
            if (_trauma <= 0f)
            {
                transform.localPosition = _basePos;
                return;
            }

            float amt = _trauma * _trauma;                 // ease-in
            float tt = Time.time * frequency;
            float nx = Mathf.PerlinNoise(_seed, tt) * 2f - 1f;
            float ny = Mathf.PerlinNoise(_seed + 10f, tt) * 2f - 1f;
            float nz = Mathf.PerlinNoise(_seed + 20f, tt) * 2f - 1f;

            transform.localPosition = _basePos + new Vector3(nx, ny, 0f) * maxOffset * amt;
            transform.localRotation *= Quaternion.Euler(nx * maxAngle * amt,
                                                        ny * maxAngle * amt,
                                                        nz * maxAngle * amt);

            _trauma = Mathf.Max(0f, _trauma - decay * Time.deltaTime);
        }
    }
}
