using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class TurretBaseTarget : MonoBehaviour, IDamageable
{
    [Header("Base Settings")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;

    public float Health
    {
        get => health;
        set => health = Mathf.Clamp(value, 0, maxHealth);
    }

    public float MaxHealth
    {
        get => maxHealth;
        set => maxHealth = value;
    }

    public delegate void BaseDestroyedHandler();
    public event BaseDestroyedHandler OnBaseDestroyed;
    public event Action<float, float> OnDamageTaken;
    public event Action<float, float> OnHeal;
    public event Action OnDeath;

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
            enemy.Die();
        }
    }

    private void HandleBaseDestroyed()
    {
        Debug.Log("Turret Base has been destroyed!");
        OnBaseDestroyed?.Invoke();
        // Add logic for game over, animations, etc.
    }
    
    public Allegiance GetAllegiance() => Allegiance.FRIENDLY;

    public void TakeDamage(float amount, Transform damageSource = null)
    {
        float previousHealth = Health;
        Health -= amount;
        OnDamageTaken?.Invoke(amount, Health);
        if (Health <= 0)
        {
            Health = 0;
            HandleBaseDestroyed();
        }
    }

    public void Heal(float amount)
    {
        Health += amount;
        OnHeal?.Invoke(amount, Health);
    }

    public void Die()
    {
        OnDeath?.Invoke();
    }

    // Draws a blue gizmo matching the BoxCollider in the Scene view.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
            {
                // Save the current Gizmos matrix.
                Matrix4x4 oldMatrix = Gizmos.matrix;
                // Apply the transform matrix to match the object's position, rotation, and scale.
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                // Draw the box collider using its center and size.
                Gizmos.DrawWireCube(box.center, box.size);
                // Restore the original Gizmos matrix.
                Gizmos.matrix = oldMatrix;
            }
            else
            {
                // For non-box colliders, fallback to drawing their bounds.
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
