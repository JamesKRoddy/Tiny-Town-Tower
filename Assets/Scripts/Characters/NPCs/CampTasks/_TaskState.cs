using UnityEngine;
using UnityEngine.AI;
using Managers;

public abstract class _TaskState : MonoBehaviour
{
    protected SettlerNPC npc;
    protected NavMeshAgent agent;
    protected float stoppingDistance = 1.5f;
    protected Animator animator;

    protected virtual void Awake()
    {
        // Ensure NPC reference is set when the state starts
        if (npc == null)
        {
            SetNPCReference(GetComponent<SettlerNPC>());
        }
    }

    // This method will be called to set the NPC reference once the state is added to the NPC
    public void SetNPCReference(SettlerNPC npc)
    {
        this.npc = npc;
        UpdateReferences();
    }

    // Update component references
    protected void UpdateReferences()
    {
        if (npc != null)
        {
            agent = npc.GetAgent();
            animator = npc.GetAnimator();
        }
        else
        {
            Debug.LogWarning($"NPC reference is null in {gameObject.name}'s {GetType().Name}");
        }
    }

    /// <summary>
    /// Get the effective stopping distance for this task state using shared navigation utilities
    /// </summary>
    /// <param name="target">The target transform (can be null for general stopping distance)</param>
    /// <param name="obstacleBoundsOffset">Additional distance to add to obstacle bounds</param>
    /// <returns>The effective stopping distance</returns>
    protected float GetEffectiveStoppingDistance(Transform target = null, float obstacleBoundsOffset = 0.5f)
    {
        if (agent == null) return stoppingDistance;
        
        return NavigationUtils.GetEffectiveStoppingDistance(agent, target, stoppingDistance, obstacleBoundsOffset);
    }

    /// <summary>
    /// Check if the agent has reached its destination using shared navigation utilities
    /// </summary>
    /// <param name="target">The target transform (can be null for general destination checking)</param>
    /// <param name="obstacleBoundsOffset">Additional distance to add to obstacle bounds</param>
    /// <returns>True if the agent has reached the destination</returns>
    protected bool HasReachedDestination(Transform target = null, float obstacleBoundsOffset = 0.5f)
    {
        if (agent == null) return false;
        
        return NavigationUtils.HasReachedDestination(agent, target, stoppingDistance, obstacleBoundsOffset);
    }

    /// <summary>
    /// Shared method to check if food is available at any operational canteen
    /// </summary>
    protected bool HasAvailableFood()
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
    /// Shared method to check for nearby enemy threats
    /// </summary>
    /// <param name="detectionRange">Range to check for threats (default 10f)</param>
    /// <returns>True if threats are detected nearby</returns>
    protected bool CheckForNearbyThreats(float detectionRange = 10f)
    {
        // Check for nearby enemies using FindObjectsByType
        var nearbyEnemies = FindObjectsByType<Enemies.EnemyBase>(FindObjectsSortMode.None);
        
        foreach (var enemy in nearbyEnemies)
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(npc.transform.position, enemy.transform.position);
                if (distance <= detectionRange)
                {
                    return true; // Threat detected
                }
            }
        }
        
        return false; // No threats nearby
    }

    /// <summary>
    /// Shared method to find the nearest canteen with available food
    /// </summary>
    /// <returns>Nearest canteen with food, or null if none found</returns>
    protected CanteenBuilding FindNearestCanteen()
    {
        CanteenBuilding nearest = null;
        float nearestDistance = float.MaxValue;

        var canteens = CampManager.Instance.CookingManager.GetRegisteredCanteens();
        foreach (var canteen in canteens)
        {
            if (canteen.HasAvailableMeals() && canteen.IsOperational())
            {
                float distance = Vector3.Distance(npc.transform.position, canteen.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = canteen;
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// Shared method to reset agent to default state
    /// </summary>
    protected void ResetAgentState()
    {
        if (agent != null)
        {
            agent.speed = MaxSpeed();
            agent.angularSpeed = npc.rotationSpeed;
            agent.isStopped = false;
            agent.stoppingDistance = stoppingDistance;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
    }

    /// <summary>
    /// Shared method to try assigning work or fallback to wander
    /// </summary>
    protected void TryAssignWorkOrWander()
    {
        if (CampManager.Instance?.WorkManager != null)
        {
            bool taskAssigned = CampManager.Instance.WorkManager.AssignNextAvailableTask(npc);
            if (!taskAssigned)
            {
                npc.ChangeTask(TaskType.WANDER);
            }
        }
        else
        {
            npc.ChangeTask(TaskType.WANDER);
        }
    }

    public abstract TaskType GetTaskType();

    // Called when the state is entered
    public abstract void OnEnterState();

    // Called when the state is exited
    public abstract void OnExitState();

    // Called every frame to update the state
    public abstract void UpdateState();

    // Max speed for the NPC in this state
    public virtual float MaxSpeed() { return npc.moveMaxSpeed; }
}
