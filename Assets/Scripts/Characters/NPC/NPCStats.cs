using UnityEngine;

public class NPCStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate = 1f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaRegenRate = 5f;
    public float fatigueRate = 2f;

    private void Update()
    {
        // Regenerate health
        currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegenRate * Time.deltaTime);
        
        // Regenerate stamina
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
    }

    public void UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
    }
} 