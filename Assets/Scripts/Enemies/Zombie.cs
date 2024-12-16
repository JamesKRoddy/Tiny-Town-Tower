using UnityEngine;
using UnityEngine.AI;

public class Zombie : EnemyBase
{
    public float attackRange = 2f;
    public float damage = 10f;
    protected bool isAttacking = false;

    protected override void Awake()
    {
        base.Awake();

        agent.updatePosition = false;
    }

    protected virtual void Update()
    {
        MoveTowardsPlayer();

        if (Vector3.Distance(transform.position, player.position) <= attackRange && !isAttacking)
        {
            StartAttack();
        }
        else
        {
            EndAttack();
        }
    }

    // This method is called by the Animator when root motion is being applied
    void OnAnimatorMove()
    {
        // Use root motion to move the zombie instead of the NavMeshAgent's movement
        if (agent.isOnNavMesh)
        {
            // If you want to keep NavMeshAgent's pathfinding active but control movement through root motion:
            Vector3 rootMotion = animator.deltaPosition; // Get the root motion delta (movement from animation)
            rootMotion.y = 0; // We don’t want to apply any vertical movement (gravity, etc.)

            // Move the zombie using root motion
            transform.position += rootMotion;

            // Update agent's position to match with root motion (so the pathfinding remains active)
            agent.nextPosition = transform.position;
        }
    }

    protected virtual void MoveTowardsPlayer()
    {
        if (!isAttacking)
        {
            // The NavMeshAgent moves the zombie, but root motion from animation drives actual movement
            agent.SetDestination(player.position);
        }
    }

    protected virtual void StartAttack()
    {
        // Trigger attack animation, this should transition to attack animations via root motion
        animator.SetTrigger("Attack");
        isAttacking = true;
    }

    public void EndAttack()
    {
        // Reset isAttacking flag after the attack animation finishes
        animator.SetBool("Attack", false);
        isAttacking = false;
    }
}
