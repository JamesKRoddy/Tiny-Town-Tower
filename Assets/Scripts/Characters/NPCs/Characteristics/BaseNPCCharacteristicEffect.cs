using UnityEngine;

namespace Characters.NPC.Characteristic
{
    public abstract class BaseNPCCharacteristicEffect : MonoBehaviour
    {
        protected NPCCharacteristicScriptableObj characteristicScriptableObj;
        protected SettlerNPC settlerNPC;
        protected int activeInstances = 0;

        public virtual int ActiveInstances
        {
            get => activeInstances;
            set => activeInstances = value;
        }

        public void Initialize(NPCCharacteristicScriptableObj characteristic, SettlerNPC settlerNPC)
        {
            this.characteristicScriptableObj = characteristic;
            this.settlerNPC = settlerNPC;
        }

        public virtual void OnEquip()
        {
            if (settlerNPC == null)
            {
                Debug.LogError($"BaseNPCCharacteristicEffect: settlerNPC is null for characteristic {characteristicScriptableObj?.name}");
                return;
            }
            ApplyEffect();
        }

        public virtual void OnUnequip()
        {
            if (settlerNPC == null)
            {
                Debug.LogError($"BaseNPCCharacteristicEffect: settlerNPC is null for characteristic {characteristicScriptableObj?.name}");
                return;
            }
            RemoveEffect();
        }

        protected abstract void ApplyEffect();
        protected abstract void RemoveEffect();
    }
} 