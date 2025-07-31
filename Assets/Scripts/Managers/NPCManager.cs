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

        [Header("Settler Generation")]
        [SerializeField] private GameObject settlerPrefab; // Base settler prefab for procedural generation
        [SerializeField] private TextAsset settlerNamesFile; // Text file with settler names (one per line)
        [SerializeField] private Vector2Int ageRange = new Vector2Int(18, 65); // Min and max age for settlers
        [SerializeField] private TextAsset settlerDescriptionsFile; // Text file with description templates
        
        // Cached data for random generation
        private List<string> availableNames = new List<string>();
        private List<string> availableDescriptions = new List<string>();

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
            
            // Initialize settler generation data
            InitializeSettlerGenerationData();
        }

        private void OnGameModeChanged(GameMode newGameMode)
        {
            Debug.Log($"[NPCManager] OnGameModeChanged called with new mode: {newGameMode}");
            
            // Reset transfer state when leaving camp (allows transfers when returning)
            if (newGameMode != GameMode.CAMP && newGameMode != GameMode.CAMP_ATTACK)
            {
                Debug.Log("[NPCManager] Leaving camp mode - resetting transfer state");
                ResetTransferState();
            }
            
            // Check if we're returning to camp mode and have recruited NPCs to transfer
            if (newGameMode == GameMode.CAMP)
            {
                Debug.Log($"[NPCManager] Detected CAMP mode change");
                
                if (PlayerInventory.Instance != null)
                {
                    bool hasRecruitedNPCs = PlayerInventory.Instance.HasRecruitedNPCs();
                    Debug.Log($"[NPCManager] PlayerInventory found, hasRecruitedNPCs: {hasRecruitedNPCs}");
                    Debug.Log($"[NPCManager] hasTransferredThisSession: {hasTransferredThisSession}");
                    
                    if (hasRecruitedNPCs)
                    {
                        Debug.Log($"[NPCManager] Scheduling recruited NPC transfer in {transferDelay} seconds");
                        Invoke(nameof(TransferRecruitedNPCs), transferDelay);
                    }
                    else
                    {
                        Debug.Log("[NPCManager] No recruited NPCs found to transfer");
                    }
                }
                else
                {
                    Debug.LogWarning("[NPCManager] PlayerInventory.Instance is null during game mode change");
                }
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

        /// <summary>
        /// Get a characteristic by its name/ID (used for save/load)
        /// </summary>
        public NPCCharacteristicScriptableObj GetCharacteristicById(string characteristicId)
        {
            if (string.IsNullOrEmpty(characteristicId))
            {
                return null;
            }
            
            return allCharacteristics.Find(c => c.name == characteristicId);
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
            Debug.Log("[NPCManager] TransferRecruitedNPCs called");
            
            if (hasTransferredThisSession)
            {
                Debug.Log("[NPCManager] Transfer blocked - already transferred this session");
                return; // Prevent multiple transfers in the same session
            }

            if (PlayerInventory.Instance == null)
            {
                Debug.LogWarning("[NPCManager] PlayerInventory not found!");
                return;
            }

            if (!PlayerInventory.Instance.HasRecruitedNPCs())
            {
                Debug.Log("[NPCManager] No recruited NPCs found in PlayerInventory");
                return; // No NPCs to transfer
            }

            var recruitedNPCs = PlayerInventory.Instance.GetRecruitedSettlers();
            Debug.Log($"[NPCManager] Found {recruitedNPCs.Count} recruited NPCs to transfer");
            
            foreach (var npc in recruitedNPCs)
            {
                Debug.Log($"[NPCManager] - {npc.name} (Age {npc.age})");
            }
            
            // Subscribe to transfer event to show notification
            if (showTransferNotification)
            {
                PlayerInventory.Instance.OnNPCsTransferredToCamp += ShowTransferNotification;
            }

            // Transfer the NPCs
            Debug.Log("[NPCManager] Calling PlayerInventory.TransferRecruitedNPCsToCamp()");
            PlayerInventory.Instance.TransferRecruitedNPCsToCamp();
            
            hasTransferredThisSession = true;
            Debug.Log("[NPCManager] Transfer completed, hasTransferredThisSession set to true");
        }

        /// <summary>
        /// Show a notification when NPCs are transferred to camp
        /// </summary>
        private void ShowTransferNotification(System.Collections.Generic.List<SettlerData> transferredNPCs)
        {
            // Unsubscribe from event
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnNPCsTransferredToCamp -= ShowTransferNotification;
            }

            if (transferredNPCs.Count == 1)
            {
                Debug.Log($"[NPCManager] {transferredNPCs[0].name} has joined your camp!");
                
                // Show UI notification if available
                if (PlayerUIManager.Instance?.inventoryPopup != null)
                {
                    // TODO: You could create a specific "NPC Joined Camp" popup here
                    //PlayerUIManager.Instance.inventoryPopup.ShowInventoryPopup(transferredNPCs[0], 1, true);
                    Debug.LogWarning("[NPCManager] NPC Joined Camp popup not implemented yet");
                }
            }
            else
            {
                Debug.Log($"[NPCManager] {transferredNPCs.Count} new settlers have joined your camp!");
                
                // Show a summary notification for multiple NPCs
                foreach (var npc in transferredNPCs)
                {
                    Debug.Log($"  - {npc.name}");
                }
            }
        }

        /// <summary>
        /// Reset transfer state (useful for testing or scene transitions)
        /// </summary>
        public void ResetTransferState()
        {
            Debug.Log("[NPCManager] ResetTransferState called - clearing hasTransferredThisSession flag");
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

        #region Settler Generation

        /// <summary>
        /// Initialize settler generation data from text files
        /// </summary>
        private void InitializeSettlerGenerationData()
        {
            // Load names from text file
            if (settlerNamesFile != null)
            {
                string[] names = settlerNamesFile.text.Split('\n');
                availableNames.Clear();
                foreach (string name in names)
                {
                    string trimmedName = name.Trim();
                    if (!string.IsNullOrEmpty(trimmedName))
                    {
                        availableNames.Add(trimmedName);
                    }
                }
                Debug.Log($"[NPCManager] Loaded {availableNames.Count} settler names");
            }
            else
            {
                Debug.LogWarning("[NPCManager] No settler names file assigned! Using default names.");
                availableNames.AddRange(new string[] { "Alex", "Morgan", "Casey", "Jordan", "Taylor", "Riley", "Sage", "Quinn", "Avery", "Blake" });
            }

            // Load descriptions from text file
            if (settlerDescriptionsFile != null)
            {
                string[] descriptions = settlerDescriptionsFile.text.Split('\n');
                availableDescriptions.Clear();
                foreach (string description in descriptions)
                {
                    string trimmedDesc = description.Trim();
                    if (!string.IsNullOrEmpty(trimmedDesc))
                    {
                        availableDescriptions.Add(trimmedDesc);
                    }
                }
                Debug.Log($"[NPCManager] Loaded {availableDescriptions.Count} settler descriptions");
            }
            else
            {
                Debug.LogWarning("[NPCManager] No settler descriptions file assigned! Using default descriptions.");
                availableDescriptions.AddRange(new string[] {
                    "A hardworking individual with a strong sense of community.",
                    "Someone who values both independence and cooperation.",
                    "A resourceful person with varied life experiences.",
                    "An adaptable settler ready for new challenges.",
                    "A practical person with a positive outlook."
                });
            }
        }

        /// <summary>
        /// Get the settler prefab for spawning (appearance randomization happens via appearance system)
        /// </summary>
        public GameObject GetSettlerPrefab()
        {
            if (settlerPrefab == null)
            {
                Debug.LogError("[NPCManager] No settler prefab assigned!");
                return null;
            }

            return settlerPrefab;
        }

        /// <summary>
        /// Generate random settler data (name, age, description)
        /// </summary>
        public SettlerData GenerateRandomSettlerData()
        {
            string randomName = availableNames.Count > 0 ? 
                availableNames[UnityEngine.Random.Range(0, availableNames.Count)] : "Unknown Settler";
            
            int randomAge = UnityEngine.Random.Range(ageRange.x, ageRange.y + 1);
            
            string randomDescription = availableDescriptions.Count > 0 ? 
                availableDescriptions[UnityEngine.Random.Range(0, availableDescriptions.Count)] : "A mysterious settler.";

            return new SettlerData(randomName, randomAge, randomDescription);
        }

        /// <summary>
        /// Check if settler generation is properly configured
        /// </summary>
        public bool IsSettlerGenerationConfigured()
        {
            return settlerPrefab != null &&
                   availableNames.Count > 0 && availableDescriptions.Count > 0;
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

    /// <summary>
    /// Data structure for procedurally generated settler information
    /// </summary>
    [System.Serializable]
    public class SettlerData
    {
        public string name;
        public int age;
        public string description;

        public SettlerData(string name, int age, string description)
        {
            this.name = name;
            this.age = age;
            this.description = description;
        }
    }
} 