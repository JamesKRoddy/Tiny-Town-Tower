using System.Collections.Generic;
using System;
using Characters.NPC;
using Characters.NPC.Characteristic;
using Managers;
using UnityEngine;
using UnityEngine.AI;
using Mono.Cecil.Cil;


[RequireComponent(typeof(NavMeshAgent))]
public class SettlerNPC : HumanCharacterController, INarrativeTarget
{
    [Header("Procedural Settler Data")]
    [SerializeField, ReadOnly] private string settlerName;
    [SerializeField, ReadOnly] private int settlerAge;
    [SerializeField, ReadOnly] private string settlerDescription;
    public string SettlerName => settlerName;
    public int SettlerAge => settlerAge;
    public string SettlerDescription => settlerDescription;

    [Header("NPC Systems")]
    [SerializeField, ReadOnly] internal NPCCharacteristicSystem characteristicSystem;
    [SerializeField] internal NPCAppearanceSystem appearanceSystem;
    
    [Header("Task Management")]
    private _TaskState currentState;
    private WorkTask assignedWorkTask; // Track the assigned work task
    public bool HasAssignedWorkTask => assignedWorkTask != null;
    private bool isOnBreak = false; // Track if NPC is on break
    // Removed workTaskBeforeSleep - using assignedWorkTask is sufficient

    [Header("Initialization Control")]
    [SerializeField] private NPCInitializationContext initializationContext = NPCInitializationContext.FRESH_SPAWN; //Set this to the context of the NPC when it is spawned, override to loaded for NPCs in scene already
    [SerializeField, ReadOnly] private bool hasBeenInitialized = false;
    
    // Store recruited appearance data for recruited NPCs
    private NPCAppearanceData recruitedAppearanceData;

    [Header("NPC Stats")]
    public int additionalMutationSlots = 3; //Additional mutation slots

    [Header("Stamina")]
    public float maxStamina = 100f;
    [ReadOnly] public float currentStamina = 100f;
    [SerializeField] private float baseStaminaRegenRate = 5f; // Base regen rate before characteristics
    [ReadOnly] public float staminaRegenRate = 5f; // Base regeneration rate (can be modified by characteristics)
    [SerializeField] private float baseFatigueRate = 1f; // Base fatigue rate before characteristics
    [ReadOnly] public float fatigueRate = 1f; // Base fatigue rate (can be modified by characteristics)
    public float sleepStaminaRegenMultiplier = 3f; // Multiplier for stamina regen while sleeping
    [SerializeField] private float staminaDrainRate = 1f; // How fast stamina drains during activity (calculated from day length)
    [SerializeField] private float nightFatigueMultiplier = 1.5f; // Extra fatigue at night

