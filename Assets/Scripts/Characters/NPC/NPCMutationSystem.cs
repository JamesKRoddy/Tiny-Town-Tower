using System.Collections.Generic;
using UnityEngine;
using Characters.NPC.Mutations;

namespace Characters.NPC
{
    public class NPCMutationSystem : MonoBehaviour
    {
        [Header("Mutation Settings")]
        [SerializeField] private int maxMutations = 3;
        [SerializeField] private float mutationSpawnChance = 0.3f;
        [SerializeField] private float rareMutationChance = 0.1f;
        [SerializeField] private int minRandomMutations = 1;
        [SerializeField] private int maxRandomMutations = 3;

        private List<NPCMutationScriptableObj> equippedMutations = new List<NPCMutationScriptableObj>();
        private Dictionary<NPCMutationScriptableObj, BaseNPCMutationEffect> activeEffects = new Dictionary<NPCMutationScriptableObj, BaseNPCMutationEffect>();

        public int MaxMutations => maxMutations;
        public List<NPCMutationScriptableObj> EquippedMutations => equippedMutations;

        private SettlerNPC settlerNPC;

        private void Start()
        {
            settlerNPC = GetComponent<SettlerNPC>();
            if (settlerNPC == null)
            {
                Debug.LogError("NPCMutationSystem requires a SettlerNPC component on the same GameObject");
                return;
            }

            // Apply random mutations
            ApplyRandomMutations();
        }

        private void ApplyRandomMutations()
        {
            // Determine if this NPC should get mutations
            if (Random.value > mutationSpawnChance) return;

            // Get all available mutations from the manager
            List<NPCMutationScriptableObj> allMutations = NPCMutationManager.Instance.GetAllMutations();
            if (allMutations.Count == 0) return;

            // Determine number of mutations
            int numMutations = Random.Range(minRandomMutations, maxRandomMutations + 1);

            if (allMutations.Count == 0) return;

            for (int i = 0; i < numMutations; i++)
            {
                if (allMutations.Count == 0) break;

                // Select a random mutation
                int index = Random.Range(0, allMutations.Count);
                NPCMutationScriptableObj mutation = allMutations[index];

                // Check rarity
                if (mutation.rarity == ResourceRarity.RARE || mutation.rarity == ResourceRarity.LEGENDARY)
                {
                    if (Random.value > rareMutationChance)
                    {
                        continue;
                    }
                }

                // Add mutation to NPC
                EquipMutation(mutation);

                // Remove from valid mutations to prevent duplicates
                allMutations.RemoveAt(index);
            }
        }

        public void EquipMutation(NPCMutationScriptableObj mutation)
        {
            if (equippedMutations.Count >= maxMutations)
            {
                Debug.LogWarning("Cannot equip more mutations: maximum mutations reached");
                return;
            }

            equippedMutations.Add(mutation);
            
            // Instantiate and initialize the mutation effect
            if (mutation.mutationEffectPrefab != null)
            {
                GameObject effectObj = Instantiate(mutation.mutationEffectPrefab, transform);
                BaseNPCMutationEffect effect = effectObj.GetComponent<BaseNPCMutationEffect>();
                if (effect != null)
                {
                    effect.Initialize(mutation);
                    effect.OnEquip();
                    activeEffects[mutation] = effect;
                }
            }
        }

        public void RemoveMutation(NPCMutationScriptableObj mutation)
        {
            if (equippedMutations.Remove(mutation))
            {
                // Remove and cleanup the mutation effect
                if (activeEffects.TryGetValue(mutation, out BaseNPCMutationEffect effect))
                {
                    effect.OnUnequip();
                    Destroy(effect.gameObject);
                    activeEffects.Remove(mutation);
                }
            }
        }

        public void SetMaxMutations(int count)
        {
            maxMutations = count;
        }

        // Helper method to check if NPC has a specific mutation
        public bool HasMutation(NPCMutationScriptableObj mutation)
        {
            return equippedMutations.Contains(mutation);
        }

        // Helper method to get all active effects of a specific type
        public List<T> GetActiveEffectsOfType<T>() where T : BaseNPCMutationEffect
        {
            List<T> effects = new List<T>();
            foreach (var effect in activeEffects.Values)
            {
                if (effect is T typedEffect)
                {
                    effects.Add(typedEffect);
                }
            }
            return effects;
        }
    }
} 