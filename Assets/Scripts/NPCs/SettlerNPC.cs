using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum TaskType
{
    NONE,
    MAKE_AMMO,
    GENERATE_ELECTRICITY,
    TEND_CROPS,
    BUILD_STRUCTURE,
    WANDER
}

public class SettlerNPC : HumanCharacterController
{
    public NavMeshAgent agent;
    public float wanderRadius = 10f;
    public float wanderIntervalMin = 5f;
    public float wanderIntervalMax = 10f;

    public TaskType currentTask = TaskType.WANDER; // Start with the wander task
    private bool isWandering = false; // Track if the wandering coroutine is running

    private void Start()
    {
        // Start wandering only if the task is WANDER
        if (currentTask == TaskType.WANDER)
            StartWandering();
    }

    private void Update()
    {
        // Update the animator's speed based on the agent's velocity
        animator.SetFloat("Speed", agent.velocity.magnitude / 3.5f);

        // If the task changes to something else, stop wandering
        if (currentTask != TaskType.WANDER)
        {
            if (isWandering)
            {
                StopWandering();
            }
            agent.ResetPath(); // Stop the agent from moving
            agent.speed = moveMaxSpeed; // Restore the original speed
            agent.angularSpeed = rotationSpeed; // Restore original rotation speed
        }
        else if (currentTask == TaskType.WANDER && !isWandering)
        {
            StartWandering(); // Start wandering if it's not already running
        }
    }

    private void StartWandering()
    {
        // Make sure the coroutine only starts once
        if (!isWandering)
        {
            isWandering = true;
            agent.speed = moveMaxSpeed * 0.5f; // Reduce speed by half while wandering
            agent.angularSpeed = rotationSpeed / 2f; // Reduce rotation speed by half while wandering
            StartCoroutine(WanderCoroutine());
        }
    }

    private void StopWandering()
    {
        // Stop the coroutine if it's already running
        if (isWandering)
        {
            isWandering = false;
            StopCoroutine(WanderCoroutine());
            agent.speed = moveMaxSpeed; // Restore the original speed after stopping wandering
            agent.angularSpeed = rotationSpeed; // Restore original rotation speed
        }
    }

    private IEnumerator WanderCoroutine()
    {
        while (currentTask == TaskType.WANDER)
        {
            // Wait for a random amount of time between wander intervals
            float waitTime = Random.Range(wanderIntervalMin, wanderIntervalMax);
            yield return new WaitForSeconds(waitTime);

            // Only set a new wander point if the agent has finished its current path
            if (!agent.pathPending && agent.remainingDistance <= 0.5f)
            {
                SetNewWanderPoint();
            }
        }
    }

    private void SetNewWanderPoint()
    {
        // Pick a random point within the specified radius
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            // Set the new destination for the agent
            agent.SetDestination(hit.position);
        }
    }

    // Method to change tasks from outside
    public void ChangeTask(TaskType newTask)
    {
        currentTask = newTask;

        // If the new task is not wandering, stop wandering
        if (newTask != TaskType.WANDER)
        {
            StopWandering();
            agent.ResetPath();
        }
        // If the new task is wandering, start wandering again
        else
        {
            StartWandering();
        }
    }
}
