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

        public void Initialize(NPCMutationScriptableObj mutation)
        {
            this.mutation = mutation;
        }

        public virtual void OnEquip()
        {
            settlerNPC = GetComponentInParent<SettlerNPC>();
            ApplyEffect();
        }

        public virtual void OnUnequip()
        {
            RemoveEffect();
        }

        protected abstract void ApplyEffect();
        protected abstract void RemoveEffect();
    }
} 