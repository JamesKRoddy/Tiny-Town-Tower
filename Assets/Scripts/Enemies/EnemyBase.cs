using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyBase : MonoBehaviour, IDamageable
{
    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform navMeshTarget;

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

    protected float damage;
    public event Action<float, float> OnDamageTaken;
    public event Action<float, float> OnHeal;
    public event Action OnDeath;

    protected virtual void Awake()
    {
        // Initialize the NavMeshAgent and Animator
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Find the player in the scene
    }

    private void Start()
    {
        if(MaxHealth == 0)
            MaxHealth = 10f;

        Health = MaxHealth;
    }

    internal void Setup(Transform navAgentTarget)
    {
        navMeshTarget = navAgentTarget;
    }

    public void SetEnemyDestination(Vector3 navMeshTarget)
    {
        agent.SetDestination(navMeshTarget);
    }

    public void Heal(float amount)
    {
        Health = Mathf.Min(Health + amount, MaxHealth);
    }

    public Allegiance GetAllegiance() => Allegiance.HOSTILE;

    public void TakeDamage(float amount, Transform damageSource = null)
    {
        Debug.Log("Enemy took damage: " + amount);
        float previousHealth = Health;
        Health -= amount;
        animator.SetTrigger("Damaged");
        OnDamageTaken?.Invoke(amount, Health);

        // Rotate towards the damage source if one is provided
        if (damageSource != null)
        {
            Vector3 direction = (damageSource.position - transform.position).normalized;
            direction.y = 0; // Keep the rotation on the horizontal plane
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.5f);

                // Add knockback effect
                Vector3 knockbackDirection = -direction; // Push away from damage source
                float knockbackDistance = 1.5f; // Adjust this value to control knockback distance
                Vector3 newPosition = transform.position + knockbackDirection * knockbackDistance;

                // Sample the NavMesh to ensure the new position is valid
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, knockbackDistance, NavMesh.AllAreas))
                {
                    StartCoroutine(KnockbackRoutine(hit.position));
                }
            }
        }

        if (Health <= 0)
        {
            Die();
        }
    }

    private IEnumerator KnockbackRoutine(Vector3 targetPosition)
    {
        float duration = 0.2f; // Duration of the knockback
        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            
            // Sample NavMesh to ensure we stay on it
            if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 0.1f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            
            yield return null;
        }
    }

    public void Die()
    {
        OnDeath?.Invoke();
        animator.SetTrigger("Dead");
        agent.enabled = false;
        Destroy(gameObject, 10f);
    }

    internal float GetDamageValue()
    {
        return damage;
    }
}
