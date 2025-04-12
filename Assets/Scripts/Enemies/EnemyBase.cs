using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Managers;

public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Character Type")]
    [SerializeField] protected CharacterType characterType = CharacterType.ZOMBIE_MELEE;

    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform navMeshTarget;
    protected bool isAttacking = false;

    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;

    // Material flash effect
    protected SkinnedMeshRenderer skinnedMeshRenderer;
    protected Material originalMaterial;
    protected Material flashMaterial;
    protected float flashDuration = 0.5f;
    protected Color flashColor = new Color(1f, 0.1f, 0.1f); // Bright red color for flash

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

    public CharacterType CharacterType => characterType;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // Get the SkinnedMeshRenderer and store original material
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null)
        {
            originalMaterial = skinnedMeshRenderer.material;
            // Create a new instance of the material for flashing
            flashMaterial = new Material(originalMaterial);
            flashMaterial.color = flashColor;
        }
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
    protected virtual void StartAttack()
    {
        // Trigger attack animation, this should transition to attack animations via root motion
        animator.SetTrigger("Attack");
        isAttacking = true;
    }

    protected virtual void EndAttack()
    {
        // Reset isAttacking flag after the attack animation finishes
        animator.SetBool("Attack", false);
        isAttacking = false;
    }

    public Allegiance GetAllegiance() => Allegiance.HOSTILE;

    public void TakeDamage(float amount, Transform damageSource = null)
    {
        Debug.Log("Enemy took damage: " + amount);
        float previousHealth = Health;
        Health -= amount;

        // Interrupt attack animation and play damage animation
        if (animator != null)
        {
            // Reset attack trigger and return to default state
            animator.ResetTrigger("Attack");
            animator.Play("Default", 1, 0); // Play the default animation on attack layer
            // Play damage animation
            animator.SetTrigger("Damaged");
        }

        OnDamageTaken?.Invoke(amount, Health);

        // Play hit VFX
        if (damageSource != null)
        {
            Vector3 hitPoint = transform.position + Vector3.up * 1.5f;
            Vector3 hitNormal = (transform.position - damageSource.position).normalized;
            EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, this);
            
            // Handle knockback and rotation
            HandleDamageReaction(damageSource);
        }

        if (Health <= 0)
        {
            Die();
        }
    }

    protected virtual void HandleDamageReaction(Transform damageSource)
    {
        if (!isAttacking)
        {
            Vector3 direction = (damageSource.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.5f);

                // Add knockback effect
                Vector3 knockbackDirection = -direction;
                float maxKnockbackDistance = 1.0f;
                float distanceFromSource = Vector3.Distance(transform.position, damageSource.position);
                float knockbackDistance = Mathf.Lerp(maxKnockbackDistance, maxKnockbackDistance * 0.3f, distanceFromSource / 5f);
                Vector3 newPosition = transform.position + knockbackDirection * knockbackDistance;

                if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, knockbackDistance, NavMesh.AllAreas))
                {
                    StartCoroutine(KnockbackRoutine(hit.position));
                }
            }
        }
    }

    private IEnumerator KnockbackRoutine(Vector3 targetPosition)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            
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
        if (animator != null)
        {
            animator.SetTrigger("Dead");
        }
        agent.enabled = false;
        GetComponent<Collider>().enabled = false;

        // Play death VFX
        Vector3 deathPoint = transform.position + Vector3.up * 1.5f;
        Vector3 deathNormal = Vector3.up;
        EffectManager.Instance.PlayDeathEffect(deathPoint, deathNormal, this);

        Destroy(gameObject, 10f);
    }

    internal float GetDamageValue()
    {
        return damage;
    }

    // Animation event function that can be called from the animator
    public void AttackWarning()
    {
        if (skinnedMeshRenderer != null)
        {
            StartCoroutine(FlashMaterialCoroutine());
        }
    }

    private IEnumerator FlashMaterialCoroutine()
    {
        // Apply flash material
        skinnedMeshRenderer.material = flashMaterial;
        
        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);
        
        // Restore original material
        skinnedMeshRenderer.material = originalMaterial;
    }
}
