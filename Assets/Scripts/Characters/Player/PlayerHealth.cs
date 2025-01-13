using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float health { get; set; }
    public float maxHealth { get; set; }

    public void Heal(float healAmount)
    {
        health += healAmount;
        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        health -= damageAmount;
        if (health <= 0)
        {
            health = 0;
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
