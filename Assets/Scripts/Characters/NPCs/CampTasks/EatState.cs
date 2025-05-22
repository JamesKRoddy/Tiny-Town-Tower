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

        // Find nearest canteen with food
        targetCanteen = FindNearestCanteen();
        if (targetCanteen != null)
        {
            agent.stoppingDistance = minDistanceToCanteen;
            agent.SetDestination(targetCanteen.GetFoodStoragePoint().position);
            agent.speed = MaxSpeed();
            agent.angularSpeed = npc.rotationSpeed;
            agent.isStopped = false;
        }
        else
        {
            // No canteen with food available, return to work if we were on break
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
        if (eatingCoroutine != null)
        {
            npc.StopCoroutine(eatingCoroutine);
            eatingCoroutine = null;
        }

        isEating = false;
        targetCanteen = null;
        ResetAgentState();
    }

    public override void UpdateState()
    {
        if (targetCanteen == null) return;

        // Check if we've reached the canteen
        if (!agent.pathPending && agent.remainingDistance <= minDistanceToCanteen)
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

        // Face the canteen
        Vector3 directionToCanteen = (targetCanteen.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(directionToCanteen);

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

        // Return to work if we were on break, otherwise wander
        if (npc.HasAssignedWork())
        {
            npc.ReturnToWork();
        }
        else
        {
            npc.ChangeTask(TaskType.WANDER);
        }
    }

    private CanteenBuilding FindNearestCanteen()
    {
        CanteenBuilding nearest = null;
        float nearestDistance = float.MaxValue;

        var canteens = CampManager.Instance.CookingManager.GetRegisteredCanteens();
        foreach (var canteen in canteens)
        {
            if (canteen.HasAvailableMeals() && canteen.IsOperational())
            {
                float distance = Vector3.Distance(transform.position, canteen.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = canteen;
                }
            }
        }

        return nearest;
    }

    private void ResetAgentState()
    {
        agent.speed = MaxSpeed();
        agent.angularSpeed = npc.rotationSpeed;
        agent.isStopped = false;
        agent.stoppingDistance = stoppingDistance;
    }

    public override float MaxSpeed()
    {
        return npc.moveMaxSpeed * 0.6f; // Slightly faster than wandering but slower than work
    }
} 