    [Header("Hunger System")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger = 100f;
    [SerializeField] private float hungerDecreaseRate = 1f; // Hunger points per second
    [SerializeField] private float hungerThreshold = 30f; // When to start looking for food
    [SerializeField] private float starvationThreshold = 10f; // When to stop working
    [SerializeField] private float workSpeedMultiplier = 1f; // Current work speed multiplier based on hunger
    [SerializeField] private float noFoodCooldown = 10f; // Cooldown period when no food is available
    private float lastNoFoodTime = -10f; // Initialize to allow immediate first check

    public event Action<float, float> OnHungerChanged; // Current hunger, max hunger
    public event Action OnStarving;
    public event Action OnNoLongerStarving;

    [Header("Sickness System")]
    [SerializeField] private bool isSick = false;
    [SerializeField] private float baseSicknessChance = 0.001f; // Base chance per second (very low)
    [SerializeField] private float sicknessCheckInterval = 5f; // Check every 5 seconds
    [SerializeField] private float sicknessDuration = 30f; // How long being sick lasts (seconds)
    [SerializeField] private float sickWorkSpeedPenalty = 0.5f; // 50% work speed when sick
    [SerializeField] private float lowStaminaThreshold = 30f; // Stamina level below which NPCs are considered tired
    [SerializeField] private float veryLowStaminaThreshold = 10f; // Stamina level for severe tiredness
    private float lastSicknessCheck = 0f;
    private float sicknessStartTime = 0f;

    public event Action OnBecameSick;
    public event Action OnRecoveredFromSickness;
    public bool IsSick => isSick;

    private int workLayerIndex;

    // Dictionary that maps TaskType to TaskState
    Dictionary<TaskType, _TaskState> taskStates = new Dictionary<TaskType, _TaskState>();

    protected override void Awake()
    {
        base.Awake();

        // Initialize characteristic system
        characteristicSystem = new NPCCharacteristicSystem(this);

        // Initialize appearance system
        if (appearanceSystem != null)
        {
        appearanceSystem.SetSettlerNPC(this);
        }
        else
        {
            Debug.LogError($"[SettlerNPC] {gameObject.name} - appearanceSystem is null! Make sure it's assigned in the prefab inspector.");
        }

        // Get all TaskState components attached to the SettlerNPC GameObject
        _TaskState[] states = GetComponents<_TaskState>();

        // Populate the dictionary with TaskType -> TaskState mappings
        foreach (var state in states)
        {
            taskStates.Add(state.GetTaskType(), state);
            state.SetNPCReference(this);
        }
    }

    protected override void Start()
    {
        base.Start();
        workLayerIndex = animator.GetLayerIndex("Work Layer");
        if (workLayerIndex == -1)
        {
            Debug.LogError($"[WorkState] Could not find 'Work Layer' in animator for {gameObject.name}");
        }

        // Initialize based on context if not already initialized
        if (!hasBeenInitialized)
        {
            InitializeForContext(initializationContext);
        }
        
        // Initialize stamina rates (before characteristics are applied)
        InitializeStaminaRates();
        
        // Subscribe to time events for automatic sleep/wake behavior
        if (GameManager.Instance?.TimeManager != null)
        {
            TimeManager.OnNightStarted += OnNightStarted;
            // Removed OnDayStarted - wake-up is handled by SleepState
            
            // Calculate stamina rates based on day/night length
            CalculateStaminaRates();
        }
    }
    
    /// <summary>
    /// Handle night started event - transition to sleep if appropriate
    /// </summary>
    private void OnNightStarted()
    {
        // Only handle sleep if night sleep behavior is enabled and it's night time
        if (GameManager.Instance?.TimeManager == null || 
            !GameManager.Instance.TimeManager.EnableNightSleepBehavior ||
            !GameManager.Instance.TimeManager.IsNight)
        {
            return;
        }
        
        // Check if NPC should sleep based on sleep chance
        if (UnityEngine.Random.value < GameManager.Instance.TimeManager.SleepChance)
        {
            // Only transition if not already sleeping and not in critical states
            var currentTask = GetCurrentTaskType();
            if (currentTask != TaskType.SLEEP && 
                currentTask != TaskType.FLEE && 
                currentTask != TaskType.ATTACK)
            {
                // No need to store work task - assignedWorkTask persists through sleep
                
                Debug.Log($"[SettlerNPC] {name} transitioning to sleep for the night (current task: {currentTask})");
                ChangeTask(TaskType.SLEEP);
            }
        }
        else
        {
            Debug.Log($"[SettlerNPC] {name} will continue working through the night");
        }
    }
    
    // Removed OnDayStarted method - wake-up logic is handled by SleepState
    
    /// <summary>
    /// Initialize stamina rates from base values (before characteristics modify them)
    /// </summary>
    private void InitializeStaminaRates()
    {
        staminaRegenRate = baseStaminaRegenRate;
        fatigueRate = baseFatigueRate;
    }
    
    /// <summary>
    /// Calculate stamina rates based on TimeManager day/night cycle
    /// Goal: NPCs should reach 0% stamina after 36 hours of continuous wandering
    /// Sleep should fully restore stamina during night periods
    /// </summary>
    private void CalculateStaminaRates()
    {
        var timeManager = GameManager.Instance?.TimeManager;
        if (timeManager == null) 
        {
            // Fallback if no TimeManager
            staminaDrainRate = 1f;
            sleepStaminaRegenMultiplier = 3f;
            Debug.LogWarning($"[SettlerNPC] {name} - No TimeManager found! Using fallback rates:");
            Debug.LogWarning($"  staminaDrainRate: {staminaDrainRate}/s");
            Debug.LogWarning($"  sleepStaminaRegenMultiplier: {sleepStaminaRegenMultiplier}x");
            return;
        }
        
        // Get day and night durations from TimeManager (now simplified to 2:1 ratio)
        float dayDurationInSeconds = timeManager.DayDurationInSeconds;
        float nightDurationInSeconds = timeManager.NightDurationInSeconds;
        float totalCycleDuration = dayDurationInSeconds + nightDurationInSeconds;
        
        // NEW REQUIREMENT: NPCs should reach 0% stamina after 36 game hours of wandering
        // 1 full day/night cycle = 24 game hours, so 36 game hours = 1.5 cycles
        float cyclesNeededForExhaustion = 1.5f;
        float targetSecondsToExhaustion = cyclesNeededForExhaustion * totalCycleDuration;
        
        // Calculate base drain rate: 100% stamina lost over the calculated time period
        staminaDrainRate = 100f / targetSecondsToExhaustion;
        
        // Calculate sleep regen multiplier so NPCs can fully recover during night sleep
        float targetStaminaRestorePerNight = 100f;
        float baseSleepRegenRate = staminaRegenRate;
        
        // Calculate required multiplier for night recovery
        float requiredRegenPerSecond = targetStaminaRestorePerNight / nightDurationInSeconds;
        sleepStaminaRegenMultiplier = requiredRegenPerSecond / baseSleepRegenRate;
        
        // Ensure minimum multiplier
        sleepStaminaRegenMultiplier = Mathf.Max(sleepStaminaRegenMultiplier, 2f);
        
        Debug.Log($"[SettlerNPC] {name} STAMINA RATES CALCULATED:");
        Debug.Log($"  TimeManager Values - Day: {dayDurationInSeconds}s, Night: {nightDurationInSeconds}s, Total Cycle: {totalCycleDuration}s");
        Debug.Log($"  Target: 36 game hours = {cyclesNeededForExhaustion} cycles = {targetSecondsToExhaustion}s real time");
        Debug.Log($"  Calculated staminaDrainRate: {staminaDrainRate:F6}/s (100 stamina / {targetSecondsToExhaustion}s)");
        Debug.Log($"  Current fatigueRate: {fatigueRate}");
        Debug.Log($"  Current nightFatigueMultiplier: {nightFatigueMultiplier}");
        Debug.Log($"  Sleep Regen Multiplier: {sleepStaminaRegenMultiplier:F2}x");
    }
    
    /// <summary>
    /// Recalculate stamina rates - call this when characteristics modify stamina properties
    /// </summary>
    public void RecalculateStaminaRates()
    {
        CalculateStaminaRates();
    }

    /// <summary>
    /// Register this NPC with the relevant managers
    /// </summary>
    private void RegisterWithManagers()
    {
        // Register with NPCManager
        NPCManager.Instance.RegisterNPC(this);
        
        // Register with CampManager for wave management
        if (CampManager.Instance != null)
        {
            CampManager.Instance.AddNPC(this);
        }
    }

    /// <summary>
    /// Initialize the NPC based on the specified context
    /// </summary>
    private void InitializeForContext(NPCInitializationContext context)
    {
        switch (context)
        {
            case NPCInitializationContext.FRESH_SPAWN:
                InitializeAsFreshSpawn();
                break;
            case NPCInitializationContext.RECRUITED:
                RegisterWithManagers();
                InitializeAsRecruited();
                break;
            case NPCInitializationContext.LOADED_FROM_SAVE:
                RegisterWithManagers(); 
                break;
        }
        
        hasBeenInitialized = true;
    }

    /// <summary>
    /// Initialize as a fresh spawn (roguelike rooms, etc.)
    /// </summary>
    private void InitializeAsFreshSpawn()
    {
        // Apply random appearance first
        if (appearanceSystem != null)
        {
            appearanceSystem.RandomizeAppearance();
        }

        // Default to WanderState
        if (taskStates.ContainsKey(TaskType.WANDER))
        {
            ChangeState(taskStates[TaskType.WANDER]);
        }

        // Apply random characteristics
        if (characteristicSystem != null)
        {
            characteristicSystem.ApplyRandomCharacteristic();
        }
    }

    /// <summary>
    /// Initialize as a recruited NPC (from player inventory)
    /// </summary>
    private void InitializeAsRecruited()
    {
        // Restore appearance data if available, otherwise randomize
        if (appearanceSystem != null)
        {
            if (recruitedAppearanceData != null)
            {
                appearanceSystem.SetAppearance(recruitedAppearanceData);
            }
            else
            {
                appearanceSystem.RandomizeAppearance();
            }
        }
        else
        {
            Debug.LogError($"[SettlerNPC] {gameObject.name} - Cannot set appearance for recruited NPC: appearanceSystem is null!");
        }
        
        // Default to WanderState
        if (taskStates.ContainsKey(TaskType.WANDER))
        {
            ChangeState(taskStates[TaskType.WANDER]);
        }

        // Apply random characteristics for recruited settlers
        if (characteristicSystem != null)
        {
            characteristicSystem.ApplyRandomCharacteristic();
        }
    }


    /// <summary>
    /// Set the initialization context before Start() is called
    /// This should be called immediately after instantiation
    /// </summary>
    public void SetInitializationContext(NPCInitializationContext context)
    {
        if (hasBeenInitialized)
        {
            Debug.LogWarning($"[SettlerNPC] {gameObject.name} - Cannot change initialization context after NPC has been initialized");
            return;
        }
        
        initializationContext = context;
    }

    /// <summary>
    /// Restore NPC state from save data
    /// This should be called after SetInitializationContext(LOADED_FROM_SAVE)
    /// </summary>
    public void RestoreFromSaveData(NPCSaveData saveData)
    {
        if (initializationContext != NPCInitializationContext.LOADED_FROM_SAVE)
        {
            Debug.LogWarning($"[SettlerNPC] {gameObject.name} - RestoreFromSaveData should only be called for NPCs loaded from save");
            return;
        }

        // Restore basic stats
        Health = saveData.health;
        MaxHealth = saveData.maxHealth;
        currentStamina = saveData.stamina;
        currentHunger = saveData.hunger;
        additionalMutationSlots = saveData.additionalMutationSlots;

        // Restore position
        transform.position = saveData.position;

        // Restore characteristics
        if (characteristicSystem != null && saveData.equippedCharacteristicIds != null)
        {
            foreach (string characteristicId in saveData.equippedCharacteristicIds)
            {
                var characteristic = NPCManager.Instance.GetCharacteristicById(characteristicId);
                if (characteristic != null)
                {
                    characteristicSystem.EquipCharacteristic(characteristic);
                }
                else
                {
                    Debug.LogWarning($"[SettlerNPC] Could not find characteristic with ID: {characteristicId}");
                }
            }
        }

        // Restore appearance
        if (appearanceSystem != null && saveData.appearanceData != null)
        {
            appearanceSystem.SetAppearance(saveData.appearanceData);
        }
        else if (saveData.appearanceData == null)
        {
            Debug.LogWarning($"[SettlerNPC] No appearance data found for {gameObject.name}, keeping default appearance");
        }

        // Restore task state
        if (!string.IsNullOrEmpty(saveData.currentTaskType) && 
            Enum.TryParse<TaskType>(saveData.currentTaskType, out TaskType taskType) &&
            taskStates.ContainsKey(taskType))
        {
            ChangeState(taskStates[taskType]);
        }
        else
        {
            // Fallback to wander if task type is invalid
            if (taskStates.ContainsKey(TaskType.WANDER))
            {
                ChangeState(taskStates[TaskType.WANDER]);
            }
        }

        hasBeenInitialized = true;
    }

    protected override void OnDestroy()
    {
        // Clean up appearance models
        if (appearanceSystem != null)
        {
            // This ensures appearance models are properly cleaned up
            appearanceSystem.ClearCurrentAppearance();
        }
        
        // Unregister from NPCManager
        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.UnregisterNPC(this);
        }
        
        // Unregister from CampManager
        if (CampManager.Instance != null)
        {
            CampManager.Instance.RemoveNPC(this);
        }
        
        // Unsubscribe from time events
        if (GameManager.Instance?.TimeManager != null)
        {
            TimeManager.OnNightStarted -= OnNightStarted;
            // Removed OnDayStarted unsubscribe - not using that event
        }
        
        // Call base class cleanup
        base.OnDestroy();
    }

