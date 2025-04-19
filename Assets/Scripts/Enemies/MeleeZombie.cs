using UnityEngine;

namespace Enemies
{
    public class MeleeZombie : Zombie
{
    public float meleeDamage = 2f;

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
}
