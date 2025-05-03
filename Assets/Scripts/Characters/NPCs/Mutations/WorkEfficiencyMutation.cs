using UnityEngine;

namespace Characters.NPC.Mutations
{
    public class WorkEfficiencyMutation : BaseNPCMutationEffect
    {
        [Header("Efficiency Multipliers")]
        [SerializeField] private float fatigueReductionMultiplier = 0.5f;
        [SerializeField] private float staminaRegenMultiplier = 2f;

        protected override void ApplyEffect()
        {
            if (settlerNPC != null)
            {
                settlerNPC.fatigueRate *= fatigueReductionMultiplier;
                settlerNPC.staminaRegenRate *= staminaRegenMultiplier;
            }
        }

        protected override void RemoveEffect()
        {
            if (settlerNPC != null)
            {
                settlerNPC.fatigueRate /= fatigueReductionMultiplier;
                settlerNPC.staminaRegenRate /= staminaRegenMultiplier;
            }
        }

        public override void OnEquip()
        {
            base.OnEquip();
            ApplyEffect();
        }

        public override void OnUnequip()
        {
            RemoveEffect();
            base.OnUnequip();
        }
    }
} 