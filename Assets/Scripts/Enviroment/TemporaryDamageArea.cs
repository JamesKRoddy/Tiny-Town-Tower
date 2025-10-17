using UnityEngine;
using System.Collections;

/// <summary>
/// Temporary damage area that destroys itself after a set duration
/// Useful for spawned hazards like vomit pools, acid puddles, fire patches, etc.
/// </summary>
public class TemporaryDamageArea : DamageArea
{
    [Header("Temporary Settings")]
    [SerializeField] protected float duration = 5f;
    
    protected float spawnTime;
    protected bool isSetup = false;

    protected virtual void Start()
    {
        // If not setup via Setup() method, use Start as fallback
        if (!isSetup)
        {
            spawnTime = Time.time;
            StartCoroutine(DestroyAfterDuration());
        }
    }

    /// <summary>
    /// Setup the temporary damage area with all parameters
    /// Call this when spawning the area programmatically
    /// </summary>
    public virtual void Setup(float damage, float poiseDamage, float duration, 
        AttackElement element = AttackElement.NONE, int elementalBonus = 0, Transform source = null, 
        float interval = 1f, Allegiance allegiance = Allegiance.NEUTRAL)
    {
        SetDamage(damage, poiseDamage);
        SetElementalProperties(element, elementalBonus);
        SetDamageInterval(interval);
        SetAllegiance(allegiance);
        
        if (source != null)
        {
            SetDamageSource(source);
        }
        
        this.duration = duration;
        spawnTime = Time.time;
        isSetup = true;
        
        // Start destruction countdown
        StartCoroutine(DestroyAfterDuration());
    }

    protected virtual IEnumerator DestroyAfterDuration()
    {
        yield return new WaitForSeconds(duration);
        
        // Call cleanup before destruction
        OnBeforeDestroy();
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Called right before the area is destroyed - override for cleanup
    /// </summary>
    protected virtual void OnBeforeDestroy()
    {
        // Override in child classes for cleanup
    }
    
    /// <summary>
    /// Get remaining time before destruction
    /// </summary>
    public float GetRemainingTime()
    {
        return Mathf.Max(0, duration - (Time.time - spawnTime));
    }
    
    /// <summary>
    /// Get progress percentage (0 to 1, where 1 = about to be destroyed)
    /// </summary>
    public float GetLifetimeProgress()
    {
        return Mathf.Clamp01((Time.time - spawnTime) / duration);
    }
}

