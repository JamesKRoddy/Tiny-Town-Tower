using UnityEngine;
using Managers;

/// <summary>
/// Example script demonstrating how to use the new elemental damage system
/// This shows how weapons can now deal elemental damage with resistance calculations
/// </summary>
public class ElementalWeaponExample : MonoBehaviour
{
    [Header("Weapon Configuration")]
    [SerializeField] private AttackElement weaponElement = AttackElement.FIRE;
    [SerializeField] private float baseDamage = 25f;
    [SerializeField] private float poiseDamage = 15f;
    [SerializeField] private float attackRange = 2f;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private LayerMask targetLayers = -1;
    
    private float lastAttackTime = 0f;
    private Animator animator;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    
    /// <summary>
    /// Example method showing how to attack with elemental damage
    /// This would typically be called by an attack input or AI system
    /// </summary>
    public void PerformElementalAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
        {
            Debug.Log("Attack on cooldown");
            return;
        }
        
        lastAttackTime = Time.time;
        
        // Find targets in range
        Collider[] targets = Physics.OverlapSphere(transform.position, attackRange, targetLayers);
        
        foreach (Collider target in targets)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null && damageable != GetComponent<IDamageable>())
            {
                // Deal elemental damage with resistance calculation
                DealElementalDamage(damageable, target.transform);
            }
        }
        
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }
    
    /// <summary>
    /// Demonstrates the new elemental damage system
    /// </summary>
    /// <param name="target">The target to damage</param>
    /// <param name="targetTransform">Transform of the target</param>
    private void DealElementalDamage(IDamageable target, Transform targetTransform)
    {
        // Get target's resistance to our weapon's element
        DamageResistance resistance = target.GetResistance(weaponElement);
        float damageMultiplier = DamageUtils.GetDamageMultiplier(resistance);
        float finalDamage = baseDamage * damageMultiplier;
        
        // Log resistance information for debugging
        Debug.Log($"{targetTransform.name} resistance to {weaponElement}: {resistance} (x{damageMultiplier:F1} damage)");
        
        // Deal the elemental damage
        if (finalDamage > 0)
        {
            // Use the new elemental damage method
            target.TakeDamage(baseDamage, poiseDamage, weaponElement, transform);
            
            Debug.Log($"Dealt {finalDamage:F1} {weaponElement} damage to {targetTransform.name}");
        }
        else
        {
            Debug.Log($"{targetTransform.name} is immune to {weaponElement} damage!");
        }
    }
    
    /// <summary>
    /// Example method showing how different character types might have different resistances
    /// </summary>
    public void TestResistances()
    {
        Debug.Log("=== Elemental Damage Resistance Test ===");
        
        Collider[] targets = Physics.OverlapSphere(transform.position, attackRange * 2f, targetLayers);
        
        foreach (Collider target in targets)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Debug.Log($"\n{target.name} ({damageable.CharacterType}) resistances:");
                
                foreach (AttackElement element in System.Enum.GetValues(typeof(AttackElement)))
                {
                    if (element != AttackElement.NONE)
                    {
                        DamageResistance resistance = damageable.GetResistance(element);
                        float multiplier = DamageUtils.GetDamageMultiplier(resistance);
                        Debug.Log($"  {element}: {resistance} (x{multiplier:F1})");
                    }
                }
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw extended range for resistance testing
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange * 2f);
    }
}

