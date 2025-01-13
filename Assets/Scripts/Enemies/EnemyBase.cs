using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour, IDamageable
{
    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform navMeshTarget;

    public float health { get; set; }
    public float maxHealth { get; set; }

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
        if(maxHealth == 0)
            maxHealth = 100f;

        health = maxHealth;
    }

    internal void Setup(Transform navAgentTarget)
    {
        navMeshTarget = navAgentTarget;
    }

    public void Heal(float amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
    }

    public void TakeDamage(float amount)
    {        
        health -= amount;
        animator.SetTrigger("Damaged");
        if (health <= 0)
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
