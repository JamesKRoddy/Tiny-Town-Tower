using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using Enemies;

namespace Managers
{
    public class CampManager : GameModeManager<CampEnemyWaveConfig>
    {
        private static CampManager _instance;
        public static CampManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CampManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("CampManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        [Header("Shared Placement Settings")]
        [SerializeField] private Vector2 sharedXBounds = new Vector2(-25f, 25f);
        [SerializeField] private Vector2 sharedZBounds = new Vector2(-25f, 25f);
        [SerializeField] private float sharedGridSize = 2f;
        [SerializeField] private bool showSharedGridBounds = true;

        [Header("Camp Wave Settings")]
        [SerializeField] private float waveEndCheckInterval = 2f;
        [SerializeField] private float waveLoopDelay = 5f; // Delay between waves
        [SerializeField] private int maxWavesPerLoop = 3; // Maximum waves before a longer break

        // Events
        public event Action OnCampWaveStarted;
        public event Action OnCampWaveEnded;
        public event Action OnWaveLoopComplete;

        // Shared grid system
        private Dictionary<Vector3, GridSlot> sharedGridSlots = new Dictionary<Vector3, GridSlot>();
        private bool gridObjectsInitialized = false;
        private Transform sharedGridParent;

        // Camp wave management
        private List<HumanCharacterController> campNPCs = new List<HumanCharacterController>();
        private float lastWaveEndCheck = 0f;
        private float waveStartTime = 0f;
        private float currentWaveDuration = 60f;
        private int currentWaveNumber = 0;
        private int wavesCompletedInLoop = 0;
        private Coroutine waveLoopCoroutine;

        // References to other managers
        private ResearchManager researchManager;
        private CleanlinessManager cleanlinessManager;
        private WorkManager workManager;
        private BuildManager buildManager;
        private CookingManager cookingManager;
        private ResourceUpgradeManager resourceUpgradeManager;
        private ElectricityManager electricityManager;
        private FarmingManager farmingManager;

        // Public access to other managers
        public ResearchManager ResearchManager => researchManager;
        public CleanlinessManager CleanlinessManager => cleanlinessManager;
        public WorkManager WorkManager => workManager;
        public BuildManager BuildManager => buildManager;
        public CookingManager CookingManager => cookingManager;
        public ResourceUpgradeManager ResourceUpgradeManager => resourceUpgradeManager;
        public ElectricityManager ElectricityManager => electricityManager;
        public FarmingManager FarmingManager => farmingManager;

        // Public access to shared placement settings
        public Vector2 SharedXBounds => sharedXBounds;
        public Vector2 SharedZBounds => sharedZBounds;
        public float SharedGridSize => sharedGridSize;
        public bool ShowSharedGridBounds => showSharedGridBounds;

        // Public access to shared grid
        public Dictionary<Vector3, GridSlot> SharedGridSlots => sharedGridSlots;

        // Public access to camp wave state
        public bool IsWaveActive => GetEnemySetupState() != EnemySetupState.ALL_WAVES_CLEARED;
        
        // Public access to current wave number
        public int GetCurrentWaveNumber() => currentWaveNumber;
        
        // Public access to current wave config max waves
        public int GetCurrentMaxWaves()
        {
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            int maxWaves = waveConfig?.maxWaves ?? maxWavesPerLoop;
            
            return maxWaves;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                InitializeManagers();
                InitializeSharedGrid();
            }
        }

        protected override void Start()
        {
            base.Start();
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            
            // Find all NPCs in the camp
            FindCampNPCs();
        }

