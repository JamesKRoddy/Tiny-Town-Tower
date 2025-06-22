using UnityEngine;

namespace Characters.NPC.Characteristic
{
    public class AdditionalMutationSlotsCharacteristic : BaseNPCCharacteristicEffect
    {
        [Header("Additional Mutation Slots Settings")]
        [SerializeField] private int additionalMutationSlots = 3;
        private int originalMaxSlots;

        protected override void ApplyEffect()
        {
            if (settlerNPC == null || settlerNPC.characteristicSystem == null)
            {
                Debug.LogError($"AdditionalMutationSlotsCharacteristic: settlerNPC or characteristicSystem is null for characteristic {characteristicScriptableObj?.name}");
                return;
            }

            // Store original max slots
            originalMaxSlots = settlerNPC.additionalMutationSlots;
            
            // Increase max slots
            settlerNPC.additionalMutationSlots = originalMaxSlots + additionalMutationSlots;
        }

        protected override void RemoveEffect()
        {
            if (settlerNPC == null || settlerNPC.characteristicSystem == null)
            {
                Debug.LogError($"AdditionalMutationSlotsCharacteristic: settlerNPC or characteristicSystem is null for characteristic {characteristicScriptableObj?.name}");
                return;
            }

            // Revert to original max slots
            settlerNPC.additionalMutationSlots = originalMaxSlots;
        }

        public override void OnUnequip()
        {
            RemoveEffect();
            base.OnUnequip();
        }

        public override string GetStatsDescription()
        {
            return $"Additional Mutation Slots: {additionalMutationSlots}";
        }
    }
} 