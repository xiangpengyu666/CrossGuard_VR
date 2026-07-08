using UnityEngine;
using UnityEngine.UI;

namespace CrossGuard
{
    /// Drives a filled UI Image from a health source. Boss bars bind to a
    /// BossHealth; the player bar binds to GameEvents.OnPlayerHealthChanged
    /// (SEAM #1 hub). The fill eases toward the target for a smooth drain.
    public class HealthBarUI : MonoBehaviour
    {
        public enum Source { Player, Boss }

        [Header("Binding")]
        public Source source = Source.Boss;
        public BossHealth boss;          // used when source == Boss

        [Header("Refs")]
        public Image fill;               // Image (type = Filled)
        public Text label;               // optional name label

        [Header("Feel")]
        public float drainSpeed = 1.2f;  // fraction per second

        float _target = 1f;
        float _shown = 1f;

        void OnEnable()
        {
            if (source == Source.Boss && boss != null) boss.OnHealthChanged += Set;
            else GameEvents.OnPlayerHealthChanged += Set;
        }

        void OnDisable()
        {
            if (source == Source.Boss && boss != null) boss.OnHealthChanged -= Set;
            else GameEvents.OnPlayerHealthChanged -= Set;
        }

        // Read the initial value in Start (after every Awake has run), so the bar
        // doesn't briefly read a boss whose _current isn't initialized yet.
        void Start()
        {
            if (source == Source.Boss && boss != null)
            {
                Set(boss.Current, boss.Max);
                _shown = _target;
                if (label != null) label.text = boss.displayName;
            }
        }

        void Set(float current, float max)
        {
            _target = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        void Update()
        {
            if (fill == null) return;
            _shown = Mathf.MoveTowards(_shown, _target, drainSpeed * Time.deltaTime);
            fill.fillAmount = _shown;
        }
    }
}
