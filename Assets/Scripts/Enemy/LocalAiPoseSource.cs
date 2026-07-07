using UnityEngine;
using UnityEngine.AI;

namespace CrossGuard
{
    /// Pure-Unity pose source: a NavMesh agent that auto-tracks the player while
    /// holding a preferred ranged distance (kiting). It owns the transform and
    /// produces the same IEnemyPoseSource output the network source will later.
    [RequireComponent(typeof(NavMeshAgent))]
    public class LocalAiPoseSource : MonoBehaviour, IEnemyPoseSource
    {
        [Header("Target")]
        public Transform player;

        [Header("Ranged kiting")]
        [Tooltip("Try to hold this distance from the player.")]
        public float preferredRange = 6f;
        [Tooltip("Re-path when off the preferred range by more than this.")]
        public float rangeTolerance = 1.5f;
        [Tooltip("How fast the body turns to face the player (deg/sec).")]
        public float turnSpeed = 360f;

        NavMeshAgent _agent;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false; // we face the player ourselves
        }

        void Update()
        {
            if (!player) return;

            Vector3 toPlayer = player.position - transform.position;
            float dist = toPlayer.magnitude;

            if (dist > preferredRange + rangeTolerance)
            {
                _agent.SetDestination(player.position);                 // too far: close in
            }
            else if (dist < preferredRange - rangeTolerance)
            {
                Vector3 away = transform.position - toPlayer.normalized * 2f;
                if (NavMesh.SamplePosition(away, out var hit, 2f, NavMesh.AllAreas))
                    _agent.SetDestination(hit.position);                // too close: back off
            }
            else
            {
                _agent.ResetPath();                                     // in the pocket: hold
            }

            FacePlayer(toPlayer);
        }

        void FacePlayer(Vector3 toPlayer)
        {
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        public bool TryGetPose(out Vector3 position, out Quaternion rotation)
        {
            position = transform.position;
            rotation = transform.rotation;
            return true;
        }
    }
}