    private bool HasAvailableFood()
    {
        var canteens = CampManager.Instance.CookingManager.GetRegisteredCanteens();
        foreach (var canteen in canteens)
        {
            if (canteen.HasAvailableMeals() && canteen.IsOperational())
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Update sickness chance based on hunger, sleep deprivation, and camp cleanliness
    /// </summary>
    private void UpdateSicknessChance()
    {
        // Only check periodically
        if (Time.time - lastSicknessCheck < sicknessCheckInterval)
            return;
        
        lastSicknessCheck = Time.time;
        
        // If already sick, check for recovery
        if (isSick)
        {
            if (Time.time - sicknessStartTime >= sicknessDuration)
            {
                RecoverFromSickness();
            }
            return;
        }
        
        // Calculate sickness chance based on multiple factors
        float sicknessChance = baseSicknessChance;
        
        // Hunger factor - higher chance if hungry or starving
        if (currentHunger <= starvationThreshold)
        {
            sicknessChance *= 8f; // Very high multiplier for starving
        }
        else if (currentHunger <= hungerThreshold)
        {
            sicknessChance *= 3f; // Moderate multiplier for hungry
        }
        
        // Tiredness factor based on stamina
        if (currentStamina <= veryLowStaminaThreshold)
        {
            sicknessChance *= 4f; // Very tired = high sickness chance
        }
        else if (currentStamina <= lowStaminaThreshold)
        {
            sicknessChance *= 2f; // Tired = moderate sickness chance
        }
        
        // Camp cleanliness factor
        if (CampManager.Instance?.CleanlinessManager != null)
        {
            float cleanlinessPercentage = CampManager.Instance.CleanlinessManager.GetCleanlinessPercentage();
            
            if (cleanlinessPercentage < 20f) // Very dirty camp
            {
                sicknessChance *= 5f;
            }
            else if (cleanlinessPercentage < 50f) // Moderately dirty camp
            {
                sicknessChance *= 2f;
            }
            // Clean camps don't increase sickness chance
        }
        
        // Roll for sickness
        float adjustedChance = sicknessChance * sicknessCheckInterval; // Adjust for check interval
        if (UnityEngine.Random.value < adjustedChance)
        {
            BecomeSick();
        }
    }
    
    /// <summary>
    /// Check if the NPC is tired based on current stamina levels
    /// </summary>
    public bool IsTired()
    {
        return currentStamina <= lowStaminaThreshold;
    }
    
    /// <summary>
    /// Check if the NPC is very tired based on current stamina levels
    /// </summary>
    public bool IsVeryTired()
    {
        return currentStamina <= veryLowStaminaThreshold;
    }
    
    /// <summary>
    /// Get stamina percentage for UI display
    /// </summary>
    public float GetStaminaPercentage()
    {
        return (currentStamina / maxStamina) * 100f;
    }
    
    /// <summary>
    /// Make the NPC become sick
    /// </summary>
    private void BecomeSick()
    {
        if (isSick) return;
        
        isSick = true;
        sicknessStartTime = Time.time;
        
        Debug.Log($"[SettlerNPC] {name} has become sick!");
        OnBecameSick?.Invoke();
        
        // Check for available medical treatment
        CheckForMedicalTreatment();
    }
    
    /// <summary>
    /// Make the NPC recover from sickness
    /// </summary>
    private void RecoverFromSickness()
    {
        if (!isSick) return;
        
        isSick = false;
        sicknessStartTime = 0f;
        
        Debug.Log($"[SettlerNPC] {name} has recovered from sickness!");
        OnRecoveredFromSickness?.Invoke();
    }
    
    /// <summary>
    /// Force recovery from sickness (called by medical treatment)
    /// </summary>
    public void ForceRecoveryFromSickness()
    {
        RecoverFromSickness();
    }
    
    /// <summary>
    /// Check if medical treatment is available and transition to it if possible
    /// </summary>
    private void CheckForMedicalTreatment()
    {
        if (!isSick) return;
        
        // Don't interrupt critical states
        if (GetCurrentTaskType() == TaskType.FLEE || 
            GetCurrentTaskType() == TaskType.ATTACK ||
            GetCurrentTaskType() == TaskType.MEDICAL_TREATMENT)
        {
            return;
        }
        
        // Check if there are available medical buildings
        if (CampManager.Instance?.MedicalManager != null && 
            CampManager.Instance.MedicalManager.HasAvailableMedicalBuildings())
        {
            Debug.Log($"[SettlerNPC] {name} is sick and medical treatment is available - seeking treatment");
            ChangeTask(TaskType.MEDICAL_TREATMENT);
        }
        else
        {
            Debug.Log($"[SettlerNPC] {name} is sick but no medical treatment available");
        }
    }
    


    private void Update()
    {
        // Don't do anything if dead
        if (Health <= 0) 
        {
            return;
        }
        
        // Update conversation rotation if in conversation
        UpdateConversationRotation();
        
        if (currentState != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude / 3.5f);
            currentState.UpdateState(); // Call UpdateState on the current state
        }

        // Update stamina through current state
        if (currentState != null)
        {
            currentState.UpdateStamina();
        }

        // Check for automatic sleep when stamina is critically low
        CheckForExhaustionSleep();

        // Update sickness system
        UpdateSicknessChance();
        
        // Check for medical treatment if sick (periodically)
        if (isSick && Time.frameCount % 300 == 0) // Check every 5 seconds at 60fps
        {
            CheckForMedicalTreatment();
        }

        // Update hunger
        if (currentHunger > 0)
        {
            currentHunger = Mathf.Max(0, currentHunger - (hungerDecreaseRate * Time.deltaTime));
            OnHungerChanged?.Invoke(currentHunger, maxHunger);

            // Update work speed multiplier based on hunger and sickness
            if (currentHunger <= starvationThreshold)
            {
                workSpeedMultiplier = 0f;
                
                // If we're currently working but too hungry, stop work and go eat
                if (GetCurrentTaskType() == TaskType.WORK)
                {
                    Debug.LogWarning($"[SettlerNPC] {name} is too hungry to work (hunger: {currentHunger}). Stopping work and going to eat.");
                    StopWork();
                    ChangeTask(TaskType.EAT);
                }
                
                if (currentHunger == 0)
                {
                    OnStarving?.Invoke();
                }
            }
            else if (currentHunger <= hungerThreshold)
            {
                workSpeedMultiplier = isSick ? (0.5f * sickWorkSpeedPenalty) : 0.5f;
                
                // If we're hungry and not already eating, and not in cooldown, and food is available, change to eat state
                if (GetCurrentTaskType() != TaskType.EAT && 
                    Time.time - lastNoFoodTime >= noFoodCooldown && 
                    HasAvailableFood())
                {
                    ChangeTask(TaskType.EAT);
                }
            }
            else
            {
                workSpeedMultiplier = isSick ? sickWorkSpeedPenalty : 1f;
                OnNoLongerStarving?.Invoke();
            }
        }
    }

    public void UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
    }

