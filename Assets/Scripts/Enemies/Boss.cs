using UnityEngine;
using Enemies.BossAttacks;
using Managers;

namespace Enemies{
    public class Boss : EnemyBase
    {
        [Header("Boss UI")]
        [SerializeField] private GameObject bossHealthBarPrefab;
        private BossHealthBarUI healthBarUI;

        [Header("Boss Attacks")]
        public BossAttackBase[] attacks; // Assign attack components in inspector
        protected BossAttackBase currentAttack;

        protected override void Awake()
        {
            base.Awake();
            InitializeBossUI();
        }

        void Start(){
            navMeshTarget = PlayerController.Instance._possessedNPC.GetTransform();
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
        public void AttackStart()
        {
            if (currentAttack != null)
            {
                currentAttack.OnAttackStart();
            }
        }

        // Called by animator events
        public void AttackEnd()
        {
            if (currentAttack != null)
            {
                currentAttack.OnAttackEnd();
                currentAttack = null;
                animator.SetInteger("AttackType", 0);
            }
        }

        public void SetCurrentAttack(BossAttackBase attack)
        {
            currentAttack = attack;
        }
    }
}
