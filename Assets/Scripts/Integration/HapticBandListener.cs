using UnityEngine;

namespace CrossGuard
{
    /// Demo subscriber for SEAM #1. Right now it just logs each hit so you can SEE
    /// the event firing in the Console. Cindy replaces the body of HandlePlayerHit
    /// with the real bridge that forwards to the ESP32 band (which upgrades from
    /// button/phone-triggered to event-triggered vibration, per the work plan).
    ///
    /// The band picks a vibration pattern from HitType:
    ///   Light   -> short soft buzz
    ///   Heavy   -> strong buzz
    ///   Warning -> pre-hit pulse
    public class HapticBandListener : MonoBehaviour
    {
        void OnEnable()  => GameEvents.OnPlayerHit += HandlePlayerHit;
        void OnDisable() => GameEvents.OnPlayerHit -= HandlePlayerHit;

        void HandlePlayerHit(HitInfo info)
        {
            switch (info.Type)
            {
                case HitType.Light:   Debug.Log("[Band] light buzz");    break;
                case HitType.Heavy:   Debug.Log("[Band] heavy buzz");    break;
                case HitType.Warning: Debug.Log("[Band] warning pulse"); break;
            }
            // TODO (Cindy): send { type, damage, direction } to the ESP32
            // over UDP / BLE / serial. Payload fields already available on `info`.
        }
    }
}
