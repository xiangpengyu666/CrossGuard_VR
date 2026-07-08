using UnityEngine;
using UnityEngine.UI;

namespace CrossGuard
{
    /// Red full-screen damage flash on player hit. Subscribes to
    /// GameEvents.OnPlayerHit (SEAM #1). Unlike CameraShake this is VR-SAFE — it's a
    /// screen-space overlay and never moves the camera, so it's the hit feedback that
    /// carries over to VR (together with the haptic band).
    public class DamageFlashUI : MonoBehaviour
    {
        public Image flash;                 // full-screen red image (alpha driven here)
        public float lightAlpha = 0.28f;
        public float heavyAlpha = 0.5f;
        public float fadeSpeed = 1.6f;      // alpha per second

        float _alpha;

        void OnEnable()  => GameEvents.OnPlayerHit += OnHit;
        void OnDisable() => GameEvents.OnPlayerHit -= OnHit;

        void OnHit(HitInfo info)
        {
            if (info.Type == HitType.Warning) return;    // telegraph, no damage flash
            float a = info.Type == HitType.Heavy ? heavyAlpha : lightAlpha;
            _alpha = Mathf.Max(_alpha, a);
        }

        void Update()
        {
            if (flash == null) return;
            _alpha = Mathf.Max(0f, _alpha - fadeSpeed * Time.deltaTime);
            var c = flash.color;
            c.a = _alpha;
            flash.color = c;
        }
    }
}
