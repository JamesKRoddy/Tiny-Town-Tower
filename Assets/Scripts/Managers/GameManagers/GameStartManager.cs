using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CampBuilding;

namespace Managers
{
    /// <summary>
    /// Manages game start and restart scenarios.
    /// Handles spawning NPCs for computer boot sequence and resetting camp state on restart.
    /// </summary>
    public class GameStartManager : MonoBehaviour
    {
        #region Singleton
        
        private static GameStartManager _instance;
        public static GameStartManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameStartManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("GameStartManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Inspector Fields

        [Header("Game Start Settings")]
        [SerializeField] private Vector2Int startingNPCRange = new Vector2Int(2, 4);
        [SerializeField] private Transform[] npcSpawnPoints;
        [SerializeField] private float npcSpawnDelay = 0.5f;

        [Header("Restart Settings")]
        [SerializeField] private float buildingDamagePercent = 0.4f; // Buildings lose 40% health
        [SerializeField] private float researchProgressLossPercent = 1.0f; // Lose 100% of current research progress
        [SerializeField] private bool clearAllBuildingQueues = true;
        [SerializeField] private bool removeAllCrops = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogging = true;

        #endregion

        #region Private Fields

        private bool isInitialized = false;
        private List<SettlerNPC> startupNPCs = new List<SettlerNPC>();

        #endregion

        #region Events

        public event System.Action OnGameStartComplete;
        public event System.Action OnGameRestartComplete;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the game based on whether it's a new game or a restart
        /// </summary>
        public void InitializeGame()
        {
            if (isInitialized)
            {
                Log("Game already initialized, skipping.");
                return;
            }

            // Check if this is a new game or a restart
            bool isNewGame = !SaveLoadManager.Instance.HasSaveFile();
            bool isRestart = SaveLoadManager.Instance.HasSaveFile() && GetNPCCount() == 0;

            if (isNewGame)
            {
                Log("Starting NEW GAME sequence...");
                StartCoroutine(StartNewGameSequence());
            }
            else if (isRestart)
            {
                Log("Starting RESTART sequence (all NPCs lost)...");
                StartCoroutine(RestartGameSequence());
            }
            else
            {
                Log("Loading existing game with NPCs...");
                isInitialized = true;
            }
        }

        /// <summary>
        /// Force a game restart (called when all NPCs are lost)
        /// </summary>
        public void TriggerGameRestart()
        {
            Log("Game restart triggered - all NPCs lost!");
            StartCoroutine(RestartGameSequence());
        }

        #endregion

        #region New Game Sequence

        private IEnumerator StartNewGameSequence()
        {
            Log("=== NEW GAME SEQUENCE START ===");

            // Pause camp systems during initialization
            PauseCampSystems();

            // Show the game start UI
            if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.gameStartMenu != null)
            {
                // Subscribe to continue button event
                PlayerUIManager.Instance.gameStartMenu.OnContinuePressed += OnContinueButtonPressed;
                
                PlayerUIManager.Instance.gameStartMenu.SetScreenActive(true);
                yield return new WaitForSeconds(1f); // Brief delay before starting
            }

            // Determine number of starting NPCs
            int npcCount = Random.Range(startingNPCRange.x, startingNPCRange.y + 1);
            Log($"Spawning {npcCount} NPCs to turn on the computer...");

            // Spawn the starting NPCs
            yield return StartCoroutine(SpawnStartingNPCs(npcCount));

            // Wait a bit for dramatic effect
            yield return new WaitForSeconds(2f);

            // Update UI with progress and show button
            if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.gameStartMenu != null)
            {
                PlayerUIManager.Instance.gameStartMenu.UpdateDescription("System initialized. Survivors ready.\n\nPress to continue...", animateDots: false);
                
                // Wait for player to press the button
                yield return StartCoroutine(PlayerUIManager.Instance.gameStartMenu.WaitForButtonPress());
            }

            // Close the start menu
            if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.gameStartMenu != null)
            {
                // Unsubscribe from event
                PlayerUIManager.Instance.gameStartMenu.OnContinuePressed -= OnContinueButtonPressed;
                
                PlayerUIManager.Instance.gameStartMenu.SetScreenActive(false);
            }

            isInitialized = true;
            OnGameStartComplete?.Invoke();

