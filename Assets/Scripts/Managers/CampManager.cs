using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using Enemies;

namespace Managers
{
    public class CampManager : GameModeManager<CampEnemyWaveConfig>
    {
        #region Singleton Pattern
        
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

        #endregion

        #region Inspector Fields

        [Header("Shared Placement Settings")]
        [SerializeField] private Vector2 sharedXBounds = new Vector2(-25f, 25f);
        [SerializeField] private Vector2 sharedZBounds = new Vector2(-25f, 25f);
        [SerializeField] private float sharedGridSize = 2f;
        [SerializeField] private bool showSharedGridBounds = true;

        [Header("Camp Wave Settings")]
        [SerializeField] private float waveEndCheckInterval = 2f;
        [SerializeField] private float waveLoopDelay = 5f;
        [SerializeField] private int maxWavesPerLoop = 3;

        #endregion

        #region Events

        public event Action OnCampWaveStarted;
        public event Action OnCampWaveEnded;
        public event Action OnWaveLoopComplete;
        public event Action OnWaveCycleComplete;

        #endregion

        #region Private Fields

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

        // Cached target lists for efficient checking
        private List<IDamageable> cachedTargets = new List<IDamageable>();

        // References to other managers
        private ResearchManager researchManager;
        private CleanlinessManager cleanlinessManager;
        private WorkManager workManager;
        private BuildManager buildManager;
        private CookingManager cookingManager;
        private ResourceUpgradeManager resourceUpgradeManager;
        private ElectricityManager electricityManager;
        private FarmingManager farmingManager;

        #endregion

        #region Public Properties

        // Manager references
        public ResearchManager ResearchManager => researchManager;
        public CleanlinessManager CleanlinessManager => cleanlinessManager;
        public WorkManager WorkManager => workManager;
        public BuildManager BuildManager => buildManager;
        public CookingManager CookingManager => cookingManager;
        public ResourceUpgradeManager ResourceUpgradeManager => resourceUpgradeManager;
        public ElectricityManager ElectricityManager => electricityManager;
        public FarmingManager FarmingManager => farmingManager;

        // Shared placement settings
        public Vector2 SharedXBounds => sharedXBounds;
        public Vector2 SharedZBounds => sharedZBounds;
        public float SharedGridSize => sharedGridSize;
        public bool ShowSharedGridBounds => showSharedGridBounds;

        // Shared grid
        public Dictionary<Vector3, GridSlot> SharedGridSlots => sharedGridSlots;

        // Wave state
        public bool IsWaveActive => GetEnemySetupState() != EnemySetupState.ALL_WAVES_CLEARED;
        public int GetCurrentWaveNumber() => currentWaveNumber;
        
