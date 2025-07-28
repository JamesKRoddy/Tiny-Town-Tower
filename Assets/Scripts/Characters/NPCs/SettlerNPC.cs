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
    private _TaskState currentState;
    private WorkTask assignedWorkTask; // Track the assigned work task
    private bool isOnBreak = false; // Track if NPC is on break

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

        // Ensure NPC reference is set for each state component
        // Default to WanderState
        if (taskStates.ContainsKey(TaskType.WANDER))
        {
            ChangeState(taskStates[TaskType.WANDER]);
        }

        // Apply random characteristics after everything is initialized
        if (characteristicSystem != null)
        {
            characteristicSystem.ApplyRandomCharacteristic();
        }

        // Register with NPCManager
        NPCManager.Instance.RegisterNPC(this);
        
        // Register with CampManager for wave management
        if (CampManager.Instance != null)
        {
            CampManager.Instance.AddNPC(this);
        }
    }

    private void OnDestroy()
    {
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

namespace Characters.NPC
{
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
