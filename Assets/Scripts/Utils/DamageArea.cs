using UnityEngine;
using System.Collections;

/// <summary>
/// Generic damage area component that can be used for persistent damage zones
/// </summary>
public class PersistentDamageArea : MonoBehaviour
{
    [Header("Damage Settings")]
    public float radius = 2f;
    public float damagePerTick = 10f;
    public float poiseDamagePerTick = 5f;
    public float damageInterval = 0.5f;
    public float duration = 5f;
    public AttackElement element = AttackElement.PHYSICAL;
    public LayerMask targetLayers = -1;

    private Transform attacker;
    private float startTime;
    private Coroutine damageCoroutine;

    public void Setup(float areaRadius, float damage, float poiseDamage, Transform attackTransform, 
        AttackElement elem, float areaDuration = 5f, float interval = 0.5f)
    {
        radius = areaRadius;
        damagePerTick = damage;
        poiseDamagePerTick = poiseDamage;
        attacker = attackTransform;
        element = elem;
        duration = areaDuration;
        damageInterval = interval;
        startTime = Time.time;
        
        // Start dealing damage
        damageCoroutine = StartCoroutine(DealDamageOverTime());
        
        // Destroy after duration
        Destroy(gameObject, duration);
    }

    private IEnumerator DealDamageOverTime()
    {
        while (Time.time - startTime < duration)
        {
            // Deal damage to all targets in radius
            DamageUtils.DealDamageInRadius(transform.position, radius, damagePerTick, poiseDamagePerTick, 
                attacker, element, targetLayers);
            
            yield return new WaitForSeconds(damageInterval);
        }
    }

    private void OnDestroy()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