        public int GetCurrentMaxWaves()
        {
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            return waveConfig?.maxWaves ?? maxWavesPerLoop;
        }

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
                InitializeManagers();
                InitializeSharedGrid();
            }
        }

        protected override void Start()
        {
            base.Start();
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            
            FindCampNPCs();
            PopulateTargetCache();
        }

        private void Update()
        {
            if (GetEnemySetupState() == EnemySetupState.ENEMIES_SPAWNED && 
                Time.time - lastWaveEndCheck >= waveEndCheckInterval)
            {
                lastWaveEndCheck = Time.time;
                CheckForWaveEnd();
            }
            
            // Periodic cleanup of target cache (every 10 seconds)
            if (Time.frameCount % 600 == 0)
            {
                CleanupTargetCache();
            }
        }

        private void OnDrawGizmos()
        {
            if (showSharedGridBounds)
            {
                Gizmos.color = Color.yellow;
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

        #endregion

        #region Initialization

        private void InitializeManagers()
        {
            // Create shared grid parent
            GameObject gridParentObj = new GameObject("SharedGridParent");
            gridParentObj.transform.SetParent(transform);
            sharedGridParent = gridParentObj.transform;

            // Find and cache references to other managers
            FindManagerReferences();
            LogMissingManagers();
            InitializeAllManagers();
        }

        private void FindManagerReferences()
        {
            researchManager = GetComponentInChildren<ResearchManager>();
            cleanlinessManager = GetComponentInChildren<CleanlinessManager>();
            cookingManager = GetComponentInChildren<CookingManager>();
            resourceUpgradeManager = GetComponentInChildren<ResourceUpgradeManager>();
            workManager = GetComponentInChildren<WorkManager>();
            buildManager = GetComponentInChildren<BuildManager>();
            electricityManager = GetComponentInChildren<ElectricityManager>();
            farmingManager = GetComponentInChildren<FarmingManager>();
        }

        private void LogMissingManagers()
        {
            if (researchManager == null) Debug.LogWarning("ResearchManager not found in scene!");
            if (cleanlinessManager == null) Debug.LogWarning("CleanlinessManager not found in scene!");
            if (cookingManager == null) Debug.LogWarning("CookingManager not found in scene!");
            if (resourceUpgradeManager == null) Debug.LogWarning("ResourceUpgradeManager not found in scene!");
            if (workManager == null) Debug.LogWarning("WorkManager not found in scene!");
            if (buildManager == null) Debug.LogWarning("BuildManager not found in scene!");
            if (electricityManager == null) Debug.LogWarning("ElectricityManager not found in scene!");
            if (farmingManager == null) Debug.LogWarning("FarmingManager not found in scene!");
        }

        private void InitializeAllManagers()
        {
            researchManager?.Initialize();
            cookingManager?.Initialize();
            resourceUpgradeManager?.Initialize();
            electricityManager?.Initialize();
            cleanlinessManager?.Initialize();
            farmingManager?.Initialize();
        }

        private void InitializeSharedGrid()
        {
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

        #endregion

        #region Target Management

        /// <summary>
        /// Check if there are any available buildings or NPCs for enemies to target
        /// </summary>
        public bool AreTargetsAvailable()
        {
            foreach (var target in cachedTargets)
            {
                if (target == null || target.Health <= 0) continue;
                
                if (target is Building || target is BaseTurret)
                {
                    return true;
                }
                else if (target is HumanCharacterController npc)
                {
                    if (npc != PlayerController.Instance._possessedNPC)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Populate the initial target cache with all existing objects
        /// </summary>
        private void PopulateTargetCache()
        {
            cachedTargets.Clear();

            // Find all buildings
            Building[] buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            cachedTargets.AddRange(buildings);

            // Find all turrets
            var turrets = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var turret in turrets)
            {
                if (turret != null && turret is IDamageable damageableTurret)
                {
                    cachedTargets.Add(damageableTurret);
                }
            }

            // Find all NPCs
            HumanCharacterController[] npcs = FindObjectsByType<HumanCharacterController>(FindObjectsSortMode.None);
            cachedTargets.AddRange(npcs);

            Debug.Log($"Populated target cache: {cachedTargets.Count} targets");
        }

        /// <summary>
        /// Register any IDamageable target in the target cache
        /// </summary>
        public void RegisterTarget(IDamageable target)
        {
            if (target != null && !cachedTargets.Contains(target))
            {
                cachedTargets.Add(target);
                target.OnDeath += OnTargetDied;
                string targetName = (target as MonoBehaviour)?.name ?? target.GetType().Name;
                Debug.Log($"Registered target: {target.GetType().Name} - {targetName}");
            }
        }

        /// <summary>
        /// Unregister any IDamageable target from the target cache
        /// </summary>
        public void UnregisterTarget(IDamageable target)
        {
            if (cachedTargets.Remove(target))
            {
                target.OnDeath -= OnTargetDied;
                string targetName = (target as MonoBehaviour)?.name ?? target.GetType().Name;
                Debug.Log($"Unregistered target: {target.GetType().Name} - {targetName}");
            }
        }

        /// <summary>
        /// Clean up null references from the target cache
        /// </summary>
        public void CleanupTargetCache()
        {
            cachedTargets.RemoveAll(target => target == null);
        }

        /// <summary>
        /// Called when a target dies - check if any targets remain
        /// </summary>
        private void OnTargetDied()
        {
            if (GetEnemySetupState() == EnemySetupState.ENEMIES_SPAWNED && !AreTargetsAvailable())
            {
                Debug.Log("No targets remaining - ending wave!");
                ForceEndWavesNoTargets();
            }
        }

        /// <summary>
        /// Public method for enemies to check if they can find targets
        /// </summary>
        public bool CheckTargetsForEnemies()
        {
            if (!AreTargetsAvailable())
            {
                Debug.Log("No targets available for enemies - ending wave!");
                ForceEndWavesNoTargets();
                return false;
            }
            return true;
        }

        #endregion

        #region Wave Management

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

            if (!AreTargetsAvailable())
            {
                Debug.Log("No buildings or NPCs available - cannot start camp wave!");
                return;
            }

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
            if (!AreTargetsAvailable())
            {
                Debug.Log("No buildings or NPCs available - cannot start wave!");
                ForceEndWavesNoTargets();
                return;
            }
            
            OnCampWaveStarted?.Invoke();
            SetEnemySetupState(EnemySetupState.WAVE_START);
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_ATTACK_CAMERA_MOVEMENT);
            
            Debug.Log("Camp wave started!");
        }

        /// <summary>
        /// Single wave cycle coroutine that runs one complete cycle of waves
        /// </summary>
        private IEnumerator SingleWaveCycle()
        {
            wavesCompletedInLoop = 0;
            
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            int maxWaves = waveConfig?.maxWaves ?? maxWavesPerLoop;
            
            Debug.Log($"Starting single wave cycle with {maxWaves} waves");
            
            while (wavesCompletedInLoop < maxWaves)
            {
                if (!AreTargetsAvailable())
                {
                    Debug.Log("No buildings or NPCs available - ending wave cycle early!");
                    break;
                }
                
                StartSingleWave();
                
                while (IsWaveActive)
                {
                    yield return null;
                }
                
                wavesCompletedInLoop++;
                Debug.Log($"Wave {wavesCompletedInLoop} completed. Waves in cycle: {wavesCompletedInLoop}/{maxWaves}");
                
                if (wavesCompletedInLoop < maxWaves)
                {
                    Debug.Log($"Waiting {waveLoopDelay} seconds before next wave...");
                    yield return new WaitForSeconds(waveLoopDelay);
                }
            }
            
            Debug.Log($"Single wave cycle completed! Total waves: {wavesCompletedInLoop}/{maxWaves}");
            OnWaveLoopComplete?.Invoke();
            
            StartCoroutine(WaveCompletionSequence());
            waveLoopCoroutine = null;
        }

        /// <summary>
        /// Wave completion sequence: fade out, clear enemies, fade in
        /// </summary>
        private IEnumerator WaveCompletionSequence()
        {
            Debug.Log("Starting wave completion sequence...");
            
            // Fade out
            if (PlayerUIManager.Instance?.transitionMenu != null)
            {
                yield return PlayerUIManager.Instance.transitionMenu.FadeIn();
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
            
            // Clear enemies
            ClearAllEnemiesWithFade();
            yield return new WaitForSeconds(2f);
            
            // Fade back in
            if (PlayerUIManager.Instance?.transitionMenu != null)
            {
                yield return PlayerUIManager.Instance.transitionMenu.FadeOut();
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
            
            Debug.Log("Wave completion sequence finished!");
            OnWaveCycleComplete?.Invoke();
        }

        /// <summary>
        /// Force end all waves when no targets are available
        /// </summary>
        public void ForceEndWavesNoTargets()
        {
            Debug.Log("No buildings or NPCs available - forcing wave end!");
            
            if (waveLoopCoroutine != null)
            {
                StopCoroutine(waveLoopCoroutine);
                waveLoopCoroutine = null;
            }
            
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            StartCoroutine(WaveCompletionSequence());
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

        #endregion

        #region Wave State Management

        protected override void EnemySetupStateChanged(EnemySetupState newState)
        {
            Debug.Log($"<color=green>Camp EnemySetupStateChanged: {newState}</color>");

            switch (newState)
            {
                case EnemySetupState.WAVE_START:
                    EnemySpawnManager.Instance.ResetWaveCount();
                    StartCoroutine(TransitionToNextState(EnemySetupState.PRE_ENEMY_SPAWNING, 0.5f));
                    break;
                case EnemySetupState.PRE_ENEMY_SPAWNING:
                    SetupCampForWave();
                    StartCoroutine(TransitionToNextState(EnemySetupState.ENEMY_SPAWN_START, 1.0f));
                    break;
                case EnemySetupState.ENEMY_SPAWN_START:
                    if (!AreTargetsAvailable())
                    {
                        Debug.Log("No buildings or NPCs available - skipping enemy spawning!");
                        SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                        return;
                    }
                    StartCampEnemyWave();
                    StartCoroutine(TransitionToNextState(EnemySetupState.ENEMIES_SPAWNED, 2.0f));
                    break;
                case EnemySetupState.ENEMIES_SPAWNED:
                    if (!AreTargetsAvailable())
                    {
                        Debug.Log("No buildings or NPCs available - ending wave immediately!");
                        SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                        return;
                    }
                    StartWaveTiming();
                    break;
                case EnemySetupState.ALL_WAVES_CLEARED:
                    EndCampWave();
                    break;
            }
        }

        private IEnumerator TransitionToNextState(EnemySetupState nextState, float delay)
        {
            yield return new WaitForSeconds(delay);
            SetEnemySetupState(nextState);
        }

        private void StartWaveTiming()
        {
            waveStartTime = Time.time;
            currentWaveNumber++;
            
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            if (waveConfig != null && waveConfig is CampEnemyWaveConfig campConfig)
            {
                currentWaveDuration = campConfig.WaveDuration;
            }
            
            Debug.Log($"Wave {currentWaveNumber} started. Duration: {currentWaveDuration}s");
        }

        private void CheckForWaveEnd()
        {
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            
            bool shouldEndWave = false;
            string endReason = "";
            
            if (enemies.Length == 0 && IsWaveActive)
            {
                shouldEndWave = true;
                endReason = "All enemies defeated";
            }
            else if (IsWaveActive && Time.time - waveStartTime >= currentWaveDuration)
            {
                shouldEndWave = true;
                endReason = "Time expired";
            }
            
            if (shouldEndWave)
            {
                Debug.Log($"Wave ending: {endReason}");
                SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            }
        }

        #endregion

        #region NPC Management

        /// <summary>
        /// Finds all NPCs in the camp for wave management
        /// </summary>
        private void FindCampNPCs()
        {
            campNPCs.Clear();
            HumanCharacterController[] npcs = FindObjectsByType<HumanCharacterController>(FindObjectsSortMode.None);
            
            foreach (var npc in npcs)
            {
                if (npc != PlayerController.Instance._possessedNPC)
                {
                    campNPCs.Add(npc);
                    RegisterTarget(npc);
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
                RegisterTarget(npc);
            }
        }

        /// <summary>
        /// Remove an NPC from the camp wave manager
        /// </summary>
        public void RemoveNPC(HumanCharacterController npc)
        {
            if (campNPCs.Contains(npc))
            {
                campNPCs.Remove(npc);
                UnregisterTarget(npc);
            }
        }

        private void SetupCampForWave()
        {
            if (PlayerController.Instance._possessedNPC != null)
            {
                PlayerController.Instance.PossessNPC(null);
            }
            
            MakeNPCsFlee();
        }

        private void StartCampEnemyWave()
        {
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            if (waveConfig != null)
            {
                Debug.Log($"Starting camp wave with config: {waveConfig.name}, Difficulty: {GetCurrentWaveDifficulty()}");
                EnemySpawnManager.Instance.StartSpawningEnemies(waveConfig);
            }
            else
            {
                Debug.LogWarning($"No wave config found for camp wave difficulty {GetCurrentWaveDifficulty()}!");
                Debug.Log("Continuing wave without enemies for testing purposes");
            }
        }

        private void EndCampWave()
        {
            Debug.Log("Camp wave ended!");
            
            ReturnNPCsToNormal();
            OnCampWaveEnded?.Invoke();
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_CAMERA_MOVEMENT);
        }

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

        #endregion

        #region Shared Grid System

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
                        continue;
                    }

                    sharedGridSlots[slot].IsOccupied = true;
                    sharedGridSlots[slot].OccupyingObject = placedObject;
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
                    UpdateGridSlotVisual(slot, false);
                }
            }
        }

        private void UpdateGridSlotVisual(Vector3 slotPosition, bool isOccupied)
        {
            if (!sharedGridSlots.ContainsKey(slotPosition)) return;
            
            GridSlot slot = sharedGridSlots[slotPosition];
            Vector3 displayPosition = new Vector3(slotPosition.x + sharedGridSize / 2, 0, slotPosition.z + sharedGridSize / 2);
            UpdateGridSlotVisualObject(slot, displayPosition, isOccupied);
        }

        private void UpdateGridSlotVisualObject(GridSlot slot, Vector3 displayPosition, bool isOccupied)
        {
            GameObject freePrefab = PlacementManager.Instance?.gridPrefab;
            GameObject takenPrefab = PlacementManager.Instance?.takenGridPrefab;
            
            if (freePrefab == null || takenPrefab == null) return;

            if (slot.FreeGridObject == null)
            {
                slot.FreeGridObject = Instantiate(freePrefab, displayPosition, Quaternion.identity, sharedGridParent);
            }
            
            if (slot.TakenGridObject == null)
            {
                slot.TakenGridObject = Instantiate(takenPrefab, displayPosition, Quaternion.identity, sharedGridParent);
            }
            
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

            if (sharedGridSlots == null) return;

            foreach (var kvp in sharedGridSlots)
            {
                Vector3 gridPosition = kvp.Key;
                GridSlot slot = kvp.Value;
                
                Vector3 displayPosition = new Vector3(gridPosition.x + sharedGridSize / 2, 0, gridPosition.z + sharedGridSize / 2);
                UpdateGridSlotVisualObject(slot, displayPosition, slot.IsOccupied);
            }
            
            gridObjectsInitialized = true;
        }

        public void ShowGridObjects()
        {
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

        #endregion

        #region Debug Methods

        /// <summary>
        /// Get the current wave difficulty level
        /// </summary>
        public override int GetCurrentWaveDifficulty()
        {
            return 1; // Base difficulty for now
        }

        /// <summary>
        /// Spawn a single enemy for debugging purposes
        /// </summary>
        public void SpawnSingleEnemy()
        {
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            if (waveConfig == null || waveConfig.enemyPrefabs == null || waveConfig.enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("No enemy prefabs found in wave config!");
                return;
            }

            Vector3 spawnPosition = GetRandomSpawnPosition();
            GameObject enemyPrefab = waveConfig.enemyPrefabs[UnityEngine.Random.Range(0, waveConfig.enemyPrefabs.Length)];
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            
            Debug.Log($"Spawned single enemy at {spawnPosition}");
        }

        /// <summary>
        /// Clear all enemies from the scene immediately
        /// </summary>
        public void ClearAllEnemies()
        {
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
        /// Clear all enemies with a fade-out effect
        /// </summary>
        public void ClearAllEnemiesWithFade()
        {
            StartCoroutine(ClearEnemiesWithFadeCoroutine());
        }
        
        private IEnumerator ClearEnemiesWithFadeCoroutine()
        {
            EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            
            if (enemies.Length == 0)
            {
                yield break;
            }
            
            Debug.Log($"Clearing {enemies.Length} enemies with fade effect");
            
            float fadeDuration = 1f;
            float elapsedTime = 0f;
            
            Dictionary<EnemyBase, Material> originalMaterials = new Dictionary<EnemyBase, Material>();
            Dictionary<EnemyBase, SkinnedMeshRenderer> renderers = new Dictionary<EnemyBase, SkinnedMeshRenderer>();
            
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    var renderer = enemy.GetComponent<SkinnedMeshRenderer>();
                    if (renderer != null)
                    {
                        renderers[enemy] = renderer;
                        originalMaterials[enemy] = renderer.material;
                        
                        Material fadeMaterial = new Material(renderer.material);
                        fadeMaterial.SetFloat("_Mode", 3);
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
            
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            
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
            
            Vector3 spawnPos = new Vector3(x, 100f, z);
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