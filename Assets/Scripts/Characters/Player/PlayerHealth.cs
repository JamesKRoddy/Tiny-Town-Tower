using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float Health { get; set; }
    public float MaxHealth { get; set; }

    public void Heal(float healAmount)
    {
        Health += healAmount;
        if (Health > MaxHealth)
        {
            Health = MaxHealth;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        Health -= damageAmount;
        if (Health <= 0)
        {
            Health = 0;
            Die();
        }
    }

    public void Die()
    {
        // Handle player death (e.g., show death animation, restart level, etc.)
        Debug.Log("Player has died!");
        // You can trigger a death animation or disable player controls here.
        // Optionally, restart the level or respawn the player.
    }
}