    public void RestoreStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
    }

    public void RestoreStaminaAtRate(float multiplier = 1f)
    {
        float staminaRestore = baseStaminaRegenRate * multiplier * Time.deltaTime;
        RestoreStamina(staminaRestore);
    }

    /// <summary>
    /// Apply stamina change from the current task state
    /// Called by task states to modify stamina based on their specific behavior
    /// </summary>
    public void ApplyStaminaChange(float staminaChange, string reason = "")
    {
        float oldStamina = currentStamina;
        currentStamina = Mathf.Clamp(currentStamina + staminaChange, 0f, maxStamina);
        
        // Enhanced debug logging for stamina changes
        if (Time.frameCount % 120 == 0 && !string.IsNullOrEmpty(reason)) // Log every 2 seconds at 60fps
        {
            float drainRatePerSecond = -staminaChange / Time.deltaTime;
            float timeToZeroAtCurrentRate = currentStamina / drainRatePerSecond;
            Debug.Log($"[SettlerNPC] {name} STAMINA CHANGE - {reason}:");
            Debug.Log($"  Change: {oldStamina:F1} -> {currentStamina:F1} (delta: {staminaChange:F4})");
            Debug.Log($"  Current drain rate: {drainRatePerSecond:F4}/s");
            Debug.Log($"  Time to zero at current rate: {timeToZeroAtCurrentRate:F1}s");
            Debug.Log($"  Time.deltaTime: {Time.deltaTime:F4}");
        }
    }
    
    /// <summary>
    /// Get base stamina drain rate for current conditions
    /// Used by task states to calculate their specific stamina effects
    /// </summary>
    public float GetBaseStaminaDrainRate()
    {
        bool isNight = GameManager.Instance?.TimeManager?.IsNight ?? false;
        float baseDrain = staminaDrainRate * fatigueRate;
        
        // Night activities are more tiring
        if (isNight)
        {
            baseDrain *= nightFatigueMultiplier;
        }
        
        return baseDrain;
    }
    
    /// <summary>
    /// Get stamina regeneration rate for sleeping
    /// Used by SleepState to calculate sleep regeneration
    /// </summary>
    public float GetSleepStaminaRegenRate(bool hasProperBed = false)
    {
        float sleepRegenMultiplier = hasProperBed ? sleepStaminaRegenMultiplier : sleepStaminaRegenMultiplier * 0.7f;
        return staminaRegenRate * sleepRegenMultiplier;
    }

    /// <summary>
    /// Check if NPC should automatically fall asleep due to exhaustion
    /// </summary>
    private void CheckForExhaustionSleep()
    {
        // Don't interrupt sleep or important states
        if (GetCurrentTaskType() == TaskType.SLEEP || 
            GetCurrentTaskType() == TaskType.FLEE || 
            GetCurrentTaskType() == TaskType.ATTACK)
        {
            return;
        }
        
        // Fall asleep if stamina is critically low (below 10%)
        if (GetStaminaPercentage() <= 20f)
        {
            Debug.Log($"[SettlerNPC] {name} is exhausted (stamina: {GetStaminaPercentage():F1}%), falling asleep");
            ChangeTask(TaskType.SLEEP);
        }
    }

    // Method to change states
    public void ChangeState(_TaskState newState)
    {
        Debug.Log($"[SettlerNPC] ChangeState called for {name} - Changing from {currentState?.GetTaskType()} to {newState?.GetTaskType()}");
        
        StopWorkAnimation();
        Debug.Log($"[SettlerNPC] StopWorkAnimation called during state change for {name}");

        if(currentState == newState){
            Debug.Log($"[SettlerNPC] Same state for {name}, returning early");
            return;
        }

        if (currentState != null)
        {
            Debug.Log($"[SettlerNPC] Exiting state {currentState.GetTaskType()} for {name}");
            currentState.OnExitState(); // Exit the old state
        }

        currentState = newState;

        if (newState != null)
        {
            Debug.Log($"[SettlerNPC] Entering state {newState.GetTaskType()} for {name}");
            currentState.OnEnterState(); // Enter the new state

            // Adjust the agent's speed according to the new state's requirements
            agent.speed = currentState.MaxSpeed();
            Debug.Log($"[SettlerNPC] State change complete for {name} - Now in {newState.GetTaskType()}");
        }
    }

    public override void PlayWorkAnimation(string animationName)
    {        
        // Set layer weight to 1 to ensure work animation plays
        animator.SetLayerWeight(workLayerIndex, 1f);
        animator.Play(animationName, workLayerIndex);
    }

    public void StopWorkAnimation()
    {
        Debug.Log($"[SettlerNPC] StopWorkAnimation called for {name} - Setting work layer to Empty state");
        animator.Play("Empty", workLayerIndex);
        
        // Also set the layer weight to 0 to ensure it doesn't interfere
        animator.SetLayerWeight(workLayerIndex, 0f);
        Debug.Log($"[SettlerNPC] Work animation stopped for {name} - Layer weight set to 0");
    }

    public override void StartWork(WorkTask newTask)
    {
        var workState = taskStates[TaskType.WORK] as WorkState;
        
        // If we're already in work state and the task is the same, don't do anything
        if (workState.assignedTask == newTask && GetCurrentTaskType() == TaskType.WORK)
        {
            return;
        }

        assignedWorkTask = newTask; // Store the assigned task
        workState.AssignTask(newTask);
        
        // Call the base class StartWork to handle the work coroutine
        base.StartWork(newTask);
        
        // If we're not already in work state, change to it
        if (GetCurrentTaskType() != TaskType.WORK)
        {
            ChangeTask(TaskType.WORK);
        }
        else
        {
            // We're already in work state, just update the destination
            workState.UpdateTaskDestination();
        }
    }

    public void TakeBreak()
    {
        if (assignedWorkTask != null)
        {
            isOnBreak = true;
            // Don't unassign from the task, just change state
            ChangeTask(TaskType.EAT);
        }
        else
        {
            Debug.LogWarning($"<color=blue>{gameObject.name}</color>: Attempted to take break but no work task assigned");
        }
    }

    public void ReturnToWork()
    {
        if (assignedWorkTask != null && isOnBreak)
        {
            isOnBreak = false;
            
            // Reassign the task to the WorkState
            (taskStates[TaskType.WORK] as WorkState).AssignTask(assignedWorkTask);
            
            ChangeTask(TaskType.WORK);
        }
        else
        {
            Debug.LogWarning($"<color=blue>{gameObject.name}</color>: Cannot return to work - AssignedTask: {(assignedWorkTask != null ? assignedWorkTask.name : "null")}, IsOnBreak: {isOnBreak}");
        }
    }

    public bool HasAssignedWork()
    {
        return assignedWorkTask != null;
    }

    public WorkTask GetAssignedWork()
    {
        return assignedWorkTask;
    }
    
    /// <summary>
    /// Check if this settler has been assigned to a bed
    /// </summary>
    /// <returns>True if the settler has a bed assignment, false otherwise</returns>
    public bool HasAssignedBed()
    {
        Debug.Log($"[SettlerNPC] HasAssignedBed called for {name}");
        
        // Check if we have a SleepTask assigned as work
        if (assignedWorkTask is SleepTask currentSleepTask)
        {
            bool result = currentSleepTask.IsBedAssigned && currentSleepTask.AssignedSettler == this;
            Debug.Log($"[SettlerNPC] {name} has SleepTask as assignedWorkTask: {result}");
            return result;
        }
        
        Debug.Log($"[SettlerNPC] {name} assignedWorkTask is: {assignedWorkTask?.GetType().Name ?? "null"}");
        
        // Also check if any SleepTask in the scene has us assigned
        var allSleepTasks = FindObjectsByType<SleepTask>(FindObjectsSortMode.None);
        Debug.Log($"[SettlerNPC] {name} found {allSleepTasks.Length} SleepTasks in scene");
        
        foreach (var sceneSleepTask in allSleepTasks)
        {
            if (sceneSleepTask.IsBedAssigned && sceneSleepTask.AssignedSettler == this)
            {
                Debug.Log($"[SettlerNPC] {name} found assigned to scene SleepTask: {sceneSleepTask.name}");
                return true;
            }
        }
        
        Debug.Log($"[SettlerNPC] {name} HasAssignedBed returning false");
        return false;
    }

    public override void StopWork()
    {
        Debug.Log($"[SettlerNPC] StopWork called for {name}");
        Debug.Log($"[SettlerNPC] - Current assigned work task: {assignedWorkTask?.GetType().Name ?? "null"}");
        Debug.Log($"[SettlerNPC] - Current task type: {GetCurrentTaskType()}");
        
        if (assignedWorkTask != null)
        {
            Debug.Log($"[SettlerNPC] {name} stopping work on {assignedWorkTask.GetType().Name}");
            
            // Call the base class StopWork to handle the work coroutine
            base.StopWork();
            Debug.Log($"[SettlerNPC] Called base.StopWork() for {name}");
            
            // Clear the assigned work and change task
            ClearAssignedWork();
            Debug.Log($"[SettlerNPC] Cleared assigned work for {name}");
            
            // Clear the WorkState's assigned task
            var workState = taskStates[TaskType.WORK] as WorkState;
            if (workState != null)
            {
                workState.AssignTask(null);
                Debug.Log($"[SettlerNPC] Cleared WorkState assigned task for {name}");
            }
            
            // Check for available work before going to wander
            if (CampManager.Instance?.WorkManager != null)
            {
                Debug.Log($"[SettlerNPC] {name} checking for next available task");
                bool taskAssigned = CampManager.Instance.WorkManager.AssignNextAvailableTask(this);
                if (!taskAssigned)
                {
                    // No tasks available, go to wander state
                    Debug.Log($"[SettlerNPC] No tasks available, {name} changing to WANDER");
                    ChangeTask(TaskType.WANDER);
                }
                else
                {
                    Debug.Log($"[SettlerNPC] {name} assigned to new task");
                }
            }
            else
            {
                Debug.Log($"[SettlerNPC] WorkManager null, {name} changing to WANDER");
                ChangeTask(TaskType.WANDER);
            }
        }
        else
        {
            Debug.Log($"[SettlerNPC] {name} StopWork called but no assigned work task");
        }
    }

    public void ClearAssignedWork()
    {
        Debug.Log($"[SettlerNPC] ClearAssignedWork called for {name} - Previous assigned work: {assignedWorkTask?.GetType().Name ?? "null"}");
        
        assignedWorkTask = null;
        isOnBreak = false;
    }

    // Method to change task and update state
    public void ChangeTask(TaskType newTask)
    {
        TaskType currentTaskType = currentState != null ? currentState.GetTaskType() : TaskType.WANDER;
        Debug.Log($"[SettlerNPC] ChangeTask called for {name} - From: {currentTaskType} To: {newTask}, AssignedWork: {(assignedWorkTask != null ? assignedWorkTask.GetType().Name : "null")}");
        
        if (taskStates.ContainsKey(newTask))
        {
            // If we're changing from work to eat, set isOnBreak
            if (currentState != null && currentState.GetTaskType() == TaskType.WORK && newTask == TaskType.EAT)
            {
                isOnBreak = true;
                Debug.Log($"[SettlerNPC] {name} taking break - changing from work to eat");
            }
            

            
            ChangeState(taskStates[newTask]);
        }
        else
        {
            Debug.LogWarning($"TaskType {newTask} does not exist in taskStates dictionary.");
        }
    }

    public NavMeshAgent GetAgent()
    {
        return agent; // Return the stored NavMeshAgent reference
    }

    public Animator GetAnimator()
    {
        return animator;
    }

    public TaskType GetCurrentTaskType()
    {
        if (currentState != null)
        {
            return currentState.GetTaskType();
        }
        return TaskType.WANDER;
    }

    public bool IsHungry()
    {
        return currentHunger <= hungerThreshold;
    }

    public bool IsStarving()
    {
        return currentHunger <= starvationThreshold;
    }

    public float GetHungerPercentage()
    {
        return currentHunger / maxHunger;
    }

    public void EatMeal(CookingRecipeScriptableObj recipe)
    {
        // Base hunger restoration from the meal
        float hungerRestore = recipe.hungerRestoreAmount;
        
        // If we have a recipe, we could potentially add bonuses or effects based on the recipe
        if (recipe != null)
        {
            // TODO: Add any special effects or bonuses based on the recipe
            // For now, we'll just use the recipe's hunger restoration
        }
        
        currentHunger = Mathf.Min(maxHunger, currentHunger + hungerRestore);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public float GetWorkSpeedMultiplier()
    {
        return workSpeedMultiplier;
    }

    public void SetNoFoodCooldown()
    {
        lastNoFoodTime = Time.time;
    }

    /// <summary>
    /// Get the current sickness status description for UI or debugging
    /// </summary>
    public string GetSicknessStatusDescription()
    {
        if (isSick)
        {
            float remainingTime = sicknessDuration - (Time.time - sicknessStartTime);
            return $"Sick (recovering in {remainingTime:F0}s)";
        }
        
        if (IsVeryTired())
        {
            return $"Exhausted (stamina: {currentStamina:F0}/{maxStamina:F0})";
        }
        
        if (IsTired())
        {
            return $"Tired (stamina: {currentStamina:F0}/{maxStamina:F0})";
        }
        
        return "Healthy";
    }

    /// <summary>
    /// Get a simple health status for UI purposes
    /// </summary>
    public HealthStatus GetHealthStatus()
    {
        if (isSick) return HealthStatus.Sick;
        if (IsStarving()) return HealthStatus.Starving;
        if (IsHungry()) return HealthStatus.Hungry;
        if (IsVeryTired()) return HealthStatus.Exhausted;
        if (IsTired()) return HealthStatus.Tired;
        return HealthStatus.Healthy;
    }

    /// <summary>
    /// Get access to the NPC's appearance system
    /// </summary>
    public NPCAppearanceSystem GetAppearanceSystem()
    {
        return appearanceSystem;
    }

    /// <summary>
    /// Set the recruited appearance data (called when spawning recruited NPCs)
    /// </summary>
    public void SetRecruitedAppearanceData(NPCAppearanceData appearanceData)
    {
        recruitedAppearanceData = appearanceData;
    }

    /// <summary>
    /// Apply procedural settler data (name, age, description) for settlers without NPCScriptableObj
    /// </summary>
    public void ApplySettlerData(Managers.SettlerData settlerData)
    {
        if (settlerData == null)
        {
            Debug.LogWarning($"[SettlerNPC] {gameObject.name} - Settler data is null!");
            return;
        }

        // Set the gameobject name to match the settler's name
        gameObject.name = $"Settler_{settlerData.name}";
        
        // Store settler data for potential UI display
        // Since we don't have an NPCScriptableObj, we store this data locally
        settlerName = settlerData.name;
        settlerAge = settlerData.age;
        settlerDescription = settlerData.description;
        
    }

    /// <summary>
    /// Get the settler's name from procedural data
    /// </summary>
    public string GetSettlerName()
    {
        return !string.IsNullOrEmpty(settlerName) ? settlerName : "Unknown Settler";
    }

    /// <summary>
    /// Get the settler's age from procedural data
    /// </summary>
    public int GetSettlerAge()
    {
        return settlerAge;
    }

    /// <summary>
    /// Get the settler's description from procedural data
    /// </summary>
    public string GetSettlerDescription()
    {
        return !string.IsNullOrEmpty(settlerDescription) ? settlerDescription : "A mysterious settler.";
    }

    /// <summary>
    /// Call this to put the NPC into the sheltered state (e.g., when entering a bunker)
    /// </summary>
    public void EnterShelter(BunkerBuilding bunker)
    {
        ChangeTask(TaskType.SHELTERED);
    }

    /// <summary>
    /// Call this to remove the NPC from the sheltered state (e.g., when leaving a bunker)
    /// </summary>
    public void ExitShelter()
    {
        // Ensure the GameObject is active before changing state
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        // Ensure the NavMeshAgent is enabled and on the NavMesh
        if (agent != null && !agent.enabled)
            agent.enabled = true;
        if (agent != null && !agent.isOnNavMesh)
        {
            // Try to warp the agent to the nearest NavMesh position
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        // Go to wander state - let WanderState handle threat detection and task assignment
        ChangeTask(TaskType.WANDER);
    }

    #region Conversation Control

    private _TaskState stateBeforeConversation;
    private bool wasAgentEnabledBeforeConversation;
    private bool isInConversation = false;

    /// <summary>
    /// Pauses the NPC's AI and movement during conversations
    /// </summary>
    public void PauseForConversation()
    {
        // Store current state to restore later
        stateBeforeConversation = currentState;
        wasAgentEnabledBeforeConversation = agent != null && agent.enabled;

        // Stop the NavMeshAgent
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // Disable current state to prevent AI updates
        if (currentState != null)
        {
            currentState.enabled = false;
        }

        isInConversation = true;
    }

    /// <summary>
    /// Resumes the NPC's AI and movement after conversations
    /// </summary>
    public void ResumeAfterConversation()
    {
        // Resume NavMeshAgent
        if (agent != null && wasAgentEnabledBeforeConversation)
        {
            agent.isStopped = false;
        }

        // Re-enable the previous state
        if (stateBeforeConversation != null)
        {
            stateBeforeConversation.enabled = true;
        }

        // Clear stored data
        stateBeforeConversation = null;
        isInConversation = false;
    }

    /// <summary>
    /// Update conversation rotation - called during conversation to face the player
    /// </summary>
    public void UpdateConversationRotation()
    {
        if (!isInConversation || PlayerController.Instance?._possessedNPC == null) return;

        Transform playerTransform = PlayerController.Instance._possessedNPC.GetTransform();
        NavigationUtils.RotateTowardsPlayerForConversation(transform, playerTransform, rotationSpeed);
    }

    /// <summary>
    /// Gets the Transform of this SettlerNPC for the IConversationTarget interface
    /// </summary>
    public new Transform GetTransform()
    {
        return transform;
    }

    #endregion

    #region Debug Visualization

    /// <summary>
    /// Draw debug information in the scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        // Only draw in editor and if the NPC is initialized
        #if UNITY_EDITOR
        if (!hasBeenInitialized && Application.isPlaying) return;
        
        // Position the text above the NPC
        Vector3 textPosition = transform.position + Vector3.up * 2.5f;
        
        // Build status text
        string statusText = BuildDebugStatusText();
        
        // Draw the debug text
        UnityEditor.Handles.Label(textPosition, statusText, GetDebugTextStyle());
        
        // Draw health status indicator gizmo
        DrawHealthStatusGizmo();
        #endif
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Build the debug status text for scene view display
    /// </summary>
    private string BuildDebugStatusText()
    {
        if (!Application.isPlaying)
        {
            return $"{GetSettlerName()}\n[Not Playing]";
        }
        
        var status = GetHealthStatus();
        var task = GetCurrentTaskType();
        var hungerPercent = GetHungerPercentage() * 100f;
        var staminaPercent = GetStaminaPercentage();
        var workSpeed = GetWorkSpeedMultiplier() * 100f;
        
        string statusText = $"{GetSettlerName()}\n";
        statusText += $"Status: {status}\n";
        statusText += $"Task: {task}\n";
        statusText += $"Hunger:{hungerPercent:F0}% Stamina:{staminaPercent:F0}%\n";
        statusText += $"Work: {workSpeed:F0}%";
        
        if (isSick)
        {
            float remainingTime = sicknessDuration - (Time.time - sicknessStartTime);
            statusText += $"\nSick: {remainingTime:F0}s";
        }
        
        return statusText;
    }
    
    /// <summary>
    /// Get the appropriate text style based on health status
    /// </summary>
    private UnityEngine.GUIStyle GetDebugTextStyle()
    {
        var style = new UnityEngine.GUIStyle();
        style.normal.textColor = GetHealthStatusColor();
        style.fontSize = 10;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        // Add background for better readability
        var bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
        bgTexture.Apply();
        style.normal.background = bgTexture;
        style.padding = new RectOffset(4, 4, 2, 2);
        
        return style;
    }
    
    /// <summary>
    /// Get color based on health status
    /// </summary>
    private Color GetHealthStatusColor()
    {
        var status = GetHealthStatus();
        return status switch
        {
            HealthStatus.Healthy => Color.green,
            HealthStatus.Hungry => Color.yellow,
            HealthStatus.Starving => new Color(1f, 0.5f, 0f), // Orange
            HealthStatus.Tired => Color.cyan,
            HealthStatus.Exhausted => Color.blue,
            HealthStatus.Sick => Color.red,
            _ => Color.white
        };
    }
    
    /// <summary>
    /// Draw a colored gizmo to indicate health status
    /// </summary>
    private void DrawHealthStatusGizmo()
    {
        Vector3 gizmoPosition = transform.position + Vector3.up * 2.2f;
        
        // Set gizmo color based on health status
        Gizmos.color = GetHealthStatusColor();
        
        // Draw a small sphere indicator
        Gizmos.DrawSphere(gizmoPosition, 0.1f);
        
        // Draw additional indicators for specific conditions
        if (isSick)
        {
            // Draw a red cross for sick NPCs
            Gizmos.color = Color.red;
            Vector3 crossPos = gizmoPosition + Vector3.right * 0.2f;
            Gizmos.DrawLine(crossPos + Vector3.up * 0.05f, crossPos + Vector3.down * 0.05f);
            Gizmos.DrawLine(crossPos + Vector3.right * 0.05f, crossPos + Vector3.left * 0.05f);
        }
        
        if (IsStarving())
        {
            // Draw hunger indicator (triangle)
            Gizmos.color = Color.red;
            Vector3 hungerPos = gizmoPosition + Vector3.left * 0.2f;
            Vector3[] trianglePoints = {
                hungerPos + Vector3.up * 0.05f,
                hungerPos + Vector3.down * 0.05f + Vector3.left * 0.05f,
                hungerPos + Vector3.down * 0.05f + Vector3.right * 0.05f
            };
            
            for (int i = 0; i < trianglePoints.Length; i++)
            {
                Gizmos.DrawLine(trianglePoints[i], trianglePoints[(i + 1) % trianglePoints.Length]);
            }
        }
        
        if (IsVeryTired())
        {
            // Draw sleep indicator (Z)
            Gizmos.color = Color.blue;
            Vector3 sleepPos = gizmoPosition + Vector3.forward * 0.2f;
            Gizmos.DrawLine(sleepPos + Vector3.up * 0.05f + Vector3.left * 0.05f, 
                          sleepPos + Vector3.up * 0.05f + Vector3.right * 0.05f);
            Gizmos.DrawLine(sleepPos + Vector3.up * 0.05f + Vector3.right * 0.05f, 
                          sleepPos + Vector3.down * 0.05f + Vector3.left * 0.05f);
            Gizmos.DrawLine(sleepPos + Vector3.down * 0.05f + Vector3.left * 0.05f, 
                          sleepPos + Vector3.down * 0.05f + Vector3.right * 0.05f);
        }
    }
    #endif

    #endregion
}

[System.Serializable]
public class NPCAppearanceSystem
{
    [Header("Model Options")]
    [SerializeField] private GameObject[] bodyModels; // Different body/mesh options
    [SerializeField] private GameObject[] headModels; // Different head options
    [SerializeField] private GameObject[] hairModels; // Different hair styles
    
    [Header("Clothing Options")]
    [SerializeField] private GameObject[] topClothing; // Shirts, jackets, etc.
    [SerializeField] private GameObject[] bottomClothing; // Pants, skirts, etc.
    [SerializeField] private GameObject[] footwear; // Shoes, boots, etc.
    
    [Header("Accessories")]
    [SerializeField] private GameObject[] headAccessories; // Hats, helmets, glasses
    [SerializeField] private GameObject[] backAccessories; // Backpacks, cloaks
    [SerializeField] private GameObject[] handAccessories; // Gloves, bracelets
    
    [Header("Material Variants")]
    [SerializeField] private Material[] skinMaterials; // Different skin tones
    [SerializeField] private Material[] hairMaterials; // Different hair colors
    [SerializeField] private Material[] clothingMaterials; // Different clothing colors
    
    [Header("Accessory Spawn Chances")]
    [Range(0f, 1f)] [SerializeField] private float headAccessoryChance = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float backAccessoryChance = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float handAccessoryChance = 0.2f;
    
    private SettlerNPC settlerNPC;
    private List<GameObject> activeModels = new List<GameObject>();

    public void SetSettlerNPC(SettlerNPC settlerNPC)
    {
        this.settlerNPC = settlerNPC;
        activeModels = new List<GameObject>();
    }
    
    /// <summary>
    /// Randomize the NPC's appearance using the available options
    /// </summary>
    public void RandomizeAppearance()
    {
        if (settlerNPC == null)
        {
            Debug.LogError("NPCAppearanceSystem: Cannot randomize appearance - settlerNPC is null");
            return;
        }
        
        
        // Clear any existing appearance models
        ClearCurrentAppearance();
        
        // Check if we have any models to work with
        bool hasAnyModels = (bodyModels?.Length > 0) || (headModels?.Length > 0) || (hairModels?.Length > 0) || 
                           (topClothing?.Length > 0) || (bottomClothing?.Length > 0) || (footwear?.Length > 0);
        
        if (!hasAnyModels)
        {
            Debug.LogError($"[NPCAppearanceSystem] No appearance models found for {settlerNPC.name}! Check prefab setup.");
            return;
        }
        
        // Randomize body parts
        if (bodyModels != null && bodyModels.Length > 0)
        {
            ActivateRandomModel(bodyModels, "Body");
        }
        else
        {
            Debug.LogWarning($"[NPCAppearanceSystem] No body models available for {settlerNPC.name}");
        }
        
        if (headModels != null && headModels.Length > 0)
        {
            ActivateRandomModel(headModels, "Head");
        }
        
        if (hairModels != null && hairModels.Length > 0)
        {
            ActivateRandomModel(hairModels, "Hair");
        }
        
        // Randomize clothing

        if (topClothing != null && topClothing.Length > 0)
        {
            ActivateRandomModel(topClothing, "Top Clothing");
        }
        
        if (bottomClothing != null && bottomClothing.Length > 0)
        {
            ActivateRandomModel(bottomClothing, "Bottom Clothing");
        }
        
        if (footwear != null && footwear.Length > 0)
        {
            ActivateRandomModel(footwear, "Footwear");
        }
        
        
        // Randomize accessories based on spawn chances
        if (headAccessories != null && headAccessories.Length > 0 && UnityEngine.Random.value <= headAccessoryChance)
        {
            ActivateRandomModel(headAccessories, "Head Accessory");
        }
        
        if (backAccessories != null && backAccessories.Length > 0 && UnityEngine.Random.value <= backAccessoryChance)
        {
            ActivateRandomModel(backAccessories, "Back Accessory");
        }
        
        if (handAccessories != null && handAccessories.Length > 0 && UnityEngine.Random.value <= handAccessoryChance)
        {
            ActivateRandomModel(handAccessories, "Hand Accessory");
        }        
        
        // Apply random materials
        ApplyRandomMaterials();
        
    }
    
    /// <summary>
    /// Activate a random model from the given array
    /// </summary>
    private void ActivateRandomModel(GameObject[] modelArray, string categoryName)
    {
        if (modelArray == null || modelArray.Length == 0) return;
        
        GameObject selectedModel = modelArray[UnityEngine.Random.Range(0, modelArray.Length)];
        if (selectedModel != null)
        {
            selectedModel.SetActive(true);
            activeModels.Add(selectedModel);
        }
    }
    
    /// <summary>
    /// Apply random materials to the active models
    /// </summary>
    private void ApplyRandomMaterials()
    {
        foreach (GameObject model in activeModels)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                // Apply random skin material if available
                if (skinMaterials != null && skinMaterials.Length > 0 && 
                    (model.name.Contains("Body") || model.name.Contains("Head")))
                {
                    Material randomSkinMaterial = skinMaterials[UnityEngine.Random.Range(0, skinMaterials.Length)];
                    renderer.material = randomSkinMaterial;
                }
                // Apply random hair material if available
                else if (hairMaterials != null && hairMaterials.Length > 0 && model.name.Contains("Hair"))
                {
                    Material randomHairMaterial = hairMaterials[UnityEngine.Random.Range(0, hairMaterials.Length)];
                    renderer.material = randomHairMaterial;
                }
                // Apply random clothing material if available
                else if (clothingMaterials != null && clothingMaterials.Length > 0)
                {
                    Material randomClothingMaterial = clothingMaterials[UnityEngine.Random.Range(0, clothingMaterials.Length)];
                    renderer.material = randomClothingMaterial;
                }
            }
        }
    }
    
    /// <summary>
    /// Clear all currently active appearance models
    /// </summary>
    public void ClearCurrentAppearance()
    {
        if (activeModels == null)
        {
            Debug.LogError("NPCAppearanceSystem: Cannot clear current appearance - activeModels is null");
            return;
        }

        foreach (GameObject model in activeModels)
        {
            if (model != null)
            {
                model.SetActive(false);
            }
        }
        activeModels.Clear();
    }
    
    /// <summary>
    /// Set specific appearance options (for saved/predefined appearances)
    /// </summary>
    public void SetAppearance(NPCAppearanceData appearanceData)
    {
        if (settlerNPC == null)
        {
            Debug.LogError("NPCAppearanceSystem: Cannot set appearance - settlerNPC is null");
            return;
        }

        if (appearanceData == null)
        {
            Debug.LogWarning($"[NPCAppearanceSystem] Appearance data is null for {settlerNPC.name}");
            return;
        }

        // Clear current appearance
        ClearCurrentAppearance();

        // Set body parts
        ActivateModelByName(bodyModels, appearanceData.bodyModelName, "Body");
        ActivateModelByName(headModels, appearanceData.headModelName, "Head");
        ActivateModelByName(hairModels, appearanceData.hairModelName, "Hair");

        // Set clothing
        ActivateModelByName(topClothing, appearanceData.topClothingName, "Top Clothing");
        ActivateModelByName(bottomClothing, appearanceData.bottomClothingName, "Bottom Clothing");
        ActivateModelByName(footwear, appearanceData.footwearName, "Footwear");

        // Set accessories (only if they have values)
        if (!string.IsNullOrEmpty(appearanceData.headAccessoryName))
        {
            ActivateModelByName(headAccessories, appearanceData.headAccessoryName, "Head Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.backAccessoryName))
        {
            ActivateModelByName(backAccessories, appearanceData.backAccessoryName, "Back Accessory");
        }
        if (!string.IsNullOrEmpty(appearanceData.handAccessoryName))
        {
            ActivateModelByName(handAccessories, appearanceData.handAccessoryName, "Hand Accessory");
        }

        // Apply saved materials
        ApplySavedMaterials(appearanceData);
    }
    
    /// <summary>
    /// Get current appearance data for saving
    /// </summary>
    public NPCAppearanceData GetCurrentAppearanceData()
    {
        NPCAppearanceData appearanceData = new NPCAppearanceData();
        
        if (activeModels == null || activeModels.Count == 0)
        {
            Debug.LogWarning($"[NPCAppearanceSystem] No active models found for {settlerNPC?.name ?? "Unknown NPC"}");
            return appearanceData;
        }

        foreach (GameObject activeModel in activeModels)
        {
            if (activeModel == null) continue;

            string modelName = activeModel.name;
            
            // Determine which type of model this is and store its name
            if (IsModelInArray(activeModel, bodyModels))
            {
                appearanceData.bodyModelName = modelName;
            }
            else if (IsModelInArray(activeModel, headModels))
            {
                appearanceData.headModelName = modelName;
            }
            else if (IsModelInArray(activeModel, hairModels))
            {
                appearanceData.hairModelName = modelName;
            }
            else if (IsModelInArray(activeModel, topClothing))
            {
                appearanceData.topClothingName = modelName;
            }
            else if (IsModelInArray(activeModel, bottomClothing))
            {
                appearanceData.bottomClothingName = modelName;
            }
            else if (IsModelInArray(activeModel, footwear))
            {
                appearanceData.footwearName = modelName;
            }
            else if (IsModelInArray(activeModel, headAccessories))
            {
                appearanceData.headAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, backAccessories))
            {
                appearanceData.backAccessoryName = modelName;
            }
            else if (IsModelInArray(activeModel, handAccessories))
            {
                appearanceData.handAccessoryName = modelName;
            }

            // Get material names from the active model
            Renderer[] renderers = activeModel.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer.material != null)
                {
                    string materialName = renderer.material.name.Replace(" (Instance)", "");
                    
                    // Categorize materials by model type
                    if (modelName.Contains("Body") || modelName.Contains("Head"))
                    {
                        appearanceData.skinMaterialName = materialName;
                    }
                    else if (modelName.Contains("Hair"))
                    {
                        appearanceData.hairMaterialName = materialName;
                    }
                    else
                    {
                        appearanceData.clothingMaterialName = materialName;
                    }
                }
            }
        }
        
        return appearanceData;
    }

    /// <summary>
    /// Helper method to check if a model exists in a given array
    /// </summary>
    private bool IsModelInArray(GameObject model, GameObject[] modelArray)
    {
        if (modelArray == null || model == null) return false;
        
        foreach (GameObject arrayModel in modelArray)
        {
            if (arrayModel == model) return true;
        }
        return false;
    }

    /// <summary>
    /// Activate a specific model by name from the given array
    /// </summary>
    private void ActivateModelByName(GameObject[] modelArray, string modelName, string categoryName)
    {
        if (modelArray == null || string.IsNullOrEmpty(modelName)) return;

        foreach (GameObject model in modelArray)
        {
            if (model != null && model.name == modelName)
            {
                model.SetActive(true);
                activeModels.Add(model);
                return;
            }
        }
        
        Debug.LogWarning($"[NPCAppearanceSystem] Could not find {categoryName} model with name: {modelName}");
    }

    /// <summary>
    /// Apply saved materials to active models
    /// </summary>
    private void ApplySavedMaterials(NPCAppearanceData appearanceData)
    {
        foreach (GameObject model in activeModels)
        {
            if (model == null) continue;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material targetMaterial = null;
                string modelName = model.name;

                // Determine which material to apply based on model type
                if ((modelName.Contains("Body") || modelName.Contains("Head")) && !string.IsNullOrEmpty(appearanceData.skinMaterialName))
                {
                    targetMaterial = FindMaterialByName(skinMaterials, appearanceData.skinMaterialName);
                }
                else if (modelName.Contains("Hair") && !string.IsNullOrEmpty(appearanceData.hairMaterialName))
                {
                    targetMaterial = FindMaterialByName(hairMaterials, appearanceData.hairMaterialName);
                }
                else if (!string.IsNullOrEmpty(appearanceData.clothingMaterialName))
                {
                    targetMaterial = FindMaterialByName(clothingMaterials, appearanceData.clothingMaterialName);
                }

                if (targetMaterial != null)
                {
                    renderer.material = targetMaterial;
                }
            }
        }
    }

    /// <summary>
    /// Find a material by name in the given material array
    /// </summary>
    private Material FindMaterialByName(Material[] materialArray, string materialName)
    {
        if (materialArray == null || string.IsNullOrEmpty(materialName)) return null;

        foreach (Material material in materialArray)
        {
            if (material != null && material.name == materialName)
            {
                return material;
            }
        }
        
        return null;
    }
}

