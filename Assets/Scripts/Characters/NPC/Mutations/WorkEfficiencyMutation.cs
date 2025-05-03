using UnityEngine;

namespace Characters.NPC.Mutations
{
    public class WorkEfficiencyMutation : BaseNPCMutationEffect
    {
        [Header("Work Efficiency Settings")]
        [SerializeField] private float workSpeedMultiplier = 1.5f;
        [SerializeField] private float fatigueReductionMultiplier = 0.7f;
        [SerializeField] private float staminaRegenMultiplier = 1.3f;

        private SettlerNPC settlerNPC;
        private NPCStats npcStats;

        protected override void ApplyEffect()
        {
            if (settlerNPC != null)
            {
                // Modify work speed through the SettlerNPC's agent
                settlerNPC.GetAgent().speed *= workSpeedMultiplier;
            }

            if (npcStats != null)
            {
                npcStats.fatigueRate *= fatigueReductionMultiplier;
                npcStats.staminaRegenRate *= staminaRegenMultiplier;
            }
        }

        protected override void RemoveEffect()
        {
            if (settlerNPC != null)
            {
                // Revert work speed
                settlerNPC.GetAgent().speed /= workSpeedMultiplier;
            }

            if (npcStats != null)
            {
                npcStats.fatigueRate /= fatigueReductionMultiplier;
                npcStats.staminaRegenRate /= staminaRegenMultiplier;
            }
        }

        public override void OnEquip()
        {
            base.OnEquip();
            
            // Get NPC components
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