using System;
using UnityEngine;

namespace CrossGuard
{
    /// Hit intensity categories. These map 1:1 to Cindy's haptic-band vibration
    /// patterns (light / heavy / warning). Keep this enum as the single source of
    /// truth shared across the Unity game and the future band bridge.
    public enum HitType
    {
        Light,
        Heavy,
        Warning   // telegraph before an incoming attack; not actual damage
    }

    /// Payload sent with every player-hit event.
    public struct HitInfo
    {
        public HitType Type;
        public float Damage;
        public Vector3 WorldPosition;   // where on/near the player it landed
        public Vector3 Direction;       // incoming direction (for directional haptics later)

        public HitInfo(HitType type, float damage, Vector3 pos, Vector3 dir)
        {
            Type = type;
            Damage = damage;
            WorldPosition = pos;
            Direction = dir;
        }
    }

    /// Central event hub. This is SEAM #1 from the work plan:
    /// Cindy's haptic band subscribes to OnPlayerHit to trigger vibration.
    /// Everything is in-process for now; later a network bridge (UDP / BLE / ROS#)
    /// forwards these events to the ESP32 band without touching gameplay code.
    public static class GameEvents
    {
        /// Raised whenever the player takes a hit (or receives a warning telegraph).
        public static event Action<HitInfo> OnPlayerHit;

        /// Raised when player health changes (current, max).
        public static event Action<float, float> OnPlayerHealthChanged;

        public static void RaisePlayerHit(HitInfo info) => OnPlayerHit?.Invoke(info);

        public static void RaisePlayerHealthChanged(float current, float max)
            => OnPlayerHealthChanged?.Invoke(current, max);
    }
}