/// <summary>
/// Data class to store NPC appearance information for saving/loading
/// </summary>
[System.Serializable]
public class NPCAppearanceData
{
    public string bodyModelName;
    public string headModelName;
    public string hairModelName;
    public string topClothingName;
    public string bottomClothingName;
    public string footwearName;
    public string headAccessoryName;
    public string backAccessoryName;
    public string handAccessoryName;
    public string skinMaterialName;
    public string hairMaterialName;
    public string clothingMaterialName;
}

namespace Characters.NPC
{
    [System.Serializable]
    public class NPCCharacteristicSystem
    {
        [Header("Characteristic Settings")]
        private int maxNPCCharacteristic = 3; //Max NPC character characteristics
        private float characteristicSpawnChance = 1f;
        private float rareCharacteristicChance = 0.1f;
        private int minRandomCharacteristic = 1;
        private int maxRandomCharacteristic = 3;

        [SerializeField, ReadOnly] private List<NPCCharacteristicScriptableObj> equippedCcharacteristics = new List<NPCCharacteristicScriptableObj>();
        private Dictionary<NPCCharacteristicScriptableObj, BaseNPCCharacteristicEffect> activeEffects = new Dictionary<NPCCharacteristicScriptableObj, BaseNPCCharacteristicEffect>();
        private SettlerNPC settlerNPC;
        public List<NPCCharacteristicScriptableObj> EquippedCharacteristics => equippedCcharacteristics;

