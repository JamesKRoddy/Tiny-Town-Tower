using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class WanderState : _TaskState
{
    private bool isWandering = false;
    private float wanderIntervalMin = 5f;
    private float wanderIntervalMax = 10f;
    private Coroutine wanderCoroutine;


    public float wanderRadius = 10f; // Allow wander radius to be modified in the Inspector

    private void Awake()
    {
        // Ensure NPC reference is set when the state starts
        if (npc == null)
        {
            SetNPCReference(GetComponent<SettlerNPC>());
        }

        agent = npc.GetAgent(); // Store reference to NavMeshAgent
    }

    private void OnDisable() //TODO do this for all states
    {
        OnExitState();
    }

    public override TaskType GetTaskType()
    {
        return TaskType.WANDER; // Return the task type associated with this state
    }

    public override void OnEnterState()
    {
        if (agent == null)
        {
            agent = npc.GetAgent(); // Store reference to NavMeshAgent
        }

        if (!isWandering)
        {
            isWandering = true;
            agent.speed = MaxSpeed(); // Reduce speed for wandering
            agent.angularSpeed = npc.rotationSpeed / 2f; // Reduce rotation speed for wandering
            wanderCoroutine = npc.StartCoroutine(WanderCoroutine()); // Start the wandering coroutine
            WorkManager.Instance.OnTaskAssigned += WorkAvalible;
        }
    }

    public override void OnExitState()
    {
        if (isWandering)
        {
            isWandering = false;
            npc.StopCoroutine(wanderCoroutine); // Stop the wandering coroutine
            agent.speed = npc.moveMaxSpeed; // Reset speed
            agent.angularSpeed = npc.rotationSpeed; // Reset rotation speed

            if(WorkManager.Instance)
                WorkManager.Instance.OnTaskAssigned -= WorkAvalible;
        }
    }

    public override void UpdateState()
    {
        
    }

    private void WorkAvalible(WorkTask newTask)
    {
        npc.AssignWork(newTask);
    }

    private IEnumerator WanderCoroutine()
    {
        while (isWandering)
        {
            // Pick a new wander point
            SetNewWanderPoint();

            // Wait for the NPC to reach the point
            while (!agent.pathPending && agent.remainingDistance > 0.5f)
            {
                yield return null; // Wait until the NPC reaches the destination
            }

            // Once reached, wait for a random duration between wanderIntervalMin and wanderIntervalMax
            float waitTime = Random.Range(wanderIntervalMin, wanderIntervalMax);
            yield return new WaitForSeconds(waitTime); // Wait at the destination

            // After waiting, pick a new wander point
        }
    }

    private void SetNewWanderPoint()
    {
        // Pick a random point within the wander radius
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += npc.transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position); // Move towards the new point
        }
    }

    public override float MaxSpeed()
    {
        return npc.moveMaxSpeed * 0.2f; // Reduce speed for wandering
    }
}
