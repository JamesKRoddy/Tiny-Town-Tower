using System;
using UnityEngine;
using Managers;
using Enemies;

/// <summary>
/// Class for the point zombies are trying to reach.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class TurretBaseTarget : MonoBehaviour, IDamageable
{
    [Header("Base Settings")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private CharacterType characterType = CharacterType.MACHINE_TURRET_BASE_TARGET;

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

    public CharacterType CharacterType => characterType;

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

        // Play hit VFX
        if (damageSource != null)
        {
            Vector3 hitPoint = transform.position + Vector3.up * 1.5f; // Adjust height as needed
            Vector3 hitNormal = (transform.position - damageSource.position).normalized;
            EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, this);
        }

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

        // Play death VFX
        Vector3 deathPoint = transform.position + Vector3.up * 1.5f;
        Vector3 deathNormal = Vector3.up; // Default upward direction for death effects
        EffectManager.Instance.PlayDeathEffect(deathPoint, deathNormal, this);
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