        public NPCCharacteristicSystem(SettlerNPC settlerNPC)
        {
            if (settlerNPC == null)
            {
                Debug.LogError("NPCCharacteristicSystem: Cannot initialize with null SettlerNPC reference");
                return;
            }
            
            this.settlerNPC = settlerNPC;
        }

        public void ApplyRandomCharacteristic()
        {
            if (settlerNPC == null)
            {
                Debug.LogError("NPCCharacteristicSystem: Cannot apply random characteristics - settlerNPC is null");
                return;
            }

            // Determine if this NPC should get characteristics
            if(UnityEngine.Random.value > characteristicSpawnChance)
            {
                return;
            }

            // Get all available characteristic from the manager
            List<NPCCharacteristicScriptableObj> allCharacteristic = NPCManager.Instance.GetAllCharacteristics();
            if (allCharacteristic.Count == 0)
            {
                Debug.LogWarning($"NPCCharacteristicSystem: No characteristics available in NPCManager for {settlerNPC.name}");
                return;
            }

            // Determine number of characteristics
            int numCharacteristic = UnityEngine.Random.Range(minRandomCharacteristic, maxRandomCharacteristic + 1);

            // Create a list of valid characteristic indices (excluding already equipped characteristics)
            List<int> validIndices = new List<int>();
            for (int i = 0; i < allCharacteristic.Count; i++)
            {
                if (!equippedCcharacteristics.Contains(allCharacteristic[i]))
                {
                    validIndices.Add(i);
                }
            }

            int characteristicApplied = 0;
            int maxAttempts = validIndices.Count * 2; // Prevent infinite loops
            int attempts = 0;

            while (characteristicApplied < numCharacteristic && validIndices.Count > 0 && attempts < maxAttempts)
            {
                attempts++;

                // Select a random valid characteristic index
                int validIndex = UnityEngine.Random.Range(0, validIndices.Count);
                int characteristicIndex = validIndices[validIndex];
                NPCCharacteristicScriptableObj characteristic = allCharacteristic[characteristicIndex];

                // Check rarity
                if (characteristic.rarity == ItemRarity.RARE || characteristic.rarity == ItemRarity.LEGENDARY)
                {
                    if (UnityEngine.Random.value > rareCharacteristicChance)
                    {
                        // Remove this index from valid indices since we won't try it again
                        validIndices.RemoveAt(validIndex);
                        continue;
                    }
                }

                // Add characteristic to NPC
                EquipCharacteristic(characteristic);
                characteristicApplied++;

                // Remove this index from valid indices to prevent duplicates
                validIndices.RemoveAt(validIndex);
            }
        }

