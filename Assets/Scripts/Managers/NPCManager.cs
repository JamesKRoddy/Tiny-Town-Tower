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

        [Header("NPC Transfer Settings")]
        [SerializeField] private float transferDelay = 2f; // Delay before transferring NPCs to allow scene setup
        [SerializeField] private bool showTransferNotification = true;

        private bool hasTransferredThisSession = false;

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

        private void Start()
        {
            // Subscribe to game mode changes to detect when returning to camp
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameModeChanged += OnGameModeChanged;
            }
        }

        private void OnGameModeChanged(GameMode newGameMode)
        {            
            // Check if we're returning to camp mode and have recruited NPCs to transfer
            if (newGameMode == GameMode.CAMP && PlayerInventory.Instance != null && PlayerInventory.Instance.HasRecruitedNPCs())
            {
                Invoke(nameof(TransferRecruitedNPCs), transferDelay);
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

        #region NPC Transfer Management

        /// <summary>
        /// Transfer recruited NPCs from player inventory to camp
        /// </summary>
        public void TransferRecruitedNPCs()
        {
            if (hasTransferredThisSession)
            {
                return; // Prevent multiple transfers in the same session
            }

            if (PlayerInventory.Instance == null)
            {
                Debug.LogWarning("[NPCManager] PlayerInventory not found!");
                return;
            }

            if (!PlayerInventory.Instance.HasRecruitedNPCs())
            {
                return; // No NPCs to transfer
            }

            var recruitedNPCs = PlayerInventory.Instance.GetRecruitedNPCs();
            
            // Subscribe to transfer event to show notification
            if (showTransferNotification)
            {
                PlayerInventory.Instance.OnNPCsTransferredToCamp += ShowTransferNotification;
            }

            // Transfer the NPCs
            PlayerInventory.Instance.TransferRecruitedNPCsToCamp();
            
            hasTransferredThisSession = true;
        }

        /// <summary>
        /// Show a notification when NPCs are transferred to camp
        /// </summary>
        private void ShowTransferNotification(System.Collections.Generic.List<NPCScriptableObj> transferredNPCs)
        {
            // Unsubscribe from event
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnNPCsTransferredToCamp -= ShowTransferNotification;
            }

            if (transferredNPCs.Count == 1)
            {
                Debug.Log($"[NPCManager] {transferredNPCs[0].nPCName} has joined your camp!");
                
                // Show UI notification if available
                if (PlayerUIManager.Instance?.inventoryPopup != null)
                {
                    // You could create a specific "NPC Joined Camp" popup here
                    PlayerUIManager.Instance.inventoryPopup.ShowInventoryPopup(transferredNPCs[0], 1, true);
                }
            }
            else
            {
                Debug.Log($"[NPCManager] {transferredNPCs.Count} new settlers have joined your camp!");
                
                // Show a summary notification for multiple NPCs
                foreach (var npc in transferredNPCs)
                {
                    Debug.Log($"  - {npc.nPCName}");
                }
            }
        }

        /// <summary>
        /// Reset transfer state (useful for testing or scene transitions)
        /// </summary>
        public void ResetTransferState()
        {
            hasTransferredThisSession = false;
        }

        /// <summary>
        /// Manually trigger NPC transfer (for testing or specific scenarios)
        /// </summary>
        [ContextMenu("Transfer Recruited NPCs")]
        public void ManualTransfer()
        {
            hasTransferredThisSession = false;
            TransferRecruitedNPCs();
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameModeChanged -= OnGameModeChanged;
            }
            
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnNPCsTransferredToCamp -= ShowTransferNotification;
            }
        }
    }
} 