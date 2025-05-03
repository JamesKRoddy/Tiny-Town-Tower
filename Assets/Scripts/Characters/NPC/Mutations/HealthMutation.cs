using UnityEngine;

namespace Characters.NPC.Mutations
{
    public class HealthMutation : BaseNPCMutationEffect
    {
        [Header("Health Settings")]
        [SerializeField] private float healthMultiplier = 1.5f;
        [SerializeField] private float healthRegenMultiplier = 1.3f;
        private float originalMaxHealth;
        private float originalHealthRegen;

        protected override void ApplyEffect()
        {
            if (npcStats != null)
            {
                // Store original values
                originalMaxHealth = npcStats.maxHealth;
                originalHealthRegen = npcStats.healthRegenRate;

                // Apply multipliers
                npcStats.maxHealth *= healthMultiplier;
                npcStats.currentHealth *= healthMultiplier; // Scale current health proportionally
                npcStats.healthRegenRate *= healthRegenMultiplier;
            }
        }

        protected override void RemoveEffect()
        {
            if (npcStats != null)
            {
                // Revert to original values
                npcStats.maxHealth = originalMaxHealth;
                // Ensure current health doesn't exceed max health
                npcStats.currentHealth = Mathf.Min(npcStats.currentHealth, originalMaxHealth);
                npcStats.healthRegenRate = originalHealthRegen;
            }
        }

        public override void OnEquip()
        {
            base.OnEquip();
            
            settlerNPC = GetComponentInParent<SettlerNPC>();
            npcStats = GetComponentInParent<NPCStats>();
            ApplyEffect();
        }

        public override void OnUnequip()
        {
            RemoveEffect();
            base.OnUnequip();
        }
    }
} 