        public void EquipCharacteristic(NPCCharacteristicScriptableObj characteristic)
        {
            if (characteristic == null)
            {
                Debug.LogError($"NPCCharacteristicSystem: Cannot equip null characteristic to {settlerNPC.name}");
                return;
            }

            if (equippedCcharacteristics.Count >= maxNPCCharacteristic)
            {
                Debug.LogWarning($"NPCCharacteristicSystem: Cannot equip characteristic {characteristic.name} to {settlerNPC.name} - maximum characteristics reached");
                return;
            }

            if (equippedCcharacteristics.Contains(characteristic))
            {
                Debug.LogWarning($"NPCCharacteristicSystem: {settlerNPC.name} already has characteristic {characteristic.name}");
                return;
            }

            equippedCcharacteristics.Add(characteristic);
            
            // Instantiate and initialize the characteristic effect
            if (characteristic.prefab != null)
            {
                GameObject effectObj = GameObject.Instantiate(characteristic.prefab, settlerNPC.transform);
                BaseNPCCharacteristicEffect effect = effectObj.GetComponent<BaseNPCCharacteristicEffect>();
                if (effect != null)
                {
                    effect.Initialize(characteristic, settlerNPC);
                    effect.OnEquip();
                    activeEffects[characteristic] = effect;
                }
                else
                {
                    Debug.LogError($"NPCCharacteristicSystem: characteristic prefab {characteristic.name} does not have a BaseNPCCharacteristicEffect component");
                    GameObject.Destroy(effectObj);
                }
            }
            else
            {
                Debug.LogWarning($"NPCCharacteristicSystem: characteristic {characteristic.name} has no prefab assigned");
            }
        }

