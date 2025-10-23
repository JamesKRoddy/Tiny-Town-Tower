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
        private CampAttackState campAttackState = CampAttackState.PEACEFUL;

        // Cached target lists for efficient checking
        private List<IDamageable> cachedTargets = new List<IDamageable>();

        // References to other managers
        private ResearchManager researchManager;
        private CleanlinessManager cleanlinessManager;
        private WorkManager workManager;
        private PlacementManager placementManager;
        private BuildManager buildManager;
        private CookingManager cookingManager;
        private ResourceUpgradeManager resourceUpgradeManager;
        private ElectricityManager electricityManager;
        private FarmingManager farmingManager;
        private MedicalManager medicalManager;

        #endregion

        #region Public Properties

        // Manager references
        public ResearchManager ResearchManager => researchManager;
        public CleanlinessManager CleanlinessManager => cleanlinessManager;
        public WorkManager WorkManager => workManager;
        public PlacementManager PlacementManager => placementManager;
        public BuildManager BuildManager => buildManager;
        public CookingManager CookingManager => cookingManager;
        public ResourceUpgradeManager ResourceUpgradeManager => resourceUpgradeManager;
        public ElectricityManager ElectricityManager => electricityManager;
        public FarmingManager FarmingManager => farmingManager;
        public MedicalManager MedicalManager => medicalManager;

        // Shared placement settings
        public Vector2 SharedXBounds => sharedXBounds;
        public Vector2 SharedZBounds => sharedZBounds;
        public float SharedGridSize => sharedGridSize;
        public bool ShowSharedGridBounds => showSharedGridBounds;

        // Shared grid
        public Dictionary<Vector3, GridSlot> SharedGridSlots => sharedGridSlots;

        // Wave state
        public bool IsWaveActive => GetEnemySetupState() != EnemySetupState.ALL_WAVES_CLEARED;
        public CampAttackState CampAttackState => campAttackState;
        public bool IsCampUnderAttack => campAttackState == CampAttackState.UNDER_ATTACK;
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
            
            // NPCs register themselves when they spawn/load - no need to search here
            // FindCampNPCs() removed - it was running before NPCs loaded from save
            PopulateTargetCache();
            
            // Subscribe to time events to end attacks at morning
            if (GameManager.Instance?.TimeManager != null)
            {
                TimeManager.OnDayStarted += OnDayStarted;
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            // Unsubscribe from time events
            TimeManager.OnDayStarted -= OnDayStarted;
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
            placementManager = GetComponentInChildren<PlacementManager>();
            buildManager = GetComponentInChildren<BuildManager>();
            electricityManager = GetComponentInChildren<ElectricityManager>();
            farmingManager = GetComponentInChildren<FarmingManager>();
            medicalManager = GetComponentInChildren<MedicalManager>();
        }

        private void LogMissingManagers()
        {
            if (researchManager == null) Debug.LogWarning("ResearchManager not found in scene!");
            if (cleanlinessManager == null) Debug.LogWarning("CleanlinessManager not found in scene!");
            if (cookingManager == null) Debug.LogWarning("CookingManager not found in scene!");
            if (resourceUpgradeManager == null) Debug.LogWarning("ResourceUpgradeManager not found in scene!");
            if (workManager == null) Debug.LogWarning("WorkManager not found in scene!");
            if (placementManager == null) Debug.LogWarning("PlacementManager not found in scene!");
            if (buildManager == null) Debug.LogWarning("BuildManager not found in scene!");
            if (electricityManager == null) Debug.LogWarning("ElectricityManager not found in scene!");
            if (farmingManager == null) Debug.LogWarning("FarmingManager not found in scene!");
            if (medicalManager == null) Debug.LogWarning("MedicalManager not found in scene!");
        }

        private void InitializeAllManagers()
        {
            researchManager?.Initialize();
            cookingManager?.Initialize();
            resourceUpgradeManager?.Initialize();
            electricityManager?.Initialize();
            cleanlinessManager?.Initialize();
            farmingManager?.Initialize();
            medicalManager?.Initialize();
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
                
                if (target is IPlaceableStructure || 
                    (target is HumanCharacterController npc && npc != PlayerController.Instance._possessedNPC))
                {
                    return true;
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

            // Find all placeable structures (buildings and turrets) - use the existing registry system
            Building[] buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var building in buildings)
            {
                if (building is IDamageable damageable)
                {
                    cachedTargets.Add(damageable);
                }
            }

            BaseTurret[] turrets = FindObjectsByType<BaseTurret>(FindObjectsSortMode.None);
            foreach (var turret in turrets)
            {
                if (turret is IDamageable damageable)
                {
                    cachedTargets.Add(damageable);
                }
            }

            // Find all NPCs
            HumanCharacterController[] npcs = FindObjectsByType<HumanCharacterController>(FindObjectsSortMode.None);
            cachedTargets.AddRange(npcs);
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
                ForceEndWavesNoTargets();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get a random valid target from the cached targets
        /// </summary>
        /// <returns>A random target transform, or null if no valid targets</returns>
        public Transform GetRandomTarget()
        {
            List<Transform> validTargets = new List<Transform>();
            
            foreach (var target in cachedTargets)
            {
                if (target == null || target.Health <= 0) continue;
                
                if (target is IPlaceableStructure || 
                    (target is HumanCharacterController npc && npc != PlayerController.Instance._possessedNPC))
                {
                    if (target is MonoBehaviour mb)
                    {
                        validTargets.Add(mb.transform);
                    }
                }
            }
            
            if (validTargets.Count == 0)
                return null;
                
            return validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
        }

        /// <summary>
        /// Get categorized targets for more complex targeting logic
        /// </summary>
        /// <param name="npcTargets">List to populate with NPC targets</param>
        /// <param name="buildingTargets">List to populate with building targets</param>
        public void GetCategorizedTargets(List<Transform> npcTargets, List<Transform> buildingTargets)
        {
            npcTargets.Clear();
            buildingTargets.Clear();
            
            foreach (var target in cachedTargets)
            {
                if (target == null || target.Health <= 0) continue;
                
                // Check if the target is still active in the scene (this will filter out NPCs in bunkers)
                if (target is MonoBehaviour mb && !mb.gameObject.activeInHierarchy) continue;
                
                if (target is HumanCharacterController npc && npc != PlayerController.Instance._possessedNPC)
                {
                    if (target is MonoBehaviour mbNpc)
                    {
                        npcTargets.Add(mbNpc.transform);
                    }
                }
                else if (target is IPlaceableStructure)
                {
                    if (target is MonoBehaviour mbBuilding)
                    {
                        buildingTargets.Add(mbBuilding.transform);
                    }
                }
            }
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
                return;
            }

            if (waveLoopCoroutine != null)
            {
                StopCoroutine(waveLoopCoroutine);
                waveLoopCoroutine = null;
            }

            // Set camp under attack - NPCs will flee and stay vigilant for entire cycle
            campAttackState = CampAttackState.UNDER_ATTACK;
            Debug.Log("[CampManager] Camp is now UNDER_ATTACK - NPCs will flee until morning or all waves cleared");

            waveLoopCoroutine = StartCoroutine(SingleWaveCycle());
        }

        /// <summary>
        /// Starts a single wave
        /// </summary>
        private void StartSingleWave()
        {
            if (!AreTargetsAvailable())
            {
                ForceEndWavesNoTargets();
                return;
            }
            
            OnCampWaveStarted?.Invoke();
            SetEnemySetupState(EnemySetupState.WAVE_START);
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_ATTACK_CAMERA_MOVEMENT);
        }

        /// <summary>
        /// Single wave cycle coroutine that runs one complete cycle of waves
        /// </summary>
        private IEnumerator SingleWaveCycle()
        {
            wavesCompletedInLoop = 0;
            
            var waveConfig = GetWaveConfig(GetCurrentWaveDifficulty());
            int maxWaves = waveConfig?.maxWaves ?? maxWavesPerLoop;
            
            while (wavesCompletedInLoop < maxWaves)
            {
                if (!AreTargetsAvailable())
                {
                    break;
                }
                
                StartSingleWave();
                
                while (IsWaveActive)
                {
                    yield return null;
                }
                
                wavesCompletedInLoop++;
                
                if (wavesCompletedInLoop < maxWaves)
                {
                    yield return new WaitForSeconds(waveLoopDelay);
                }
            }
            
            OnWaveLoopComplete?.Invoke();
            
            // All waves in the cycle complete - camp is no longer under attack
            SetCampAttackState(CampAttackState.PEACEFUL);
            
            StartCoroutine(WaveCompletionSequence());
            waveLoopCoroutine = null;
        }

        /// <summary>
        /// Wave completion sequence: fade out, clear enemies, fade in
        /// </summary>
        private IEnumerator WaveCompletionSequence()
        {
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
            
            OnWaveCycleComplete?.Invoke();
        }

        /// <summary>
        /// Force end all waves when no targets are available
        /// </summary>
        public void ForceEndWavesNoTargets()
        {
            if (waveLoopCoroutine != null)
            {
                StopCoroutine(waveLoopCoroutine);
                waveLoopCoroutine = null;
            }
            
            // Attack is over - no targets left
            SetCampAttackState(CampAttackState.PEACEFUL);
            
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            StartCoroutine(WaveCompletionSequence());
        }

        /// <summary>
        /// Manually end the camp wave (for debugging)
        /// </summary>
        public void ForceEndCampWave()
        {
            SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_CAMERA_MOVEMENT);
        }

        #endregion

        #region Wave State Management

        protected override void EnemySetupStateChanged(EnemySetupState newState)
        {
            Debug.Log($"[CampManager] EnemySetupStateChanged: {newState}");
            
            switch (newState)
            {
                case EnemySetupState.WAVE_START:
                    Debug.Log("[CampManager] WAVE_START - resetting wave count");
                    EnemySpawnManager.Instance.ResetWaveCount();
                    StartCoroutine(TransitionToNextState(EnemySetupState.PRE_ENEMY_SPAWNING, 0.5f));
                    break;
                case EnemySetupState.PRE_ENEMY_SPAWNING:
                    Debug.Log("[CampManager] PRE_ENEMY_SPAWNING - calling SetupCampForWave");
                    SetupCampForWave();
                    StartCoroutine(TransitionToNextState(EnemySetupState.ENEMY_SPAWN_START, 1.0f));
                    break;
                case EnemySetupState.ENEMY_SPAWN_START:
                    if (!AreTargetsAvailable())
                    {
                        SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
                        return;
                    }
                    StartCampEnemyWave();
                    StartCoroutine(TransitionToNextState(EnemySetupState.ENEMIES_SPAWNED, 2.0f));
                    break;
                case EnemySetupState.ENEMIES_SPAWNED:
                    if (!AreTargetsAvailable())
                    {
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
        }

        private void CheckForWaveEnd()
        {
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            
            if (enemies.Length == 0 && IsWaveActive)
            {
                SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            }
            else if (IsWaveActive && Time.time - waveStartTime >= currentWaveDuration)
            {
                SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
            }
        }

        #endregion

        #region NPC Management

        /// <summary>
        /// Finds all NPCs in the camp for wave management
        /// Only used as a fallback - NPCs should register themselves via AddNPC()
        /// </summary>
        private void FindCampNPCs()
        {
            // Don't clear if NPCs have already registered themselves (e.g., from save load)
            // Only clear and search if the list is empty
            if (campNPCs.Count > 0)
            {
                Debug.Log($"[CampManager] FindCampNPCs skipped - {campNPCs.Count} NPCs already registered");
                return;
            }
            
            Debug.Log("[CampManager] FindCampNPCs searching for NPCs in scene");
            HumanCharacterController[] npcs = FindObjectsByType<HumanCharacterController>(FindObjectsSortMode.None);
            
            foreach (var npc in npcs)
            {
                if (npc != PlayerController.Instance._possessedNPC)
                {
                    campNPCs.Add(npc);
                    RegisterTarget(npc);
                }
            }
            
            Debug.Log($"[CampManager] FindCampNPCs found {campNPCs.Count} NPCs");
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
                Debug.Log($"[CampManager] Added NPC {npc.name} to wave manager. Total NPCs: {campNPCs.Count}");
            }
        }

        public void RemoveNPC(HumanCharacterController npc)
        {
            if (campNPCs.Contains(npc))
            {
                campNPCs.Remove(npc);
                UnregisterTarget(npc);
                Debug.Log($"[CampManager] Removed NPC {npc.name} from wave manager. Total NPCs: {campNPCs.Count}");
            }
        }

        private void SetupCampForWave()
        {
            Debug.Log("[CampManager] SetupCampForWave called");
            
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
                EnemySpawnManager.Instance.StartSpawningEnemies(waveConfig);
            }
            else
            {
                Debug.LogWarning($"No wave config found for camp wave difficulty {GetCurrentWaveDifficulty()}!");
            }
        }

        private void EndCampWave()
        {
            ReturnNPCsToNormal();
            OnCampWaveEnded?.Invoke();
            PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.CAMP_CAMERA_MOVEMENT);
        }

        private void MakeNPCsFlee()
        {
            Debug.Log($"[CampManager] MakeNPCsFlee called - NPCs count: {campNPCs.Count}");
            
            foreach (var npc in campNPCs)
            {
                if (npc is SettlerNPC settler)
                {
                    Debug.Log($"[CampManager] Telling {settler.name} to FLEE");
                    settler.ChangeTask(TaskType.FLEE);
                }
                else
                {
                    Debug.Log($"[CampManager] NPC {npc?.name} is not a SettlerNPC");
                }
            }
        }

        private void ReturnNPCsToNormal()
        {
            // Only return NPCs to normal if camp is no longer under attack
            if (campAttackState == CampAttackState.UNDER_ATTACK)
            {
                Debug.Log("[CampManager] Not returning NPCs to normal - camp is still UNDER_ATTACK (more waves coming)");
                return;
            }
            
            Debug.Log("[CampManager] Returning NPCs to normal - camp is PEACEFUL");
            foreach (var npc in campNPCs)
            {
                if (npc is SettlerNPC settler)
                {
                    // Check for available work before going to wander
                    if (WorkManager != null)
                    {
                        bool taskAssigned = WorkManager.AssignNextAvailableTask(settler);
                        if (!taskAssigned)
                        {
                            // No tasks available, go to wander state
                            settler.ChangeTask(TaskType.WANDER);
                        }
                    }
                    else
                    {
                        settler.ChangeTask(TaskType.WANDER);
                    }
                }
            }
        }
        
        /// <summary>
        /// Set the camp attack state and notify NPCs
        /// </summary>
        private void SetCampAttackState(CampAttackState newState)
        {
            if (campAttackState == newState) return;
            
            campAttackState = newState;
            Debug.Log($"[CampManager] CampAttackState changed to: {newState}");
            
            // When attack ends, return NPCs to normal
            if (newState == CampAttackState.PEACEFUL)
            {
                ReturnNPCsToNormal();
            }
        }
        
        /// <summary>
        /// Called when day starts - ends any active attack
        /// </summary>
        private void OnDayStarted()
        {
            Debug.Log("[CampManager] Day started - ending any active attacks");
            
            if (campAttackState == CampAttackState.UNDER_ATTACK)
            {
                // Morning has arrived - force end the attack
                SetCampAttackState(CampAttackState.PEACEFUL);
                
                // Stop wave loop if active
                if (waveLoopCoroutine != null)
                {
                    StopCoroutine(waveLoopCoroutine);
                    waveLoopCoroutine = null;
                }
                
                // Clear any remaining enemies
                ClearAllEnemiesWithFade();
                
                // Reset wave state
                SetEnemySetupState(EnemySetupState.ALL_WAVES_CLEARED);
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
            GameObject freePrefab = placementManager?.gridPrefab;
            GameObject takenPrefab = placementManager?.takenGridPrefab;
            
            if (freePrefab == null || takenPrefab == null) return;

            if (slot.FreeGridObject == null)
            {
                slot.FreeGridObject = Instantiate(freePrefab, displayPosition, Quaternion.identity, sharedGridParent);
            }
            
            if (slot.TakenGridObject == null)
            {
                slot.TakenGridObject = Instantiate(takenPrefab, displayPosition, Quaternion.identity, sharedGridParent);
            }
            
            // Only show grid objects if they are initialized AND the grid should be visible
            bool shouldShowGrid = gridObjectsInitialized && IsGridVisible();
            
            slot.FreeGridObject?.SetActive(!isOccupied && shouldShowGrid);
            slot.TakenGridObject?.SetActive(isOccupied && shouldShowGrid);
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

        public Vector3 SnapToSharedGrid(Vector3 position)
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
                slot.FreeGridObject?.SetActive(!slot.IsOccupied);
                slot.TakenGridObject?.SetActive(slot.IsOccupied);
            }
        }

        public void HideGridObjects()
        {
            if (sharedGridSlots == null) return;

            foreach (var slot in sharedGridSlots.Values)
            {
                slot.FreeGridObject?.SetActive(false);
                slot.TakenGridObject?.SetActive(false);
            }
        }

        /// <summary>
        /// Check if the grid should currently be visible (i.e., in placement mode)
        /// </summary>
        private bool IsGridVisible()
        {
            // Check if we're in building placement mode by checking the current control type
            return PlayerInput.Instance != null && 
                   PlayerInput.Instance.CurrentControlType == PlayerControlType.BUILDING_PLACEMENT;
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
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
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
            
            float fadeDuration = 1f;
            float elapsedTime = 0f;
            
            Dictionary<EnemyBase, Material> originalMaterials = new Dictionary<EnemyBase, Material>();
            Dictionary<EnemyBase, SkinnedMeshRenderer> renderers = new Dictionary<EnemyBase, SkinnedMeshRenderer>();
            
            foreach (var enemy in enemies)
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
                Destroy(enemy.gameObject);
            }
            
            foreach (var material in originalMaterials.Values)
            {
                DestroyImmediate(material);
            }
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
        
        #region Medical Building Management
        
        /// <summary>
        /// Register a medical building with the medical system
        /// </summary>
        /// <param name="medicalBuilding">The medical building to register</param>
        public void RegisterMedicalBuilding(MedicalBuilding medicalBuilding)
        {
            if (medicalManager != null)
            {
                medicalManager.RegisterMedicalBuilding(medicalBuilding);
            }
            else
            {
                Debug.LogWarning($"[CampManager] Cannot register medical building {medicalBuilding.name} - MedicalManager is null");
            }
        }
        
        /// <summary>
        /// Unregister a medical building from the medical system
        /// </summary>
        /// <param name="medicalBuilding">The medical building to unregister</param>
        public void UnregisterMedicalBuilding(MedicalBuilding medicalBuilding)
        {
            if (medicalManager != null)
            {
                medicalManager.UnregisterMedicalBuilding(medicalBuilding);
            }
            else
            {
                Debug.LogWarning($"[CampManager] Cannot unregister medical building {medicalBuilding.name} - MedicalManager is null");
            }
        }
        
        #endregion
    }
}