using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Zombie : EnemyBase
{
    public float attackRange = 2f;

    protected override void Awake()
    {
        base.Awake();

        agent.updatePosition = false;
        agent.updateRotation = true; // Enable rotation by default
        
        // Ensure the enemy is on the NavMesh when spawned
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.Warp(hit.position);
        }
        else
        {
            Debug.LogWarning($"Zombie {gameObject.name} could not be placed on NavMesh at spawn position {transform.position}");
        }
    }

    protected virtual void Update()
    {
        // Don't do anything if dead or no target
        if (Health <= 0 || navMeshTarget == null)
            return;

        if (!isAttacking)
        {
            // Update the destination continuously
            agent.SetDestination(navMeshTarget.position);
        }

        if (Vector3.Distance(transform.position, navMeshTarget.position) <= attackRange && !isAttacking)
        {
            StartAttack();
        }
    }

    // This method is called by the Animator when root motion is being applied
    void OnAnimatorMove()
    {
        // Use root motion to move the zombie instead of the NavMeshAgent's movement
        if (Health > 0 && agent.isOnNavMesh)
        {
            // Get the root motion delta (movement from animation)
            Vector3 rootMotion = animator.deltaPosition;
            rootMotion.y = 0; // We don't want to apply any vertical movement (gravity, etc.)

            // Calculate the new position
            Vector3 newPosition = transform.position + rootMotion;

            // Sample the NavMesh to ensure the new position is valid
            if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
            {
                // Move the zombie using root motion
                transform.position = hit.position;
                
                // Update agent's position to match with root motion
                agent.nextPosition = hit.position;
            }
            else
            {
                // If we can't find a valid position on the NavMesh, warp the agent to the current position
                agent.Warp(transform.position);
            }
        }
    }

    protected virtual void MoveTowardsPlayer()
    {
        if (!isAttacking)
        {
            // The NavMeshAgent moves the zombie, but root motion from animation drives actual movement
            SetEnemyDestination(navMeshTarget.position);
        }
    }

    protected override void StartAttack()
    {
        base.StartAttack();
        agent.updateRotation = false; // Disable NavMeshAgent rotation during attack
    }

    protected override void EndAttack()
    {
        base.EndAttack();
        agent.updateRotation = true; // Re-enable NavMeshAgent rotation after attack
    }
}
