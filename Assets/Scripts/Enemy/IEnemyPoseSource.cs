using UnityEngine;

namespace CrossGuard
{
    /// SEAM #2 from the work plan: "how the robot pose is communicated into Unity".
    ///
    /// The component implementing this interface OWNS the enemy transform (it moves
    /// and rotates the enemy GameObject). Two implementations:
    ///   - Now  : LocalAiPoseSource  (pure-Unity NavMesh AI)
    ///   - Later: NetworkPoseSource  (real S100 robot pose streamed over WiFi)
    ///
    /// Attack / hit logic (EnemyRobot) only READS the pose through TryGetPose and
    /// never talks to the network. Swapping virtual -> physical is therefore a
    /// one-component change with zero gameplay rewrite.
    public interface IEnemyPoseSource
    {
        /// Latest world-space pose of the enemy/robot. Returns false until a valid
        /// pose is available (e.g. network source before the first packet arrives).
        bool TryGetPose(out Vector3 position, out Quaternion rotation);
    }
}
