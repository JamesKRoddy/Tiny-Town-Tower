using UnityEngine;

namespace Characters.NPC.Mutations
{
    public class HealthMutation : BaseNPCMutationEffect
    {
        [Header("Health Multipliers")]
        [SerializeField] private float healthMultiplier = 1.5f;
        [SerializeField] private float healthRegenMultiplier = 2f;

        private float originalMaxHealth;
        private float originalHealthRegen;

        protected override void ApplyEffect()
        {
            if (settlerNPC != null)
            {
                originalMaxHealth = settlerNPC.MaxHealth;
                originalHealthRegen = healthRegenMultiplier; // Store the multiplier since we don't have direct access to regen rate

                settlerNPC.MaxHealth *= healthMultiplier;
                settlerNPC.Health *= healthMultiplier; // Scale current health proportionally
            }
        }

        protected override void RemoveEffect()
        {
            if (settlerNPC != null)
            {
                settlerNPC.MaxHealth = originalMaxHealth;
                // Ensure current health doesn't exceed max health
                settlerNPC.Health = Mathf.Min(settlerNPC.Health, originalMaxHealth);
            }
        }

        public override void OnEquip()
        {
            base.OnEquip();
            
            settlerNPC = GetComponentInParent<SettlerNPC>();
            ApplyEffect();
        }

        public override void OnUnequip()
        {
            RemoveEffect();
            base.OnUnequip();
        }
    }
} 