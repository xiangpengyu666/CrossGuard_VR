using System;
using UnityEngine;

namespace CrossGuard
{
    /// Boss hit points with multiple lives / phases. Each life is a full bar:
    /// depleting a non-final life advances a phase (the AI plays the transform and
    /// calls BeginNextPhase to refill) instead of dying; depleting the final life
    /// ends the fight. Kept separate from PlayerHealth so bosses re-skin freely.
    [RequireComponent(typeof(Animator))]
    public class BossHealth : MonoBehaviour
    {
        [Header("Health")]
        public float maxHealth = 600f;
        public string displayName = "BOSS";
        [Tooltip("Number of lives / phases. Life 1 = pre-transform, life 2 = demon form.")]
        public int maxLives = 2;
        [SerializeField] float _current;

        public event Action<float, float> OnHealthChanged;  // (current, max)
        public event Action OnPhaseAdvance;                 // a non-final life emptied
        public event Action OnDied;                         // final life emptied

        public float Current => _current;
        public float Max => maxHealth;
        public int Life => _life;              // 0-based current life index
        public bool IsDead => _dead;

        /// Set by the AI to block damage during the transform animation.
        [HideInInspector] public bool Invulnerable;

        Animator _anim;
        bool _dead;
        bool _awaitingPhase;   // life emptied, waiting for the transform to finish
        int _life;

        void Awake()
        {
            _anim = GetComponent<Animator>();
            _current = maxHealth;
        }

        /// Player melee damage routes here.
        public void TakeDamage(float amount)
        {
            if (_dead || Invulnerable || _awaitingPhase || amount <= 0f) return;

            _current = Mathf.Max(0f, _current - amount);
            OnHealthChanged?.Invoke(_current, maxHealth);

            if (_current <= 0f)
            {
                if (_life < maxLives - 1)
                {
                    _awaitingPhase = true;      // no more damage until the phase advances
                    OnPhaseAdvance?.Invoke();   // AI plays the transform, then refills
                }
                else
                {
                    Die();
                }
            }
        }

        /// Called by the boss AI once the transform animation finishes: begin the
        /// next life with a full bar.
        public void BeginNextPhase()
        {
            if (_dead) return;
            _life++;
            _awaitingPhase = false;
            _current = maxHealth;
            OnHealthChanged?.Invoke(_current, maxHealth);
        }

        void Die()
        {
            _dead = true;
            if (_anim != null) _anim.Play("Death", 0, 0f);
            var chaser = GetComponent<BossChaser>();
            if (chaser != null) chaser.enabled = false;
            OnDied?.Invoke();
            Debug.Log("[CrossGuard] Boss defeated: " + displayName);
        }
    }
}
