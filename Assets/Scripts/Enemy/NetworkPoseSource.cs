using UnityEngine;

namespace CrossGuard
{
    /// SEAM #2 (future phase): drives the enemy transform from the REAL robot pose
    /// streamed over WiFi. Swap this in for LocalAiPoseSource once co-localization
    /// is validated on the basketball court.
    ///
    /// Fill in the transport with the team's choice. Candidates from our reference
    /// systems: ROS# over WebSocket, or a lightweight UDP/MQTT feed. Poses arrive in
    /// the robot map frame and must be converted to Unity world frame using the
    /// co-localization offset measured at the venue (Lewis's validation step).
    ///
    /// Contract is identical to LocalAiPoseSource, so EnemyRobot needs no changes.
    public class NetworkPoseSource : MonoBehaviour, IEnemyPoseSource
    {
        [Header("Jitter smoothing")]
        public float positionLerp = 15f;
        public float rotationLerp = 15f;

        Vector3 _targetPos;
        Quaternion _targetRot = Quaternion.identity;
        bool _hasPose;

        // TODO: subscribe to the transport in OnEnable, unsubscribe in OnDisable,
        // and call OnPoseReceived(...) from the transport callback.
        public void OnPoseReceived(Vector3 worldPos, Quaternion worldRot)
        {
            _targetPos = worldPos;
            _targetRot = worldRot;
            _hasPose = true;
        }

        void Update()
        {
            if (!_hasPose) return;
            transform.position = Vector3.Lerp(transform.position, _targetPos,
                                              positionLerp * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot,
                                                  rotationLerp * Time.deltaTime);
        }

        public bool TryGetPose(out Vector3 position, out Quaternion rotation)
        {
            position = transform.position;
            rotation = transform.rotation;
            return _hasPose;
        }
    }
}
