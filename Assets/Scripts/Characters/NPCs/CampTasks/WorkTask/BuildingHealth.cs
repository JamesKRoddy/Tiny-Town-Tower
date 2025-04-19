using UnityEngine;

public class BuildingHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    public float Health => currentHealth;
    public float MaxHealth => maxHealth;

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth <= 0)
        {
            OnBuildingDestroyed();
        }
    }

    public void RestoreHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    private void OnBuildingDestroyed()
    {
        // TODO: Implement building destruction effects and cleanup
        Debug.Log("Building destroyed!");
        Destroy(gameObject);
    }
} 