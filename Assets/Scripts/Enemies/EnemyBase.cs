using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour, IDamageable
{
    protected NavMeshAgent agent;
    protected Animator animator;
    protected Transform navMeshTarget;

    [SerializeField] public float Health { get; set; }
    [SerializeField] public float MaxHealth { get; set; }
    [SerializeField] public float damage;

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
            MaxHealth = 10f;

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
        Destroy(gameObject); //TODO testing purpose
    }

    internal float GetDamageValue()
    {
        return damage;
    }
}
