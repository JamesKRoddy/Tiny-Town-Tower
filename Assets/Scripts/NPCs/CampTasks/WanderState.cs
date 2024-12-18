using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class WanderState : _TaskState
{
    private bool isWandering = false;
    private float wanderIntervalMin = 5f;
    private float wanderIntervalMax = 10f;

    public float wanderRadius = 10f; // Allow wander radius to be modified in the Inspector

    private NavMeshAgent agent;

    private void Start()
    {
        // Ensure NPC reference is set when the state starts
        if (npc == null)
        {
            SetNPCReference(GetComponent<SettlerNPC>());
        }

        agent = npc.GetAgent(); // Store reference to NavMeshAgent
    }

    public override TaskType GetTaskType()
    {
        return TaskType.WANDER; // Return the task type associated with this state
    }

    public override void OnEnterState()
    {
        if (!isWandering)
        {
            isWandering = true;
            agent.speed = MaxSpeed(); // Reduce speed for wandering
            agent.angularSpeed = npc.rotationSpeed / 2f; // Reduce rotation speed for wandering
            npc.StartCoroutine(WanderCoroutine()); // Start the wandering coroutine
        }
    }

    public override void OnExitState()
    {
        if (isWandering)
        {
            isWandering = false;
            npc.StopCoroutine(WanderCoroutine()); // Stop the wandering coroutine
            agent.speed = npc.moveMaxSpeed; // Reset speed
            agent.angularSpeed = npc.rotationSpeed; // Reset rotation speed
        }
    }

    public override void UpdateState()
    {
        
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
