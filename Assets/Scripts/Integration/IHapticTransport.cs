namespace CrossGuard
{
    /// The Unity-side seam for the haptic belt (Cindy's connected device). Mirrors
    /// IEnemyPoseSource on the robot side: the combat loop only ever talks to this
    /// interface, so swapping the transport (UDP now, BLE/serial later) touches no
    /// gameplay code.
    ///
    /// Contract matches the belt firmware: fire-and-forget, one-way downlink. Send a
    /// single "intensity duration" packet ("0.7 200"); never block, never wait for an
    /// ack (latency budget is <=100 ms, jitter matters more than mean).
    public interface IHapticTransport
    {
        /// True once the transport is ready to send (e.g. UDP socket open).
        bool IsReady { get; }

        /// Fire a single buzz. intensity 0..1, durationMs the total envelope length.
        /// Must return immediately (no blocking, no retransmit, no ordering).
        void SendPulse(float intensity, int durationMs);

        /// Seconds since the last heartbeat was heard from the belt (uplink 1 Hz).
        /// float.PositiveInfinity if none ever received. Used for disconnect detection.
        float SecondsSinceHeartbeat { get; }
    }
}
