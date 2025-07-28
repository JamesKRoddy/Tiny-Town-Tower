using UnityEngine;
using Managers;

namespace Managers
{
    /// <summary>
    /// Handles transferring recruited NPCs from player inventory to camp when returning from roguelite
    /// </summary>
    public class CampNPCTransferManager : MonoBehaviour
    {
        public static CampNPCTransferManager Instance { get; private set; }

        [Header("Transfer Settings")]
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
                return;
            }
        }

        private void Start()
        {
            // Check if we need to transfer NPCs when camp scene loads
            if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasRecruitedNPCs())
            {
                Invoke(nameof(TransferRecruitedNPCs), transferDelay);
            }
        }

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
                Debug.LogWarning("[CampNPCTransferManager] PlayerInventory not found!");
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
            PlayerInventory.Instance.OnNPCsTransferredToCamp -= ShowTransferNotification;

            if (transferredNPCs.Count == 1)
            {
                Debug.Log($"[CampNPCTransferManager] {transferredNPCs[0].nPCName} has joined your camp!");
                
                // Show UI notification if available
                if (PlayerUIManager.Instance?.inventoryPopup != null)
                {
                    // You could create a specific "NPC Joined Camp" popup here
                    PlayerUIManager.Instance.inventoryPopup.ShowInventoryPopup(transferredNPCs[0], 1, true);
                }
            }
            else
            {
                Debug.Log($"[CampNPCTransferManager] {transferredNPCs.Count} new settlers have joined your camp!");
                
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

        private void OnDestroy()
        {
            // Clean up event subscription
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnNPCsTransferredToCamp -= ShowTransferNotification;
            }
        }
    }
} 