            Log("=== NEW GAME SEQUENCE COMPLETE ===");
        }

        /// <summary>
        /// Called when the player presses the continue button
        /// </summary>
        private void OnContinueButtonPressed()
        {
            Log("Continue button pressed - resuming camp systems");
            ResumeCampSystems();
        }

        private IEnumerator SpawnStartingNPCs(int count)
        {
            startupNPCs.Clear();

            // Get spawn points (use default positions if none set)
            Vector3[] spawnPositions = GetSpawnPositions(count);

            for (int i = 0; i < count; i++)
            {
                // Spawn NPC
                GameObject npcPrefab = NPCManager.Instance?.GetSettlerPrefab();
                if (npcPrefab != null)
                {
                    Vector3 spawnPos = spawnPositions[i];
                    GameObject npcObj = Instantiate(npcPrefab, spawnPos, Quaternion.identity);
                    
                    SettlerNPC settler = npcObj.GetComponent<SettlerNPC>();
                    if (settler != null)
                    {
                        // Generate random settler data
                        SettlerData settlerData = NPCManager.Instance?.GenerateRandomSettlerData();
                        if (settlerData != null)
                        {
                            settler.ApplySettlerData(settlerData);
                        }
                        
                        // Set initialization context to CAMP_SPAWN (spawned directly in camp, not from roguelike)
                        settler.SetInitializationContext(NPCInitializationContext.CAMP_SPAWN);
                        
                        startupNPCs.Add(settler);
                        
                        Log($"Spawned starting NPC: {settler.SettlerName}");
                    }

                    // Stagger spawns
                    if (i < count - 1)
                    {
                        yield return new WaitForSeconds(npcSpawnDelay);
                    }
                }
                else
                {
                    Debug.LogError("Failed to spawn NPC - NPCManager not configured!");
                    break;
                }
            }
        }

        #endregion

        #region Restart Game Sequence

        private IEnumerator RestartGameSequence()
        {
            Log("=== GAME RESTART SEQUENCE START ===");

            // Pause camp systems during restart
            PauseCampSystems();

            // Show the restart UI
            if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.gameStartMenu != null)
            {
                // Subscribe to continue button event
                PlayerUIManager.Instance.gameStartMenu.OnContinuePressed += OnContinueButtonPressed;
                
                PlayerUIManager.Instance.gameStartMenu.SetScreenActive(true);
                PlayerUIManager.Instance.gameStartMenu.UpdateDescription("Rebooting system... Survivors detected nearby.", animateDots: true);
                yield return new WaitForSeconds(1f);
            }

            // Reset the camp state
            ResetCampState();
            yield return new WaitForSeconds(0.5f);

            // Spawn new NPCs to restart
            int npcCount = Random.Range(startingNPCRange.x, startingNPCRange.y + 1);
            Log($"Spawning {npcCount} new NPCs for restart...");
            yield return StartCoroutine(SpawnStartingNPCs(npcCount));

            // Wait a bit
            yield return new WaitForSeconds(2f);

            // Update UI and show button
            if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.gameStartMenu != null)
            {
                PlayerUIManager.Instance.gameStartMenu.UpdateDescription("System restored. New survivors ready.\n\nPress to continue...", animateDots: false);
                
                // Wait for player to press the button
                yield return StartCoroutine(PlayerUIManager.Instance.gameStartMenu.WaitForButtonPress());
            }

            // Close the restart menu
            if (PlayerUIManager.Instance != null && PlayerUIManager.Instance.gameStartMenu != null)
            {
                // Unsubscribe from event
                PlayerUIManager.Instance.gameStartMenu.OnContinuePressed -= OnContinueButtonPressed;
                
                PlayerUIManager.Instance.gameStartMenu.SetScreenActive(false);
            }

            isInitialized = true;
            OnGameRestartComplete?.Invoke();

            Log("=== GAME RESTART SEQUENCE COMPLETE ===");
        }

        #endregion

        #region Camp Reset Logic

        /// <summary>
        /// Reset the camp state for restart scenario
        /// </summary>
        private void ResetCampState()
        {
            Log("Resetting camp state...");

            // 0. Clean up dead NPCs first
            CleanupDeadNPCs();

            // 1. Damage all buildings
            DamageAllBuildings();

            // 2. Reset research progress
            ResetResearchProgress();

            // 3. Clear all building task queues
            if (clearAllBuildingQueues)
            {
                ClearAllBuildingQueues();
            }

            // 4. Remove all farm crops
            if (removeAllCrops)
            {
                RemoveAllCrops();
            }

            // 5. Clear any in-progress work tasks
            ClearInProgressTasks();

            // 6. Reset cleanliness (optional - starts fresh)
            ResetCleanliness();

            Log("Camp state reset complete.");
        }

        /// <summary>
        /// Clean up all dead NPC GameObjects from the scene during restart
        /// Delegates to CampManager which tracks dead NPCs
        /// </summary>
        private void CleanupDeadNPCs()
        {
            if (CampManager.Instance != null)
            {
                int deadCount = CampManager.Instance.GetTotalDeadNPCs();
                Log($"Cleaning up {deadCount} dead NPC(s)...");
                CampManager.Instance.CleanupDeadNPCs();
            }
            else
            {
                Log("CampManager not available for dead NPC cleanup");
            }
        }

        private void DamageAllBuildings()
        {
            Building[] buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            int damagedCount = 0;

            foreach (Building building in buildings)
            {
                if (building.IsUnderConstruction())
                    continue;

                float damageAmount = building.GetMaxHealth() * buildingDamagePercent;
                var damageInfo = DamageInfo.Environmental(damageAmount, playHitVFX: false);
                building.TakeDamage(damageInfo);
                damagedCount++;
            }

            Log($"Damaged {damagedCount} buildings by {buildingDamagePercent * 100}%");
        }

        private void ResetResearchProgress()
        {
            if (CampManager.Instance?.ResearchManager == null)
                return;

            // Clear currently researching items
            CampManager.Instance.ResearchManager.ClearCurrentlyResearching();

            // Find all research tasks and reset their progress
            ResearchTask[] researchTasks = FindObjectsByType<ResearchTask>(FindObjectsSortMode.None);
            foreach (ResearchTask task in researchTasks)
            {
                task.ClearQueue();
                task.ResetProgress();
            }

            Log($"Reset research progress on {researchTasks.Length} research buildings");
        }

        private void ClearAllBuildingQueues()
        {
            int clearedCount = 0;

            // Clear cooking queues
            CookingTask[] cookingTasks = FindObjectsByType<CookingTask>(FindObjectsSortMode.None);
            foreach (CookingTask task in cookingTasks)
            {
                task.ClearQueue();
                clearedCount++;
            }

            // Clear research queues (already done above, but for consistency)
            ResearchTask[] researchTasks = FindObjectsByType<ResearchTask>(FindObjectsSortMode.None);
            foreach (ResearchTask task in researchTasks)
            {
                task.ClearQueue();
                clearedCount++;
            }

            // Any other queued tasks can be added here

            Log($"Cleared queues on {clearedCount} buildings");
        }

        private void RemoveAllCrops()
        {
            FarmBuilding[] farms = FindObjectsByType<FarmBuilding>(FindObjectsSortMode.None);
            int clearedCount = 0;

            foreach (FarmBuilding farm in farms)
            {
                if (farm.IsOccupied)
                {
                    farm.ClearPlot();
                    clearedCount++;
                }
            }

            Log($"Removed crops from {clearedCount} farm plots");
        }

        private void ClearInProgressTasks()
        {
            if (CampManager.Instance?.WorkManager == null)
                return;

            // Clear the work queue
            CampManager.Instance.WorkManager.ClearAllTasks();

            // Find all work tasks and reset them
            WorkTask[] allTasks = FindObjectsByType<WorkTask>(FindObjectsSortMode.None);
            foreach (WorkTask task in allTasks)
            {
                task.StopAllWork();
            }

            Log($"Cleared {allTasks.Length} in-progress work tasks");
        }

        private void ResetCleanliness()
        {
            if (CampManager.Instance?.CleanlinessManager == null)
                return;

            // Reset cleanliness to default state
            CampManager.Instance.CleanlinessManager.ResetCleanliness();
            Log("Reset cleanliness state");
        }

        #endregion

        #region Helper Methods

        private Vector3[] GetSpawnPositions(int count)
        {
            Vector3[] positions = new Vector3[count];

            if (npcSpawnPoints != null && npcSpawnPoints.Length > 0)
            {
                // Use defined spawn points
                for (int i = 0; i < count; i++)
                {
                    int spawnIndex = i % npcSpawnPoints.Length;
                    positions[i] = npcSpawnPoints[spawnIndex].position;
                }
            }
            else
            {
                // Generate spawn positions in a circle around origin
                float radius = 5f;
                for (int i = 0; i < count; i++)
                {
                    float angle = (i / (float)count) * 360f * Mathf.Deg2Rad;
                    positions[i] = new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius
                    );
                }
            }

            return positions;
        }

        private int GetNPCCount()
        {
            return NPCManager.Instance?.TotalNPCs ?? 0;
        }

        private void Log(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"[GameStartManager] {message}");
            }
        }

        #endregion

        #region Camp System Pause/Resume

        /// <summary>
        /// Pause camp systems during game start sequence
        /// </summary>
        private void PauseCampSystems()
        {
            Log("Pausing camp systems...");

            // Pause time manager
            if (GameManager.Instance?.TimeManager != null)
            {
                GameManager.Instance.TimeManager.PauseTime();
            }

            // Pause cleanliness manager
            if (CampManager.Instance?.CleanlinessManager != null)
            {
                CampManager.Instance.CleanlinessManager.SetPaused(true);
            }

            // Pause electricity manager
            if (CampManager.Instance?.ElectricityManager != null)
            {
                CampManager.Instance.ElectricityManager.SetPaused(true);
            }

            Log("Camp systems paused");
        }

        /// <summary>
        /// Resume camp systems after game start sequence
        /// </summary>
        private void ResumeCampSystems()
        {
            Log("Resuming camp systems...");

            // Resume time manager
            if (GameManager.Instance?.TimeManager != null)
            {
                GameManager.Instance.TimeManager.ResumeTime();
            }

            // Resume cleanliness manager
            if (CampManager.Instance?.CleanlinessManager != null)
            {
                CampManager.Instance.CleanlinessManager.SetPaused(false);
            }

            // Resume electricity manager
            if (CampManager.Instance?.ElectricityManager != null)
            {
                CampManager.Instance.ElectricityManager.SetPaused(false);
            }

            Log("Camp systems resumed");
        }

        #endregion
    }
}

