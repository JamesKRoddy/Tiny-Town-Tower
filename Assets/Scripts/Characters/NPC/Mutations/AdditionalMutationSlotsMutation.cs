using UnityEngine;

namespace Characters.NPC.Mutations
{
    public class AdditionalMutationSlotsMutation : BaseNPCMutationEffect
    {
        [Header("Additional Mutation Slots Settings")]
        [SerializeField] private int additionalMutationSlots = 3;
        private NPCMutationSystem npcMutationSystem;
        private int originalMaxSlots;

        protected override void ApplyEffect()
        {
            if (npcMutationSystem != null)
            {
                // Store original max slots
                originalMaxSlots = npcMutationSystem.MaxMutations;
                
                // Increase max slots
                npcMutationSystem.SetMaxMutations(originalMaxSlots + additionalMutationSlots);
            }

            if (settlerNPC != null)
            {
                // Increase possession duration through the SettlerNPC's agent
                // Note: You'll need to add a possessionDuration property to SettlerNPC if it doesn't exist
                // settlerNPC.possessionDuration *= possessionDurationMultiplier;
            }
        }

        protected override void RemoveEffect()
        {
            if (npcMutationSystem != null)
            {
                // Revert to original max slots
                npcMutationSystem.SetMaxMutations(originalMaxSlots);
            }

            if (settlerNPC != null)
            {
                // Revert possession duration
                // settlerNPC.possessionDuration /= possessionDurationMultiplier;
            }
        }

        public override void OnEquip()
        {
            base.OnEquip();
            npcMutationSystem = GetComponentInParent<NPCMutationSystem>();
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