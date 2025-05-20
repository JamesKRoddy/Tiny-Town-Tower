using System.Collections.Generic;
using System;
using Characters.NPC;
using Characters.NPC.Mutations;
using Managers;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class SettlerNPC : HumanCharacterController
{
    [Header("NPC Data")]
    public SettlerNPCScriptableObj nPCDataObj;
    [SerializeField, ReadOnly] internal NPCMutationSystem mutationSystem;
    private _TaskState currentState;

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

    public event Action<float, float> OnHungerChanged; // Current hunger, max hunger
    public event Action OnStarving;
    public event Action OnNoLongerStarving;

    // Dictionary that maps TaskType to TaskState
    Dictionary<TaskType, _TaskState> taskStates = new Dictionary<TaskType, _TaskState>();

    protected override void Awake()
    {
        base.Awake();

        // Initialize mutation system
        mutationSystem = new NPCMutationSystem(this);

        // Get all TaskState components attached to the SettlerNPC GameObject
        _TaskState[] states = GetComponents<_TaskState>();

        // Populate the dictionary with TaskType -> TaskState mappings
        foreach (var state in states)
        {
            taskStates.Add(state.GetTaskType(), state);
            state.SetNPCReference(this);
        }
    }

    private void Start()
    {
        // Ensure NPC reference is set for each state component
        // Default to WanderState
        if (taskStates.ContainsKey(TaskType.WANDER))
        {
            ChangeState(taskStates[TaskType.WANDER]);
        }

        // Apply random mutations after everything is initialized
        if (mutationSystem != null)
        {
            mutationSystem.ApplyRandomMutations();
        }

        // Register with NPCManager
        NPCManager.Instance.RegisterNPC(this);
    }

    private void OnDestroy()
    {
        // Unregister from NPCManager
        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.UnregisterNPC(this);
        }
    }

    private void Update()
    {
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
                
                // If we're hungry and not already eating, change to eat state
                if (GetCurrentTaskType() != TaskType.EAT)
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

    public override void StartWork(WorkTask newTask)
    {
        if((taskStates[TaskType.WORK] as WorkState).assignedTask == newTask){
            return;
        }

        (taskStates[TaskType.WORK] as WorkState).AssignTask(newTask);
        ChangeTask(TaskType.WORK);
    }

    // Method to change task and update state
    public void ChangeTask(TaskType newTask)
    {
        if (taskStates.ContainsKey(newTask))
        {
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
}

namespace Characters.NPC
{
    public class NPCMutationSystem
    {
        [Header("Mutation Settings")]
        private int maxMutations = 3;
        private float mutationSpawnChance = 1f;
        private float rareMutationChance = 0.1f;
        private int minRandomMutations = 1;
        private int maxRandomMutations = 3;

        [SerializeField, ReadOnly] private List<NPCMutationScriptableObj> equippedMutations = new List<NPCMutationScriptableObj>();
        private Dictionary<NPCMutationScriptableObj, BaseNPCMutationEffect> activeEffects = new Dictionary<NPCMutationScriptableObj, BaseNPCMutationEffect>();
        private SettlerNPC settlerNPC;

        public int MaxMutations => maxMutations;
        public List<NPCMutationScriptableObj> EquippedMutations => equippedMutations;

        public NPCMutationSystem(SettlerNPC settlerNPC)
        {
            if (settlerNPC == null)
            {
                Debug.LogError("NPCMutationSystem: Cannot initialize with null SettlerNPC reference");
                return;
            }
            
            this.settlerNPC = settlerNPC;
        }

        public void ApplyRandomMutations()
        {
            if (settlerNPC == null)
            {
                Debug.LogError("NPCMutationSystem: Cannot apply random mutations - settlerNPC is null");
                return;
            }

            // Determine if this NPC should get mutations
            if(UnityEngine.Random.value > mutationSpawnChance)
            {
                Debug.Log($"NPCMutationSystem: {settlerNPC.name} did not receive mutations (chance roll failed)");
                return;
            }

            // Get all available mutations from the manager
            List<NPCMutationScriptableObj> allMutations = NPCManager.Instance.GetAllMutations();
            if (allMutations.Count == 0)
            {
                Debug.LogWarning($"NPCMutationSystem: No mutations available in NPCManager for {settlerNPC.name}");
                return;
            }

            // Determine number of mutations
            int numMutations = UnityEngine.Random.Range(minRandomMutations, maxRandomMutations + 1);

            if (allMutations.Count == 0) return;

            for (int i = 0; i < numMutations; i++)
            {
                if (allMutations.Count == 0) break;

                // Select a random mutation
                int index = UnityEngine.Random.Range(0, allMutations.Count);
                NPCMutationScriptableObj mutation = allMutations[index];

                // Check rarity
                if (mutation.rarity == ResourceRarity.RARE || mutation.rarity == ResourceRarity.LEGENDARY)
                {
                    if (UnityEngine.Random.value > rareMutationChance)
                    {
                        continue;
                    }
                }

                // Add mutation to NPC
                EquipMutation(mutation);

                // Remove from valid mutations to prevent duplicates
                allMutations.RemoveAt(index);
            }
        }

        public void EquipMutation(NPCMutationScriptableObj mutation)
        {
            if (mutation == null)
            {
                Debug.LogError($"NPCMutationSystem: Cannot equip null mutation to {settlerNPC.name}");
                return;
            }

            if (equippedMutations.Count >= maxMutations)
            {
                Debug.LogWarning($"NPCMutationSystem: Cannot equip mutation {mutation.name} to {settlerNPC.name} - maximum mutations reached");
                return;
            }

            if (equippedMutations.Contains(mutation))
            {
                Debug.LogWarning($"NPCMutationSystem: {settlerNPC.name} already has mutation {mutation.name}");
                return;
            }

            equippedMutations.Add(mutation);
            
            // Instantiate and initialize the mutation effect
            if (mutation.prefab != null)
            {
                GameObject effectObj = GameObject.Instantiate(mutation.prefab, settlerNPC.transform);
                BaseNPCMutationEffect effect = effectObj.GetComponent<BaseNPCMutationEffect>();
                if (effect != null)
                {
                    effect.Initialize(mutation, settlerNPC);
                    effect.OnEquip();
                    activeEffects[mutation] = effect;
                }
                else
                {
                    Debug.LogError($"NPCMutationSystem: Mutation prefab {mutation.name} does not have a BaseNPCMutationEffect component");
                    GameObject.Destroy(effectObj);
                }
            }
            else
            {
                Debug.LogWarning($"NPCMutationSystem: Mutation {mutation.name} has no prefab assigned");
            }
        }

        public void RemoveMutation(NPCMutationScriptableObj mutation)
        {
            if (mutation == null)
            {
                Debug.LogError($"NPCMutationSystem: Cannot remove null mutation from {settlerNPC.name}");
                return;
            }

            if (equippedMutations.Remove(mutation))
            {
                // Remove and cleanup the mutation effect
                if (activeEffects.TryGetValue(mutation, out BaseNPCMutationEffect effect))
                {
                    effect.OnUnequip();
                    GameObject.Destroy(effect.gameObject);
                    activeEffects.Remove(mutation);
                }
                else
                {
                    Debug.LogWarning($"NPCMutationSystem: No active effect found for mutation {mutation.name} on {settlerNPC.name}");
                }
            }
            else
            {
                Debug.LogWarning($"NPCMutationSystem: Mutation {mutation.name} not found in equipped mutations for {settlerNPC.name}");
            }
        }

        public void SetMaxMutations(int count)
        {
            if (count < 0)
            {
                Debug.LogError($"NPCMutationSystem: Cannot set max mutations to negative value {count} for {settlerNPC.name}");
                return;
            }

            if (count < equippedMutations.Count)
            {
                Debug.LogWarning($"NPCMutationSystem: Setting max mutations to {count} for {settlerNPC.name} which has {equippedMutations.Count} equipped mutations");
            }

            maxMutations = count;
        }

        // Helper method to check if NPC has a specific mutation
        public bool HasMutation(NPCMutationScriptableObj mutation)
        {
            return equippedMutations.Contains(mutation);
        }

        // Helper method to get all active effects of a specific type
        public List<T> GetActiveEffectsOfType<T>() where T : BaseNPCMutationEffect
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