        protected override void EnemySetupStateChanged(EnemySetupState newState)
        {
            Debug.Log($"<color=green>Camp EnemySetupStateChanged: {newState}</color>");

            switch (newState)
            {
                case EnemySetupState.NONE:
                    break;
                case EnemySetupState.WAVE_START:
                    EnemySpawnManager.Instance.ResetWaveCount();
                    // Move to next state after a short delay
                    StartCoroutine(TransitionToNextState(EnemySetupState.PRE_ENEMY_SPAWNING, 0.5f));
                    break;
                case EnemySetupState.PRE_ENEMY_SPAWNING:
                    SetupCampForWave();
                    // Move to spawn state after setup
                    StartCoroutine(TransitionToNextState(EnemySetupState.ENEMY_SPAWN_START, 1.0f));
                    break;
                case EnemySetupState.ENEMY_SPAWN_START:
                    StartCampEnemyWave();
                    // Move to spawned state after spawning
                    StartCoroutine(TransitionToNextState(EnemySetupState.ENEMIES_SPAWNED, 2.0f));
                    break;
                case EnemySetupState.ENEMIES_SPAWNED:
                    // Start wave timing
                    StartWaveTiming();
                    break;
                case EnemySetupState.ALL_WAVES_CLEARED:
                    EndCampWave();
                    break;
                default:
                    break;
            }
        }
        
        /// <summary>
        /// Start timing the current wave
        /// </summary>
        private void StartWaveTiming()
        {
            waveStartTime = Time.time;
            currentWaveNumber++;
            
            // Get wave duration from current wave config
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            if (waveConfig != null && waveConfig is CampEnemyWaveConfig campConfig)
            {
                currentWaveDuration = campConfig.WaveDuration;
            }
            
            Debug.Log($"Wave {currentWaveNumber} started. Duration: {currentWaveDuration}s");
        }

        private IEnumerator TransitionToNextState(EnemySetupState nextState, float delay)
        {
            yield return new WaitForSeconds(delay);
            SetEnemySetupState(nextState);
        }

        public override int GetCurrentWaveDifficulty()
        {
            // For camp waves, we can use a simple difficulty system
            // Could be based on camp level, number of buildings, etc.
            return 1; // Base difficulty for now
        }

        private void SetupCampForWave()
        {
            // Unpossess any currently possessed NPC
            if (PlayerController.Instance._possessedNPC != null)
            {
                PlayerController.Instance.PossessNPC(null);
            }
            
            // Make all NPCs flee
            MakeNPCsFlee();
        }

        private void StartCampEnemyWave()
        {
            // Get the wave config for current difficulty
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            if (waveConfig != null)
            {
                Debug.Log($"Starting camp wave with config: {waveConfig.name}, Difficulty: {GetCurrentWaveDifficulty()}");
                EnemySpawnManager.Instance.StartSpawningEnemies(waveConfig);
            }
            else
            {
                Debug.LogWarning($"No wave config found for camp wave difficulty {GetCurrentWaveDifficulty()}! Make sure to assign CampEnemyWaveConfig in the inspector.");
                // Don't immediately end the wave - let it continue with no enemies for testing
                Debug.Log("Continuing wave without enemies for testing purposes");
            }
        }

        private void Update()
        {
            // Only check for wave end when we're in the ENEMIES_SPAWNED state
            if (GetEnemySetupState() == EnemySetupState.ENEMIES_SPAWNED && 
                Time.time - lastWaveEndCheck >= waveEndCheckInterval)
            {
                lastWaveEndCheck = Time.time;
                CheckForWaveEnd();
            }
        }

        private void InitializeManagers()
        {
            // Create shared grid parent
            GameObject gridParentObj = new GameObject("SharedGridParent");
            gridParentObj.transform.SetParent(transform);
            sharedGridParent = gridParentObj.transform;

            // Find and cache references to other managers
            if (researchManager == null) researchManager = gameObject.GetComponentInChildren<ResearchManager>();
            if (cleanlinessManager == null) cleanlinessManager = gameObject.GetComponentInChildren<CleanlinessManager>();
            if (cookingManager == null) cookingManager = gameObject.GetComponentInChildren<CookingManager>();
            if (resourceUpgradeManager == null) resourceUpgradeManager = gameObject.GetComponentInChildren<ResourceUpgradeManager>();
            if (workManager == null) workManager = gameObject.GetComponentInChildren<WorkManager>();
            if (buildManager == null) buildManager = gameObject.GetComponentInChildren<BuildManager>();
            if (electricityManager == null) electricityManager = gameObject.GetComponentInChildren<ElectricityManager>();
            if (farmingManager == null) farmingManager = gameObject.GetComponentInChildren<FarmingManager>();
            // Log warnings for any missing managers
            if (researchManager == null) Debug.LogWarning("ResearchManager not found in scene!");
            if (cleanlinessManager == null) Debug.LogWarning("CleanlinessManager not found in scene!");
            if (cookingManager == null) Debug.LogWarning("CookingManager not found in scene!");
            if (resourceUpgradeManager == null) Debug.LogWarning("ResourceUpgradeManager not found in scene!");
            if (workManager == null) Debug.LogWarning("WorkManager not found in scene!");
            if (buildManager == null) Debug.LogWarning("BuildManager not found in scene!");
            if (electricityManager == null) Debug.LogWarning("ElectricityManager not found in scene!");
            if (farmingManager == null) Debug.LogWarning("FarmingManager not found in scene!");
            // Initialize managers
            if (researchManager != null) researchManager.Initialize();
            if (cookingManager != null) cookingManager.Initialize();
            if (resourceUpgradeManager != null) resourceUpgradeManager.Initialize();
            if (electricityManager != null) electricityManager.Initialize();
            if (cleanlinessManager != null) cleanlinessManager.Initialize();
            if (farmingManager != null) farmingManager.Initialize();
        }

