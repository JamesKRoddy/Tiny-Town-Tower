using UnityEngine;

public class TurretBaseTarget : MonoBehaviour, IDamageable
{
    [Header("Base Settings")]
    public float Health { get ; set; }
    public float MaxHealth { get; set; }

    public delegate void BaseDestroyedHandler();
    public event BaseDestroyedHandler OnBaseDestroyed;

    private void Start()
    {
        Health = MaxHealth;
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            TakeDamage(enemy.GetDamageValue());
            Destroy(enemy.gameObject);
        }
    }

    private void TakeDamage(float damage)
    {
        Health -= damage;
        if (Health <= 0)
        {
            Health = 0;
            HandleBaseDestroyed();
        }
    }

    private void HandleBaseDestroyed()
    {
        Debug.Log("Turret Base has been destroyed!");
        OnBaseDestroyed?.Invoke();
        // Add logic for game over, animations, etc.
    }

    void IDamageable.TakeDamage(float amount)
    {
        throw new System.NotImplementedException();
    }

    public void Heal(float amount)
    {
        throw new System.NotImplementedException();
    }

    public void Die()
    {
        throw new System.NotImplementedException();
    }
}