        public void RemoveCharacteristic(NPCCharacteristicScriptableObj characteristic)
        {
            if (characteristic == null)
            {
                Debug.LogError($"NPCCharacteristicSystem: Cannot remove null characteristic from {settlerNPC.name}");
                return;
            }

            if (equippedCcharacteristics.Remove(characteristic))
            {
                // Remove and cleanup the characteristic effect
                if (activeEffects.TryGetValue(characteristic, out BaseNPCCharacteristicEffect effect))
                {
                    effect.OnUnequip();
                    GameObject.Destroy(effect.gameObject);
                    activeEffects.Remove(characteristic);
                }
                else
                {
                    Debug.LogWarning($"NPCCharacteristicSystem: No active effect found for characteristic {characteristic.name} on {settlerNPC.name}");
                }
            }
            else
            {
                Debug.LogWarning($"NPCCharacteristicSystem: Characteristic {characteristic.name} not found in equipped characteristic for {settlerNPC.name}");
            }
        }

        // Helper method to check if NPC has a specific characteristic
        public bool HasCharacteristic(NPCCharacteristicScriptableObj characteristic)
        {
            return equippedCcharacteristics.Contains(characteristic);
        }

        // Helper method to get all active effects of a specific type
        public List<T> GetActiveEffectsOfType<T>() where T : BaseNPCCharacteristicEffect
        {
            List<T> effects = new List<T>();
            foreach (var effect in activeEffects.Values)
            {
                if (effect is T typedEffect)
                {
                    effects.Add(typedEffect);
                }
            }
            return effects;
        }
    }
}

/// <summary>
/// Enum for NPC health status including sickness and other conditions
/// </summary>
public enum HealthStatus
{
    Healthy,
    Hungry,
    Starving,
    Tired,
    Exhausted,
    Sick
}
