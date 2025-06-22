using System.Collections.Generic;
using UnityEngine;
using Characters.NPC.Characteristic;
using Characters.NPC;
using System;

namespace Managers
{
    public class NPCManager : MonoBehaviour
    {
        public static NPCManager Instance { get; private set; }

        [Header("Characteristic Settings")]
        [SerializeField] private List<NPCCharacteristicScriptableObj> allCharacteristics = new List<NPCCharacteristicScriptableObj>();

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

        public List<NPCCharacteristicScriptableObj> GetAllCharacteristics()
        {
            return allCharacteristics;
        }

        public void AddCharacteristicToNPC(SettlerNPC npc, NPCCharacteristicScriptableObj characteristic)
        {
            if (npc == null || characteristic == null) return;

            NPCCharacteristicSystem characteristicSystem = npc.GetComponent<NPCCharacteristicSystem>();
            if (characteristicSystem == null) return;

            characteristicSystem.EquipCharacteristic(characteristic);
        }

        public void RemoveCharacteristicFromNPC(SettlerNPC npc, NPCCharacteristicScriptableObj characteristic)
        {
            if (npc == null || characteristic == null) return;

            NPCCharacteristicSystem characteristicsSystem = npc.GetComponent<NPCCharacteristicSystem>();
            if (characteristicsSystem == null) return;

            characteristicsSystem.RemoveCharacteristic(characteristic);
        }
    }
} 