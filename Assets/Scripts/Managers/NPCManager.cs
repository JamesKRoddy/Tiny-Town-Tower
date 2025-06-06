using System.Collections.Generic;
using UnityEngine;
using Characters.NPC.Mutations;
using Characters.NPC;
using System;

namespace Managers
{
    public class NPCManager : MonoBehaviour
    {
        public static NPCManager Instance { get; private set; }

        [Header("Mutation Settings")]
        [SerializeField] private List<NPCMutationScriptableObj> allMutations = new List<NPCMutationScriptableObj>();

        [Header("NPC Tracking")]
        private List<SettlerNPC> activeNPCs = new List<SettlerNPC>();
        public int TotalNPCs => activeNPCs.Count;

        // Event for NPC count changes
        public event Action<int> OnNPCCountChanged;

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

        public void RegisterNPC(SettlerNPC npc)
        {
            if (!activeNPCs.Contains(npc))
            {
                activeNPCs.Add(npc);
                OnNPCCountChanged?.Invoke(TotalNPCs);
            }
        }

        public void UnregisterNPC(SettlerNPC npc)
        {
            if (activeNPCs.Remove(npc))
            {
                OnNPCCountChanged?.Invoke(TotalNPCs);
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