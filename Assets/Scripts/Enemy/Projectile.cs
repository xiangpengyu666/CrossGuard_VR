using UnityEngine;

namespace CrossGuard
{
    /// Simple travelling projectile. On hitting the player it applies damage,
    /// which routes through PlayerHealth -> OnPlayerHit (SEAM #1).
    ///
    /// Setup: give the prefab a Collider with "Is Trigger" enabled and a Rigidbody
    /// with "Is Kinematic" enabled (so OnTriggerEnter fires). The player capsule
    /// needs a non-trigger Collider.
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        public float speed = 12f;
        public float lifeTime = 4f;

        Vector3 _dir;
        float _damage;
        GameObject _owner;

        public void Launch(Vector3 dir, float damage, GameObject owner)
        {
            _dir = dir.normalized;
            _damage = damage;
            _owner = owner;
            Destroy(gameObject, lifeTime);
        }

        void Update() => transform.position += _dir * speed * Time.deltaTime;

        void OnTriggerEnter(Collider other)
        {
            if (_owner && other.gameObject == _owner) return;

            var health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(_damage, transform.position);
                Destroy(gameObject);
            }
            else if (!other.isTrigger)
            {
                Destroy(gameObject); // hit arena geometry / wall
            }
        }
    }
}
