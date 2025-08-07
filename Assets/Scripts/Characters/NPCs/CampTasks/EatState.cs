using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Managers;

public class EatState : _TaskState
{
    private CanteenBuilding targetCanteen;
    private bool isEating = false;
    private float eatingDuration = 2f;
    private float minDistanceToCanteen = 1.5f;
    private Coroutine eatingCoroutine;

    protected override void Awake()
    {
        base.Awake();
    }

    public override TaskType GetTaskType()
    {
        return TaskType.EAT;
    }

    public override void OnEnterState()
    {
        if (agent == null)
        {
            agent = npc.GetAgent();
        }

        // Subscribe to food availability events
        CampManager.Instance.CookingManager.OnFoodAvailable += HandleFoodAvailable;

        // Find nearest canteen with food using shared method
        targetCanteen = FindNearestCanteen();
        if (targetCanteen != null)
        {
            // Use base class helper for stopping distance
            agent.stoppingDistance = GetEffectiveStoppingDistance(targetCanteen.GetFoodStoragePoint(), 0.5f);
            agent.SetDestination(targetCanteen.GetFoodStoragePoint().position);
            agent.speed = MaxSpeed();
            agent.angularSpeed = npc.rotationSpeed;
            agent.isStopped = false;
        }
        else
        {
            // No canteen with food available, set cooldown and return to previous state
            npc.SetNoFoodCooldown();
            if (npc.HasAssignedWork())
            {
                npc.ReturnToWork();
            }
            else
            {
                npc.ChangeTask(TaskType.WANDER);
            }
        }
    }

    public override void OnExitState()
    {
        // Unsubscribe from food availability events
        if (CampManager.Instance?.CookingManager != null)
        {
            CampManager.Instance.CookingManager.OnFoodAvailable -= HandleFoodAvailable;
        }

        if (eatingCoroutine != null)
        {
            npc.StopCoroutine(eatingCoroutine);
            eatingCoroutine = null;
        }

        isEating = false;
        targetCanteen = null;
        ResetAgentState();
    }

    private void HandleFoodAvailable(CanteenBuilding canteen)
    {
        // If we don't have a target or the new food is closer, update our target
        if (targetCanteen == null || 
            Vector3.Distance(transform.position, canteen.transform.position) < 
            Vector3.Distance(transform.position, targetCanteen.transform.position))
        {
            targetCanteen = canteen;
            
            // Use base class helper for stopping distance
            agent.stoppingDistance = GetEffectiveStoppingDistance(targetCanteen.GetFoodStoragePoint(), 0.5f);
            agent.SetDestination(targetCanteen.GetFoodStoragePoint().position);
            agent.speed = MaxSpeed();
            agent.angularSpeed = npc.rotationSpeed;
            agent.isStopped = false;
        }
    }

    public override void UpdateState()
    {
        if (targetCanteen == null) return;

        // Use base class helper for destination reached checking
        bool hasReachedCanteen = HasReachedDestination(targetCanteen.GetFoodStoragePoint(), 0.5f);
        
        if (hasReachedCanteen)
        {
            if (!isEating && targetCanteen.HasAvailableMeals())
            {
                StartEating();
            }
        }
    }

    private void StartEating()
    {
        isEating = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Face the canteen using centralized rotation utility
        NavigationUtils.RotateTowardsTarget(transform, targetCanteen.transform, npc.rotationSpeed, true);

        // Start eating animation
        if (animator != null)
        {
            animator.SetTrigger("IsEating");
        }

        eatingCoroutine = npc.StartCoroutine(EatingCoroutine());
    }

    private IEnumerator EatingCoroutine()
    {
        // Wait for eating animation
        yield return new WaitForSeconds(eatingDuration);

        // Eat the meal
        if (targetCanteen.HasAvailableMeals())
        {
            CookingRecipeScriptableObj recipe = targetCanteen.RemoveMeal();
            if (recipe != null)
            {
                npc.EatMeal(recipe);
            }
        }

        // Return to work if we were on break, otherwise check for available tasks
        if (npc.HasAssignedWork())
        {
            npc.ReturnToWork();
        }
        else
        {
            // Use shared method to assign work or wander
            TryAssignWorkOrWander();
        }
    }

    public override float MaxSpeed()
    {
        return npc.moveMaxSpeed * 0.6f; // Slightly faster than wandering but slower than work
    }
} 