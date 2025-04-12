using UnityEngine;

public class MeleeZombie : Zombie
{
    public float meleeDamage = 2f;

    protected override void StartAttack()
    {
        base.StartAttack();
        // Additional logic for melee attack can be added here
    }

    // Called by animation event or timing logic
    public void MeleeAttack()
    {
        if (Vector3.Distance(transform.position, navMeshTarget.position) <= attackRange)
        {
            // Damage the player if in range during melee attack
            navMeshTarget.GetComponent<IDamageable>().TakeDamage(meleeDamage, transform);
            Debug.Log("Player hit by melee attack");
        }
    }
}