        private void InitializeSharedGrid()
        {
            // Initialize the shared grid slots
            sharedGridSlots.Clear();
            
            for (float x = sharedXBounds.x; x < sharedXBounds.y; x += sharedGridSize)
            {
                for (float z = sharedZBounds.x; z < sharedZBounds.y; z += sharedGridSize)
                {
                    Vector3 gridPosition = new Vector3(x, 0, z);
                    sharedGridSlots[gridPosition] = new GridSlot { IsOccupied = false, FreeGridObject = null, TakenGridObject = null };
                }
            }
            
            Debug.Log($"Initialized shared grid with {sharedGridSlots.Count} slots");
        }

        // Shared grid methods that placers can use
        public bool AreSharedGridSlotsAvailable(Vector3 position, Vector2Int size)
        {
            List<Vector3> requiredSlots = GetRequiredSharedGridSlots(position, size);

            foreach (var slot in requiredSlots)
            {
                if (sharedGridSlots.ContainsKey(slot) && sharedGridSlots[slot].IsOccupied)
                {
                    return false;
                }
            }
            return true;
        }

        public void MarkSharedGridSlotsOccupied(Vector3 position, Vector2Int size, GameObject placedObject)
        {
            List<Vector3> requiredSlots = GetRequiredSharedGridSlots(position, size);

            foreach (var slot in requiredSlots)
            {
                if (sharedGridSlots.ContainsKey(slot))
                {
                    if (sharedGridSlots[slot].IsOccupied)
                    {
                        Debug.LogWarning($"Shared grid slot at {slot} is already occupied by {sharedGridSlots[slot].OccupyingObject.name}!");
                        continue; // Skip marking if already occupied
                    }

                    sharedGridSlots[slot].IsOccupied = true;
                    sharedGridSlots[slot].OccupyingObject = placedObject;
                    
                    // Update the visual representation
                    UpdateGridSlotVisual(slot, true);
                }
                else
                {
                    Debug.LogError($"Shared grid slot at {slot} does not exist in the dictionary!");
                }
            }
        }

        public void MarkSharedGridSlotsUnoccupied(Vector3 position, Vector2Int size)
        {
            List<Vector3> requiredSlots = GetRequiredSharedGridSlots(position, size);

            foreach (var slot in requiredSlots)
            {
                if (sharedGridSlots.ContainsKey(slot))
                {
                    sharedGridSlots[slot].IsOccupied = false;
                    sharedGridSlots[slot].OccupyingObject = null;
                    
                    // Update the visual representation
                    UpdateGridSlotVisual(slot, false);
                }
            }
        }

        private void UpdateGridSlotVisual(Vector3 slotPosition, bool isOccupied)
        {
            if (!sharedGridSlots.ContainsKey(slotPosition)) return;
            
            GridSlot slot = sharedGridSlots[slotPosition];
            Vector3 displayPosition = new Vector3(slotPosition.x + sharedGridSize / 2, 0, slotPosition.z + sharedGridSize / 2);
            
            // Actually update the visual grid object
            UpdateGridSlotVisualObject(slot, displayPosition, isOccupied);
        }

