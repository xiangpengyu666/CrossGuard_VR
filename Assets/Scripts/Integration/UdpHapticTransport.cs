using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace CrossGuard
{
    /// UDP implementation of IHapticTransport. Fire-and-forget downlink to the belt
    /// ESP32 (default port 8888) with an ASCII "intensity duration" payload (e.g.
    /// "0.70 200"), plus a listener for the belt's 1 Hz heartbeat uplink used for
    /// disconnect detection. Matches the belt firmware: prefer drop over late, no
    /// retransmit, no ordering.
    public class UdpHapticTransport : MonoBehaviour, IHapticTransport
    {
        [Header("Belt endpoint (downlink)")]
        [Tooltip("ESP32 IP printed on its serial monitor after it joins WiFi.")]
        public string beltIp = "192.168.1.50";
        public int beltPort = 8888;

        [Header("Heartbeat (uplink)")]
        [Tooltip("Local UDP port the belt sends its 1 Hz heartbeat to. 0 = don't listen.")]
        public int heartbeatListenPort = 8889;

        UdpClient _send;
        UdpClient _recv;
        IPEndPoint _beltEndpoint;
        long _lastHbTicks;      // DateTime.UtcNow.Ticks of last heartbeat (0 = none)
        bool _ready;

        public bool IsReady => _ready;

        public float SecondsSinceHeartbeat
        {
            get
            {
                long ticks = Interlocked.Read(ref _lastHbTicks);
                if (ticks == 0L) return float.PositiveInfinity;
                return (float)(DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds;
            }
        }

        void OnEnable()
        {
            try
            {
                _beltEndpoint = new IPEndPoint(IPAddress.Parse(beltIp), beltPort);
                _send = new UdpClient();
                _send.Client.Blocking = false;         // never stall the game loop
                _ready = true;

                if (heartbeatListenPort > 0)
                {
                    _recv = new UdpClient(heartbeatListenPort);
                    _recv.BeginReceive(OnHeartbeat, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Haptic] UDP transport init failed: " + e.Message);
                _ready = false;
            }
        }

        void OnDisable()
        {
            _ready = false;
            try { _send?.Close(); } catch { }
            try { _recv?.Close(); } catch { }
            _send = null;
            _recv = null;
        }

        public void SendPulse(float intensity, int durationMs)
        {
            if (!_ready || _send == null) return;
            intensity = Mathf.Clamp01(intensity);
            string msg = intensity.ToString("0.00", CultureInfo.InvariantCulture)
                       + " " + durationMs.ToString(CultureInfo.InvariantCulture);
            byte[] data = Encoding.ASCII.GetBytes(msg);
            try { _send.Send(data, data.Length, _beltEndpoint); }   // fire & forget
            catch (SocketException) { /* prefer drop over stall */ }
        }

        // Runs on a threadpool thread; keep it minimal and thread-safe.
        void OnHeartbeat(IAsyncResult ar)
        {
            try
            {
                IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                _recv.EndReceive(ar, ref from);
                Interlocked.Exchange(ref _lastHbTicks, DateTime.UtcNow.Ticks);
            }
            catch { return; }                          // socket closing
            try { _recv?.BeginReceive(OnHeartbeat, null); } catch { }
        }
    }
}
