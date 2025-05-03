using UnityEngine;

namespace Characters.NPC.Mutations
{
    public class AdditionalMutationSlotsMutation : BaseNPCMutationEffect
    {
        [Header("Additional Mutation Slots Settings")]
        [SerializeField] private int additionalMutationSlots = 3;
        private int originalMaxSlots;

        protected override void ApplyEffect()
        {
            if (settlerNPC != null)
            {
                // Store original max slots
                originalMaxSlots = settlerNPC.mutationSystem.MaxMutations;
                
                // Increase max slots
                settlerNPC.mutationSystem.SetMaxMutations(originalMaxSlots + additionalMutationSlots);
            }
        }

        protected override void RemoveEffect()
        {
            if (settlerNPC != null)
            {
                // Revert to original max slots
                settlerNPC.mutationSystem.SetMaxMutations(originalMaxSlots);
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