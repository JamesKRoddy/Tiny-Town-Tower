using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Diagnostics;
using Managers;
public class WanderState : _TaskState
{
    private bool isWandering = false;
    [SerializeField] private float wanderIntervalMin = 5f;
    [SerializeField] private float wanderIntervalMax = 10f;
    private Coroutine wanderCoroutine;
    private float destinationReachedThreshold;
    private float stuckTime = 0f;
    private float stuckThreshold = 3f;
    private Vector3 lastPosition;
    private float positionCheckInterval = 0.5f;
    private float lastPositionCheckTime;
    private bool isWaiting = false;

    [SerializeField] private float wanderRadius = 10f;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnDisable()
    {
        if (isWandering)
        {
            enabled = true;
            return;
        }
        OnExitState();
    }

    private void OnEnable()
    {
        if (isWandering)
        {
            OnEnterState();
        }
    }

    public override TaskType GetTaskType()
    {
        return TaskType.WANDER;
    }

    public override void OnEnterState()
    {
        if (!isWandering)
        {
            isWandering = true;
            enabled = true;
            agent.speed = MaxSpeed();
            agent.angularSpeed = npc.rotationSpeed / 2f;
            stoppingDistance = 0.5f;
            agent.stoppingDistance = stoppingDistance;
            destinationReachedThreshold = agent.stoppingDistance + 0.1f;
            wanderCoroutine = npc.StartCoroutine(WanderCoroutine());
            CampManager.Instance.WorkManager.OnTaskAvailable += WorkAvalible;
            lastPosition = npc.transform.position;
            lastPositionCheckTime = Time.time;
        }
    }

    public override void OnExitState()
    {
        if (isWandering)
        {
            isWandering = false;
            isWaiting = false;
            if (wanderCoroutine != null)
            {
                npc.StopCoroutine(wanderCoroutine);
                wanderCoroutine = null;
            }
            agent.speed = npc.moveMaxSpeed;
            agent.angularSpeed = npc.rotationSpeed;
            CampManager.Instance.WorkManager.OnTaskAvailable -= WorkAvalible;
        }
    }

    public override void UpdateState()
    {
        if (isWandering && !isWaiting)
        {
            float currentTime = Time.time;
            if (currentTime - lastPositionCheckTime >= positionCheckInterval)
            {
                Vector3 currentPosition = npc.transform.position;
                float distanceMoved = Vector3.Distance(currentPosition, lastPosition);
                
                if (distanceMoved < 0.01f)
                {
                    stuckTime += positionCheckInterval;
                    
                    if (stuckTime >= stuckThreshold)
                    {
                        SetNewWanderPoint();
                        stuckTime = 0f;
                    }
                }
                else
                {
                    stuckTime = 0f;
                }
                
                lastPosition = currentPosition;
                lastPositionCheckTime = currentTime;
            }
        }
    }

    private void WorkAvalible(WorkTask newTask)
    {
        npc.AssignWork(newTask);
    }

    private IEnumerator WanderCoroutine()
    {
        while (isWandering)
        {
            // Wait until we reach the destination
            while (!agent.pathPending && agent.remainingDistance > destinationReachedThreshold)
            {
                yield return null;
            }

            // We've reached the destination, start waiting
            isWaiting = true;
            float waitTime = Random.Range(wanderIntervalMin, wanderIntervalMax);
            yield return new WaitForSeconds(waitTime);
            isWaiting = false;

            // After waiting, set a new destination
            SetNewWanderPoint();
        }
    }

    private void SetNewWanderPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += npc.transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            stuckTime = 0f;
        }
    }

    public override float MaxSpeed()
    {
        return npc.moveMaxSpeed * 0.2f;
    }
}
