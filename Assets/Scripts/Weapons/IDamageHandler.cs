

using UnityEngine;

public interface IDamageable
{
    [SerializeField] public float Health { get; set; } // Property for current health
    [SerializeField] public float MaxHealth { get; set; } // Property for max health

    void TakeDamage(float amount); // Method to handle damage
    void Heal(float amount);       // Optional: Method to handle healing
    void Die();
}
