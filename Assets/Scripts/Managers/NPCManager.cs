using System.Collections.Generic;
using UnityEngine;
using Characters.NPC.Mutations;
using Characters.NPC;

namespace Managers
{
    public class NPCManager : MonoBehaviour
    {
        public static NPCManager Instance { get; private set; }

        [Header("Mutation Settings")]
        [SerializeField] private List<NPCMutationScriptableObj> allMutations = new List<NPCMutationScriptableObj>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public List<NPCMutationScriptableObj> GetAllMutations()
        {
            return allMutations;
        }

        public void AddMutationToNPC(SettlerNPC npc, NPCMutationScriptableObj mutation)
        {
            if (npc == null || mutation == null) return;

            NPCMutationSystem mutationSystem = npc.GetComponent<NPCMutationSystem>();
            if (mutationSystem == null) return;

            mutationSystem.EquipMutation(mutation);
        }

        public void RemoveMutationFromNPC(SettlerNPC npc, NPCMutationScriptableObj mutation)
        {
            if (npc == null || mutation == null) return;

            NPCMutationSystem mutationSystem = npc.GetComponent<NPCMutationSystem>();
            if (mutationSystem == null) return;

            mutationSystem.RemoveMutation(mutation);
        }
    }
} 