        private void UpdateGridSlotVisualObject(GridSlot slot, Vector3 displayPosition, bool isOccupied)
        {
            // Get the appropriate prefabs
            GameObject freePrefab = PlacementManager.Instance?.gridPrefab;
            GameObject takenPrefab = PlacementManager.Instance?.takenGridPrefab;
            
            if (freePrefab == null || takenPrefab == null) return;

            // Create free grid object if it doesn't exist
            if (slot.FreeGridObject == null)
            {
                slot.FreeGridObject = Instantiate(freePrefab, displayPosition, Quaternion.identity, sharedGridParent);
            }
            
            // Create taken grid object if it doesn't exist
            if (slot.TakenGridObject == null)
            {
                slot.TakenGridObject = Instantiate(takenPrefab, displayPosition, Quaternion.identity, sharedGridParent);
            }
            
            // Enable/disable the correct grid object based on occupation status
            if (slot.FreeGridObject != null)
            {
                slot.FreeGridObject.SetActive(!isOccupied && gridObjectsInitialized);
            }
            
            if (slot.TakenGridObject != null)
            {
                slot.TakenGridObject.SetActive(isOccupied && gridObjectsInitialized);
            }
        }

        private List<Vector3> GetRequiredSharedGridSlots(Vector3 position, Vector2Int size)
        {
            List<Vector3> requiredSlots = new List<Vector3>();

            Vector3 basePosition = SnapToSharedGrid(position);

            // Calculate the starting position (bottom-left corner)
            float startX = basePosition.x - ((size.x * sharedGridSize) / 2f);
            float startZ = basePosition.z - ((size.y * sharedGridSize) / 2f);

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector3 slotPosition = new Vector3(
                        startX + (x * sharedGridSize),
                        0,
                        startZ + (z * sharedGridSize)
                    );

                    requiredSlots.Add(slotPosition);
                }
            }

