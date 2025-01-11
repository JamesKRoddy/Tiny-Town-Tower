using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour, IDamageable
{
    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform navMeshTarget;

    public float Health { get; set; }
    public float MaxHealth { get; set; }

    public event Action OnEnemyKilled;

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
            MaxHealth = 100f;

        Health = MaxHealth;
    }

    internal void Setup(Transform navAgentTarget)
    {
        navMeshTarget = navAgentTarget;
    }

    public void Heal(float amount)
    {
        Health = Mathf.Min(Health + amount, MaxHealth);
    }

    public void TakeDamage(float amount)
    {        
        Health -= amount;
        animator.SetTrigger("Damaged");
        if (Health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        OnEnemyKilled?.Invoke();
        animator.SetTrigger("Dead");
    }
}
