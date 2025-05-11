using UnityEngine;

namespace Characters.NPC.Mutations
{
    public abstract class BaseNPCMutationEffect : MonoBehaviour
    {
        protected NPCMutationScriptableObj mutation;
        protected SettlerNPC settlerNPC;
        protected int activeInstances = 0;

        public virtual int ActiveInstances
        {
            get => activeInstances;
            set => activeInstances = value;
        }

        public void Initialize(NPCMutationScriptableObj mutation, SettlerNPC settlerNPC)
        {
            this.mutation = mutation;
            this.settlerNPC = settlerNPC;
        }

        public virtual void OnEquip()
        {
            if (settlerNPC == null)
            {
                Debug.LogError($"BaseNPCMutationEffect: settlerNPC is null for mutation {mutation?.name}");
                return;
            }
            ApplyEffect();
        }

        public virtual void OnUnequip()
        {
            if (settlerNPC == null)
            {
                Debug.LogError($"BaseNPCMutationEffect: settlerNPC is null for mutation {mutation?.name}");
                return;
            }
            RemoveEffect();
        }

        protected abstract void ApplyEffect();
        protected abstract void RemoveEffect();
    }
} 