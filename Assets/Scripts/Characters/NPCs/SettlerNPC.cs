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
    [Header("NPC Data")]
    public NPCScriptableObj nPCDataObj;
    [SerializeField, ReadOnly] internal NPCCharacteristicSystem characteristicSystem;
    [SerializeField] internal NPCAppearanceSystem appearanceSystem;
    private _TaskState currentState;
    private WorkTask assignedWorkTask; // Track the assigned work task
    private bool isOnBreak = false; // Track if NPC is on break

    [Header("Initialization Control")]
    [SerializeField] private NPCInitializationContext initializationContext = NPCInitializationContext.FRESH_SPAWN; //Set this to the context of the NPC when it is spawned, override to loaded for NPCs in scene already
    [SerializeField, ReadOnly] private bool hasBeenInitialized = false;

    [Header("NPC Stats")]
    public int additionalMutationSlots = 3; //Additional mutation slots

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaRegenRate = 5f;
    public float fatigueRate = 2f;

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

    private int workLayerIndex;

    // Dictionary that maps TaskType to TaskState
    Dictionary<TaskType, _TaskState> taskStates = new Dictionary<TaskType, _TaskState>();

    protected override void Awake()
    {
        base.Awake();

        // Initialize characteristic system
        characteristicSystem = new NPCCharacteristicSystem(this);

        // Initialize appearance system
        appearanceSystem.SetSettlerNPC(this);

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
                InitializeAsLoadedFromSave();
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
        // Do NOT randomize appearance - it should be carried over from the recruited NPC gameobject
        // The appearance was already set when the NPC was first spawned/recruited
        
        // Default to WanderState
        if (taskStates.ContainsKey(TaskType.WANDER))
        {
            ChangeState(taskStates[TaskType.WANDER]);
        }

        // Apply characteristics from NPCScriptableObj or random if none specified
        if (characteristicSystem != null)
        {
            // TODO: Check if nPCDataObj has predefined characteristics, otherwise apply random
            characteristicSystem.ApplyRandomCharacteristic();
        }
    }

    /// <summary>
    /// Initialize as an NPC loaded from save data
    /// </summary>
    private void InitializeAsLoadedFromSave()
    {
        // Don't apply random characteristics - they should be restored from save data
        // Don't default to WanderState - task state should be restored from save data
        // This method is called when NPCs are loaded from save files
        Debug.Log($"[SettlerNPC] {gameObject.name} initialized as loaded from save - characteristics and state should be restored externally");
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
            Debug.Log($"[SettlerNPC] Restored appearance for {gameObject.name}");
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
        Debug.Log($"[SettlerNPC] {gameObject.name} state restored from save data");
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

    private void Update()
    {
        // Don't do anything if dead
        if (Health <= 0) 
        {
            return;
        }
        
        if (currentState != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude / 3.5f);
            currentState.UpdateState(); // Call UpdateState on the current state
        }

        // Regenerate stamina
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);

        // Update hunger
        if (currentHunger > 0)
        {
            currentHunger = Mathf.Max(0, currentHunger - (hungerDecreaseRate * Time.deltaTime));
            OnHungerChanged?.Invoke(currentHunger, maxHunger);

            // Update work speed multiplier based on hunger
            if (currentHunger <= starvationThreshold)
            {
                workSpeedMultiplier = 0f;
                if (currentHunger == 0)
                {
                    OnStarving?.Invoke();
                }
            }
            else if (currentHunger <= hungerThreshold)
            {
                workSpeedMultiplier = 0.5f;
                
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
                workSpeedMultiplier = 1f;
                OnNoLongerStarving?.Invoke();
            }
        }
    }

    public void UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
    }

    // Method to change states
    public void ChangeState(_TaskState newState)
    {
        
        StopWorkAnimation();

        if(currentState == newState){
            return;
        }

        if (currentState != null)
        {
            currentState.OnExitState(); // Exit the old state
        }

        currentState = newState;

        if (newState != null)
        {
            currentState.OnEnterState(); // Enter the new state

            // Adjust the agent's speed according to the new state's requirements
            agent.speed = currentState.MaxSpeed();
        }
    }

    public override void PlayWorkAnimation(string animationName)
    {
        animator.Play(animationName, workLayerIndex);
    }

    public void StopWorkAnimation()
    {
        animator.Play("Empty", workLayerIndex);
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

    public void StopWork()
    {
        
        if (assignedWorkTask != null)
        {
            // Stop the current work
            assignedWorkTask.StopWorkCoroutine();
            assignedWorkTask.UnassignNPC();
            
            // Clear the assigned work and change task
            ClearAssignedWork();
            
            // Clear the WorkState's assigned task
            var workState = taskStates[TaskType.WORK] as WorkState;
            if (workState != null)
            {
                workState.AssignTask(null);
            }
            
            // Check for available work before going to wander
            if (CampManager.Instance?.WorkManager != null)
            {
                bool taskAssigned = CampManager.Instance.WorkManager.AssignNextAvailableTask(this);
                if (!taskAssigned)
                {
                    // No tasks available, go to wander state
                    ChangeTask(TaskType.WANDER);
                }
            }
            else
            {
            ChangeTask(TaskType.WANDER);
            }
        }
    }

    public void ClearAssignedWork()
    {
        assignedWorkTask = null;
        isOnBreak = false;
    }

    // Method to change task and update state
    public void ChangeTask(TaskType newTask)
    {
        if (taskStates.ContainsKey(newTask))
        {
            // If we're changing from work to eat, set isOnBreak
            if (currentState != null && currentState.GetTaskType() == TaskType.WORK && newTask == TaskType.EAT)
            {
                isOnBreak = true;
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
    /// Get access to the NPC's appearance system
    /// </summary>
    public NPCAppearanceSystem GetAppearanceSystem()
    {
        return appearanceSystem;
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
    }

    /// <summary>
    /// Gets the Transform of this SettlerNPC for the IConversationTarget interface
    /// </summary>
    public new Transform GetTransform()
    {
        return transform;
    }

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
        Debug.Log($"[NPCAppearanceSystem] bodyModels count: {bodyModels.Length}");
        // Randomize body parts
        if (bodyModels != null && bodyModels.Length > 0)
        {
            Debug.Log($"[NPCAppearanceSystem] Randomizing body models for {settlerNPC.name}");
            ActivateRandomModel(bodyModels, "Body");
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
        
        Debug.Log($"[NPCAppearanceSystem] Randomized appearance for {settlerNPC.name} with {activeModels.Count} active models");
    }
    
    /// <summary>
    /// Activate a random model from the given array
    /// </summary>
    private void ActivateRandomModel(GameObject[] modelArray, string categoryName)
    {
        Debug.Log($"[NPCAppearanceSystem] Activating random model for {categoryName}");
        if (modelArray == null || modelArray.Length == 0) return;
        
        GameObject selectedModel = modelArray[UnityEngine.Random.Range(0, modelArray.Length)];
        if (selectedModel != null)
        {
            Debug.Log($"[NPCAppearanceSystem] Instantiating model: {selectedModel.name}");
            selectedModel.SetActive(true);
            activeModels.Add(selectedModel);
            Debug.Log($"[NPCAppearanceSystem] Activated {categoryName}: {selectedModel.name}");
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

        Debug.Log($"[NPCAppearanceSystem] Set appearance for {settlerNPC.name} with {activeModels.Count} active models");
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
        
        Debug.Log($"[NPCAppearanceSystem] Captured appearance data for {settlerNPC?.name ?? "Unknown NPC"} with {activeModels.Count} active models");
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
                Debug.Log($"[NPCAppearanceSystem] Activated {categoryName}: {modelName}");
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
                if (characteristic.rarity == ResourceRarity.RARE || characteristic.rarity == ResourceRarity.LEGENDARY)
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
