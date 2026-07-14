using UnityEngine;

namespace CrossGuard
{
    /// SEAM #1 hardware bridge: forwards player-hit events to the haptic belt through
    /// an IHapticTransport. Maps HitType -> (intensity, durationMs) — the payload the
    /// belt firmware expects ("intensity duration") — and fires it the instant the hit
    /// resolves (the telegraph flash frame), so end-to-end latency stays under budget.
    ///
    /// Also watches the belt's heartbeat and pauses the game if it goes silent: a
    /// silently-dead haptic is worse than none — the player thinks they weren't hit
    /// while still taking damage.
    public class HapticBandListener : MonoBehaviour
    {
        [Header("Transport")]
        [Tooltip("A component implementing IHapticTransport (e.g. UdpHapticTransport). " +
                 "Left empty falls back to Console logging.")]
        public MonoBehaviour transportBehaviour;

        [Header("Buzz per hit type — x = intensity 0..1, y = duration ms")]
        public Vector2 light   = new Vector2(0.50f, 150f);
        public Vector2 heavy   = new Vector2(0.90f, 250f);
        public Vector2 warning = new Vector2(0.25f, 90f);
        [Tooltip("Also buzz on the pre-hit warning telegraph (no damage).")]
        public bool buzzOnWarning = false;

        [Header("Disconnect safety")]
        [Tooltip("Pause the game if the belt heartbeat is silent longer than this (s). " +
                 "0 = disabled. Only arms after the first heartbeat is heard.")]
        public float heartbeatTimeout = 3f;

        IHapticTransport _transport;
        bool _everSeenHeartbeat;
        bool _paused;

        void Awake() => _transport = transportBehaviour as IHapticTransport;

        void OnEnable()  => GameEvents.OnPlayerHit += HandlePlayerHit;
        void OnDisable() => GameEvents.OnPlayerHit -= HandlePlayerHit;

        void HandlePlayerHit(HitInfo info)
        {
            if (info.Type == HitType.Warning && !buzzOnWarning) return;

            Vector2 p = info.Type == HitType.Heavy   ? heavy
                      : info.Type == HitType.Warning ? warning
                                                     : light;

            if (_transport != null && _transport.IsReady)
                _transport.SendPulse(p.x, Mathf.RoundToInt(p.y));
            else
                Debug.Log($"[Band] {info.Type} buzz (no transport) i={p.x:0.00} d={p.y}ms");
        }

        void Update()
        {
            if (_transport == null || heartbeatTimeout <= 0f) return;

            float silent = _transport.SecondsSinceHeartbeat;
            if (!float.IsInfinity(silent)) _everSeenHeartbeat = true;
            if (!_everSeenHeartbeat) return;           // belt not in use yet — don't pause

            bool lost = silent > heartbeatTimeout;
            if (lost && !_paused)
            {
                _paused = true;
                Time.timeScale = 0f;
                Debug.LogWarning("[Band] belt heartbeat lost -> game paused");
            }
            else if (!lost && _paused)
            {
                _paused = false;
                Time.timeScale = 1f;
                Debug.Log("[Band] belt heartbeat back -> resumed");
            }
        }
    }
}
