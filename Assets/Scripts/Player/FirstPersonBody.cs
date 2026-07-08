using UnityEngine;

namespace CrossGuard
{
    /// Drives the first-person player body's Animator from actual movement.
    /// Reads the CharacterController's horizontal velocity (set by PlayerController)
    /// and writes it to the "Speed" float so the controller blends Idle <-> Run.
    ///
    /// The body model is a child of the Player; its head bone is scaled down so the
    /// first-person camera (at eye height) isn't looking at the inside of the head.
    /// All of the imported clips (attacks, spells, etc.) are available on the FBX
    /// to wire into more states later.
    [RequireComponent(typeof(Animator))]
    public class FirstPersonBody : MonoBehaviour
    {
        Animator _anim;
        CharacterController _cc;
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        void Awake()
        {
            _anim = GetComponent<Animator>();
            _cc = GetComponentInParent<CharacterController>();
        }

        void Update()
        {
            float speed = 0f;
            if (_cc != null)
            {
                Vector3 v = _cc.velocity;
                v.y = 0f;                    // ignore gravity/jump vertical component
                speed = v.magnitude;
            }
            _anim.SetFloat(SpeedHash, speed);
        }
    }
}
