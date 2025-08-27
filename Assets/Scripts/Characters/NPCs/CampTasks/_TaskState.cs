using UnityEngine;
using UnityEngine.AI;
using Managers;
using System.Collections;

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
        Debug.Log($"[TaskState] TryAssignWorkOrWander called for {npc.name} from {GetTaskType()} state");
        
        // First check if NPC has an assigned work task they should return to
        if(npc.HasAssignedWorkTask)
        {
            var assignedWork = npc.GetAssignedWork();
            Debug.Log($"[TaskState] {npc.name} has assigned work task: {assignedWork.GetType().Name}");
            
            // Validate the assigned task is still available
            bool canPerform = assignedWork.CanPerformTask();
            bool isCompleted = assignedWork.IsTaskCompleted;
            Debug.Log($"[TaskState] {npc.name} assigned task validation - CanPerform: {canPerform}, IsCompleted: {isCompleted}");
            
            if (canPerform)
            {
                Debug.Log($"[TaskState] {npc.name} returning to assigned work task: {assignedWork.GetType().Name}");
                npc.StartWork(assignedWork);
                return;
            }
            else
            {
                Debug.Log($"[TaskState] {npc.name} assigned work task cannot be performed, clearing it");
                npc.ClearAssignedWork();
            }
        }
        
        // If no assigned work or it was invalid, try to find new work
        if (CampManager.Instance?.WorkManager != null)
        {
            bool taskAssigned = CampManager.Instance.WorkManager.AssignNextAvailableTask(npc);
            Debug.Log($"[TaskState] AssignNextAvailableTask result for {npc.name}: {taskAssigned}");
            
            if (!taskAssigned)
            {
                // Try again with a small delay in case new tasks are being added
                StartCoroutine(RetryWorkAssignment());
            }
        }
        else
        {
            Debug.Log($"[TaskState] WorkManager not available, changing {npc.name} to WANDER");
            npc.ChangeTask(TaskType.WANDER);
        }
    }

    /// <summary>
    /// Shared method to handle precise positioning using WorkTask system
    /// Used by both WorkState and SleepState for consistent positioning behavior
    /// </summary>
    /// <param name="precisePosition">The precise position transform to move to</param>
    /// <param name="needsPrecisePositioning">Reference to the precise positioning flag</param>
    /// <returns>True if precise positioning is complete, false if still in progress</returns>
    protected bool UpdatePrecisePositioning(Transform precisePosition, ref bool needsPrecisePositioning)
    {
        if (!needsPrecisePositioning || precisePosition == null)
        {
            return true; // No precise positioning needed or already complete
        }

        Vector3 oldPosition = transform.position;
        float distanceToTarget = Vector3.Distance(transform.position, precisePosition.position);
        
        // Use faster lerping for precise positioning (8f for quicker movement)
        float lerpSpeed = 8f;
        transform.position = Vector3.Lerp(transform.position, precisePosition.position, 
            Time.deltaTime * lerpSpeed);
        
        // Use centralized rotation utility
        NavigationUtils.RotateTowardsWorkPoint(transform, precisePosition, lerpSpeed);

        // Check if we've reached the precise position
        float newDistance = Vector3.Distance(transform.position, precisePosition.position);
        
        // Use a more generous threshold (0.05f) or if we're very close, snap to position
        if (newDistance <= 0.05f || newDistance >= distanceToTarget) // If we're very close or not getting closer
        {
            needsPrecisePositioning = false;
            transform.position = precisePosition.position;
            transform.rotation = precisePosition.rotation;
            
            Debug.Log($"[{GetType().Name}] {npc.name} Precise positioning complete - final position: {transform.position}");
            return true; // Positioning complete
        }
        
        return false; // Still positioning
    }

    /// <summary>
    /// Shared method to update movement animations
    /// </summary>
    protected void UpdateMovementAnimation()
    {
        if (agent == null || animator == null) return;
        
        float maxSpeed = MaxSpeed();
        float currentSpeedNormalized = agent.velocity.magnitude / maxSpeed;
        animator.SetFloat("Speed", currentSpeedNormalized);
    }

    /// <summary>
    /// Shared method to setup NavMesh agent for WorkTask navigation
    /// Used by both WorkState and SleepState for consistent navigation behavior
    /// </summary>
    /// <param name="destination">The destination transform</param>
    /// <param name="obstacleBoundsOffset">Offset for obstacle bounds calculation</param>
    protected void SetupNavMeshForWorkTask(Transform destination, float obstacleBoundsOffset = 0.5f)
    {
        if (agent == null || destination == null) return;

        agent.stoppingDistance = GetEffectiveStoppingDistance(destination, obstacleBoundsOffset);
        agent.SetDestination(destination.position);
        agent.speed = MaxSpeed();
        agent.angularSpeed = npc.rotationSpeed;
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
    }

    /// <summary>
    /// Shared method to handle reaching a destination with precise positioning setup
    /// Returns true if the state has just reached the destination (first time)
    /// </summary>
    /// <param name="isAtDestination">Reference to the "at destination" flag</param>
    /// <param name="needsPrecisePositioning">Reference to the precise positioning flag</param>
    /// <param name="precisePosition">The precise position transform (can be null)</param>
    /// <returns>True if just reached destination (first time), false if already there</returns>
    protected bool HandleReachedDestination(ref bool isAtDestination, ref bool needsPrecisePositioning, Transform precisePosition)
    {
        if (!isAtDestination)
        {
            isAtDestination = true;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            // Check if we need precise positioning
            if (precisePosition != null)
            {
                needsPrecisePositioning = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
            }
            else
            {
                needsPrecisePositioning = false;
            }
            
            return true; // Just reached destination
        }
        
        return false; // Already at destination
    }

    /// <summary>
    /// Shared method to handle moving away from destination
    /// </summary>
    /// <param name="isAtDestination">Reference to the "at destination" flag</param>
    /// <param name="needsPrecisePositioning">Reference to the precise positioning flag</param>
    protected void HandleMovingFromDestination(ref bool isAtDestination, ref bool needsPrecisePositioning)
    {
        if (isAtDestination)
        {
            isAtDestination = false;
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
            needsPrecisePositioning = false;
        }
    }
    
    /// <summary>
    /// Retry work assignment after a short delay
    /// </summary>
    private IEnumerator RetryWorkAssignment()
    {
        // Wait a short time for new tasks to potentially be added to the queue
        yield return new WaitForSeconds(0.1f);
        
        if (CampManager.Instance?.WorkManager != null)
        {
            bool taskAssigned = CampManager.Instance.WorkManager.AssignNextAvailableTask(npc);
            Debug.Log($"[TaskState] Retry AssignNextAvailableTask result for {npc.name}: {taskAssigned}");
            
            if (!taskAssigned)
            {
                Debug.Log($"[TaskState] No task assigned after retry, changing {npc.name} to WANDER");
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
