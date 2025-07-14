using UnityEngine;
using Enemies.BossAttacks;
using Managers;
using System.Collections;

namespace Enemies{
    public class Boss : EnemyBase
    {
        [Header("Boss UI")]
        [SerializeField] private GameObject bossHealthBarPrefab;
        private BossHealthBarUI healthBarUI;

        [Header("Boss Attacks")]
        public BossAttackBase[] attacks; // Assign attack components in inspector
        protected BossAttackBase currentAttack;

        protected virtual void SetCurrentAttack(BossAttackBase attack)
        {
            currentAttack = attack;
        }

        protected override void Awake()
        {
            base.Awake();
            InitializeBossUI();
            var allAttacks = GetComponents<BossAttackBase>();
            attacks = System.Array.FindAll(allAttacks, attack => attack.enabled);
        }

        void Start(){
            StartCoroutine(WaitForPlayer());
            // Speed will be set by UpdateAnimationParameters based on agent velocity
        }
        private void InitializeAttacks()
        {
            foreach (var attack in attacks)
            {
                if (attack != null)
                {
                    attack.Initialize(this);
                }
            }
        }
        private IEnumerator WaitForPlayer()
        {
            float timeout = 3f;
            float elapsedTime = 0f;

            while (PlayerController.Instance._possessedNPC == null && elapsedTime < timeout)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (PlayerController.Instance._possessedNPC == null)
            {
                Debug.LogWarning("Boss failed to find player target after 3 seconds");
            }
            else
            {
                navMeshTarget = PlayerController.Instance._possessedNPC.GetTransform();
                InitializeAttacks();
            }
        }

        private void InitializeBossUI()
        {
            if (bossHealthBarPrefab != null)
            {
                GameObject healthBarObj = Instantiate(bossHealthBarPrefab);
                healthBarUI = healthBarObj.GetComponent<BossHealthBarUI>();
                if (healthBarUI != null)
                {
                    healthBarUI.Initialize(this);
                }
                else
                {
                    Debug.LogWarning("BossHealthBarUI component not found on boss health bar prefab");
                }
            }
            else
            {
                Debug.LogWarning("Boss health bar prefab not assigned");
            }
        }

        public new void TakeDamage(float amount, Transform damageSource = null)
        {
            base.TakeDamage(amount, damageSource);
            UpdateHealthUI();
        }

        private void UpdateHealthUI()
        {
            if (healthBarUI != null)
            {
                healthBarUI.UpdateHealth(Health, MaxHealth);
            }
        }

        public new void Die()
        {
            if (healthBarUI != null)
            {
                Destroy(healthBarUI.gameObject);
            }
            base.Die();
        }

        // Called by animator events
        public void AttackHit()
        {
            // Don't attack if dead
            if (Health <= 0) return;
            
            if (currentAttack != null)
            {
                currentAttack.OnAttack();
            }
        }

        // Called by animator events
        public void AttackEnd()
        {
            // Don't execute attack end logic if dead
            if (Health <= 0) return;
            
            if (currentAttack != null)
            {
                EndAttack();
                currentAttack.OnAttackEnd();
                currentAttack = null;
                animator.SetInteger("AttackType", 0);
                // Speed will be set by UpdateAnimationParameters based on agent velocity
            }
        }
    }
}
