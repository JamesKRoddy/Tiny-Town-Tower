

public interface IDamageable
{
    float health { get; set; } // Property for current health
    float maxHealth { get; set; } // Property for max health

    void TakeDamage(float amount); // Method to handle damage
    void Heal(float amount);       // Optional: Method to handle healing
    void Die();
}
