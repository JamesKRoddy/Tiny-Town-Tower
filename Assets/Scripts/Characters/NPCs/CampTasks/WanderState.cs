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
    private float destinationReachedThreshold = 0.5f;
    private float stuckTime = 0f;
    private float stuckThreshold = 3f;
    private Vector3 lastPosition;
    private float positionCheckInterval = 0.5f;
    private float lastPositionCheckTime;
    private bool isWaiting = false;
    private float waitTime = 0f;
    private float waitDuration = 2f;

    [SerializeField] private float wanderRadius = 10f;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnDisable()
    {
        if (wanderCoroutine != null)
        {
            npc.StopCoroutine(wanderCoroutine);
            wanderCoroutine = null;
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
            isWaiting = false;
            agent.speed = MaxSpeed();
            agent.angularSpeed = npc.rotationSpeed / 2f;
            
            // Use base class helper for stopping distance
            agent.stoppingDistance = GetEffectiveStoppingDistance(null, 0.5f);
            
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
            if (CampManager.Instance != null)
            {
                CampManager.Instance.WorkManager.OnTaskAvailable -= WorkAvalible;
            }
        }
    }

    public override void UpdateState()
    {
        if (isWandering)
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

            // Check if we've reached our destination using base class helper
            bool hasReachedDestination = HasReachedDestination(null, 0.5f);
            
            if (hasReachedDestination)
            {
                if (!isWaiting)
                {
                    isWaiting = true;
                    waitTime = 0f;
                }
                else
                {
                    waitTime += Time.deltaTime;
                    if (waitTime >= waitDuration)
                    {
                        SetNewWanderPoint();
                        isWaiting = false;
                    }
                }
            }
            else
            {
                isWaiting = false;
            }
        }
    }

    private void SetNewWanderPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection.y = 0;
        Vector3 newPosition = npc.transform.position + randomDirection;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(newPosition, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private IEnumerator WanderCoroutine()
    {
        while (isWandering)
        {
            if (!isWaiting)
            {
                SetNewWanderPoint();
            }
            yield return new WaitForSeconds(Random.Range(wanderIntervalMin, wanderIntervalMax));
        }
    }

    private void WorkAvalible(WorkTask newTask)
    {
        // Try to assign the next available task from the work queue
        if (CampManager.Instance?.WorkManager != null)
        {
            bool taskAssigned = CampManager.Instance.WorkManager.AssignNextAvailableTask(npc);
            if (!taskAssigned)
            {
                // If no task was assigned from the queue, try to assign the specific task that triggered the event
        npc.StartWork(newTask);
            }
        }
    }

    public override float MaxSpeed()
    {
        return npc.moveMaxSpeed * 0.2f;
    }
}