            return requiredSlots;
        }

        private Vector3 SnapToSharedGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / sharedGridSize) * sharedGridSize,
                0,
                Mathf.Round(position.z / sharedGridSize) * sharedGridSize
            );
        }

        public void ResetSharedGridObjects()
        {
            // Clear all grid objects from the shared grid
            foreach (var slot in sharedGridSlots.Values)
            {
                if (slot.FreeGridObject != null)
                {
                    DestroyImmediate(slot.FreeGridObject);
                    slot.FreeGridObject = null;
                }
                if (slot.TakenGridObject != null)
                {
                    DestroyImmediate(slot.TakenGridObject);
                    slot.TakenGridObject = null;
                }
            }
            gridObjectsInitialized = false;
        }

        public void InitializeGridObjects(GameObject gridPrefab, GameObject takenGridPrefab)
        {
            if (gridObjectsInitialized) return;

            var sharedGridSlots = CampManager.Instance?.SharedGridSlots;
            if (sharedGridSlots == null) return;

            foreach (var kvp in sharedGridSlots)
            {
                Vector3 gridPosition = kvp.Key;
                GridSlot slot = kvp.Value;
                
                Vector3 displayPosition = new Vector3(gridPosition.x + sharedGridSize / 2, 0, gridPosition.z + sharedGridSize / 2);
                
                // Use the new visual update method
                UpdateGridSlotVisualObject(slot, displayPosition, slot.IsOccupied);
            }
            
            gridObjectsInitialized = true;
        }

        public void ShowGridObjects()
        {
            var sharedGridSlots = CampManager.Instance?.SharedGridSlots;
            if (sharedGridSlots == null) return;

            foreach (var slot in sharedGridSlots.Values)
            {
                if (slot.FreeGridObject != null)
                {
                    slot.FreeGridObject.SetActive(!slot.IsOccupied);
                }
                if (slot.TakenGridObject != null)
                {
                    slot.TakenGridObject.SetActive(slot.IsOccupied);
                }
            }
        }

        public void HideGridObjects()
        {
            var sharedGridSlots = CampManager.Instance?.SharedGridSlots;
            if (sharedGridSlots == null) return;

            foreach (var slot in sharedGridSlots.Values)
            {
                if (slot.FreeGridObject != null)
                {
                    slot.FreeGridObject.SetActive(false);
                }
                if (slot.TakenGridObject != null)
                {
                    slot.TakenGridObject.SetActive(false);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (showSharedGridBounds)
            {
                Gizmos.color = Color.yellow; // Using yellow to distinguish shared grid
                Vector3 bottomLeft = new Vector3(sharedXBounds.x, 0, sharedZBounds.x);
                Vector3 bottomRight = new Vector3(sharedXBounds.y, 0, sharedZBounds.x);
                Vector3 topLeft = new Vector3(sharedXBounds.x, 0, sharedZBounds.y);
                Vector3 topRight = new Vector3(sharedXBounds.y, 0, sharedZBounds.y);

                Gizmos.DrawLine(bottomLeft, bottomRight);
                Gizmos.DrawLine(bottomRight, topRight);
                Gizmos.DrawLine(topRight, topLeft);
                Gizmos.DrawLine(topLeft, bottomLeft);
            }
        }

        #region Camp Wave Management

        /// <summary>
        /// Starts a camp wave of enemies
        /// </summary>
        public void StartCampWave()
        {
            if (GameManager.Instance.CurrentGameMode != GameMode.CAMP)
            {
                Debug.LogWarning("Camp wave can only be started in CAMP game mode!");
                return;
            }

            // Stop any existing wave loop
            if (waveLoopCoroutine != null)
            {
                StopCoroutine(waveLoopCoroutine);
                waveLoopCoroutine = null;
            }

            waveLoopCoroutine = StartCoroutine(SingleWaveCycle());
        }
        
        /// <summary>
        /// Starts a single wave
        /// </summary>
        private void StartSingleWave()
        {
            // Trigger camp wave started event
            OnCampWaveStarted?.Invoke();
            
            // Start the wave using the GameModeManager system
            SetEnemySetupState(EnemySetupState.WAVE_START);

            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_ATTACK_CAMERA_MOVEMENT);
            
            Debug.Log("Camp wave started!");
        }

        /// <summary>
        /// Makes all NPCs in the camp flee from enemies
        /// </summary>
        private void MakeNPCsFlee()
        {
            Debug.Log("Making NPCs flee from enemies!");
            
            foreach (var npc in campNPCs)
            {
                if (npc != null && npc is SettlerNPC settler)
                {
                    settler.ChangeTask(TaskType.FLEE);
                }
            }
        }

        /// <summary>
        /// Returns NPCs to normal behavior when wave ends
        /// </summary>
        private void ReturnNPCsToNormal()
        {
            Debug.Log("Returning NPCs to normal behavior");
            
            foreach (var npc in campNPCs)
            {
                if (npc != null && npc is SettlerNPC settler)
                {
                    settler.ChangeTask(TaskType.WANDER);
                }
            }
        }

        /// <summary>
        /// Checks if the wave should end (no enemies remaining)
        /// </summary>
        private void CheckForWaveEnd()
        {
            // Check if there are any active enemies
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            
            // Check if wave should end due to time or enemies cleared
            bool shouldEndWave = false;
            string endReason = "";
            
            if (enemies.Length == 0 && IsWaveActive)
            {
                // No enemies left, wave is over
                shouldEndWave = true;
                endReason = "All enemies defeated";
            }
            else if (IsWaveActive && Time.time - waveStartTime >= currentWaveDuration)
            {
                // Time's up, wave is over
                shouldEndWave = true;
                endReason = "Time expired";
            }
            
            if (shouldEndWave)
            {
                Debug.Log($"Wave ending: {endReason}");
                SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            }
        }

        /// <summary>
        /// Ends the current camp wave
        /// </summary>
        public void EndCampWave()
        {
            Debug.Log("Camp wave ended!");
            
            // Return NPCs to normal behavior
            ReturnNPCsToNormal();
            
            // Trigger camp wave ended event
            OnCampWaveEnded?.Invoke();

            // Return to camp camera movement
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_CAMERA_MOVEMENT);
        }
        
        /// <summary>
        /// Single wave cycle coroutine that runs one complete cycle of waves
        /// </summary>
        private IEnumerator SingleWaveCycle()
        {
            wavesCompletedInLoop = 0;
            
            // Get the wave config to determine max waves and multiple waves setting
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            int maxWaves = maxWavesPerLoop; // Default fallback
            
            if (waveConfig != null)
            {
                maxWaves = waveConfig.maxWaves;
            }
            else
            {
                Debug.LogWarning($"No wave config found, using default maxWavesPerLoop: {maxWavesPerLoop}");
            }
            
            Debug.Log($"Starting single wave cycle with {maxWaves} waves");
            
            while (wavesCompletedInLoop < maxWaves)
            {
                // Start a single wave
                StartSingleWave();
                
                // Wait for wave to complete
                while (IsWaveActive)
                {
                    yield return null;
                }
                
                wavesCompletedInLoop++;
                Debug.Log($"Wave {wavesCompletedInLoop} completed. Waves in cycle: {wavesCompletedInLoop}/{maxWaves}");
                
                // If we haven't reached max waves, wait before next wave
                if (wavesCompletedInLoop < maxWaves)
                {
                    Debug.Log($"Waiting {waveLoopDelay} seconds before next wave...");
                    yield return new WaitForSeconds(waveLoopDelay);
                }
            }
            
            // Cycle complete
            Debug.Log($"Single wave cycle completed! Total waves: {wavesCompletedInLoop}/{maxWaves}");
            OnWaveLoopComplete?.Invoke();
            
            // Don't start another cycle - this is single cycle mode
            waveLoopCoroutine = null;
        }
        
        /// <summary>
        /// Wave loop coroutine that handles continuous waves
        /// </summary>
        private IEnumerator WaveLoop()
        {
            wavesCompletedInLoop = 0;
            
            // Get the wave config to determine max waves and multiple waves setting
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            int maxWaves = maxWavesPerLoop; // Default fallback
            
            if (waveConfig != null)
            {
                maxWaves = waveConfig.maxWaves;
                        
            }
            else
            {
                Debug.LogWarning($"No wave config found, using default maxWavesPerLoop: {maxWavesPerLoop}");
            }
            
            while (wavesCompletedInLoop < maxWaves)
            {
                // Start a single wave
                StartSingleWave();
                
                // Wait for wave to complete
                while (IsWaveActive)
                {
                    yield return null;
                }
                
                wavesCompletedInLoop++;
                Debug.Log($"Wave {wavesCompletedInLoop} completed. Waves in loop: {wavesCompletedInLoop}/{maxWaves}");
                
                // If we haven't reached max waves, wait before next wave
                if (wavesCompletedInLoop < maxWaves)
                {
                    Debug.Log($"Waiting {waveLoopDelay} seconds before next wave...");
                    yield return new WaitForSeconds(waveLoopDelay);
                }
            }
            
            // Loop complete
            Debug.Log($"Wave loop completed! Total waves: {wavesCompletedInLoop}/{maxWaves}");
            OnWaveLoopComplete?.Invoke();
            
            // Optional: Wait longer before starting a new loop
            yield return new WaitForSeconds(waveLoopDelay * 2);
            waveLoopCoroutine = StartCoroutine(WaveLoop());
        }

        /// <summary>
        /// Manually end the camp wave (for debugging)
        /// </summary>
        public void ForceEndCampWave()
        {
            Debug.Log("Force ending camp wave!");
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_CAMERA_MOVEMENT);
        }

        /// <summary>
        /// Finds all NPCs in the camp for wave management
        /// </summary>
        private void FindCampNPCs()
        {
            campNPCs.Clear();
            HumanCharacterController[] npcs = FindObjectsByType<HumanCharacterController>(FindObjectsSortMode.None);
            
            foreach (var npc in npcs)
            {
                // Exclude the player if they're possessed
                if (npc != PlayerController.Instance._possessedNPC)
                {
                    campNPCs.Add(npc);
                }
            }
            
            Debug.Log($"Found {campNPCs.Count} NPCs in camp for wave management");
        }

        /// <summary>
        /// Add an NPC to the camp wave manager
        /// </summary>
        public void AddNPC(HumanCharacterController npc)
        {
            if (npc != null && !campNPCs.Contains(npc))
            {
                campNPCs.Add(npc);
            }
        }

        /// <summary>
        /// Remove an NPC from the camp wave manager
        /// </summary>
        public void RemoveNPC(HumanCharacterController npc)
        {
            if (campNPCs.Contains(npc))
                campNPCs.Remove(npc);
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Spawn a single enemy for debugging purposes
        /// </summary>
        public void SpawnSingleEnemy()
        {
            // Get the wave config for current difficulty
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            if (waveConfig == null || waveConfig.enemyPrefabs == null || waveConfig.enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("No enemy prefabs found in wave config!");
                return;
            }

            // Get a random spawn position within bounds
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            // Spawn a random enemy prefab from the wave config
            GameObject enemyPrefab = waveConfig.enemyPrefabs[UnityEngine.Random.Range(0, waveConfig.enemyPrefabs.Length)];
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            
            Debug.Log($"Spawned single enemy at {spawnPosition}");
        }

        /// <summary>
        /// Clear all enemies from the scene
        /// </summary>
        public void ClearAllEnemies()
        {
            // Find all enemies in the scene
            EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            
            Debug.Log($"Cleared {enemies.Length} enemies from the scene");
        }
        
        /// <summary>
        /// Clear all enemies with fade effect
        /// </summary>
        public void ClearAllEnemiesWithFade()
        {
            StartCoroutine(ClearEnemiesWithFadeCoroutine());
        }
        
        /// <summary>
        /// Coroutine to clear enemies with fade effect
        /// </summary>
        private IEnumerator ClearEnemiesWithFadeCoroutine()
        {
            // Find all enemies in the scene
            EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            
            if (enemies.Length == 0)
            {
                yield break;
            }
            
            Debug.Log($"Clearing {enemies.Length} enemies with fade effect");
            
            // Fade out enemies
            float fadeDuration = 1f;
            float elapsedTime = 0f;
            
            // Store original materials
            Dictionary<EnemyBase, Material> originalMaterials = new Dictionary<EnemyBase, Material>();
            Dictionary<EnemyBase, SkinnedMeshRenderer> renderers = new Dictionary<EnemyBase, SkinnedMeshRenderer>();
            
            // Setup fade materials
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    var renderer = enemy.GetComponent<SkinnedMeshRenderer>();
                    if (renderer != null)
                    {
                        renderers[enemy] = renderer;
                        originalMaterials[enemy] = renderer.material;
                        
                        // Create fade material
                        Material fadeMaterial = new Material(renderer.material);
                        fadeMaterial.SetFloat("_Mode", 3); // Transparent mode
                        fadeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        fadeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        fadeMaterial.SetInt("_ZWrite", 0);
                        fadeMaterial.DisableKeyword("_ALPHATEST_ON");
                        fadeMaterial.EnableKeyword("_ALPHABLEND_ON");
                        fadeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        fadeMaterial.renderQueue = 3000;
                        
                        renderer.material = fadeMaterial;
                    }
                }
            }
            
            // Fade out
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                
                foreach (var kvp in renderers)
                {
                    if (kvp.Key != null && kvp.Value != null)
                    {
                        Color color = kvp.Value.material.color;
                        color.a = alpha;
                        kvp.Value.material.color = color;
                    }
                }
                
                yield return null;
            }
            
            // Destroy enemies
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            
            // Clean up materials
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Value != null)
                {
                    DestroyImmediate(kvp.Value);
                }
            }
            
            Debug.Log("Enemies cleared with fade effect");
        }

        /// <summary>
        /// Get a random spawn position within the camp bounds
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            float x = UnityEngine.Random.Range(sharedXBounds.x, sharedXBounds.y);
            float z = UnityEngine.Random.Range(sharedZBounds.x, sharedZBounds.y);
            
            // Find a position on the ground
            Vector3 spawnPos = new Vector3(x, 100f, z); // Start high up
            RaycastHit hit;
            
            if (Physics.Raycast(spawnPos, Vector3.down, out hit, 200f, LayerMask.GetMask("Default")))
            {
                return hit.point;
            }
            
            return new Vector3(x, 0f, z);
        }

        #endregion
    }
}