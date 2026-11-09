using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for all damage areas - handles trigger-based damage to any IDamageable
/// Can be used for environmental hazards, enemy attacks, or any damage zone
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DamageArea : MonoBehaviour, IDamageDealer
{
    [Header("Damage Settings")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float poiseDamage = 5f;
    [SerializeField] protected float damageInterval = 1f; // Time between damage applications
    
    [Header("Elemental Settings")]
    [SerializeField] protected AttackElement elementType = AttackElement.NONE;
    [SerializeField] protected int elementalDamageBonus = 0;
    
    [Header("Source Settings")]
    [SerializeField] protected Transform damageSource; // Optional damage source (defaults to this transform)
    [Tooltip("Allegiance of this damage dealer:\n" +
        "- HOSTILE: Damages FRIENDLY targets (enemy-created areas like vomit pools)\n" +
        "- FRIENDLY: Damages HOSTILE targets (player-created areas like grenades)\n" +
        "- NEUTRAL: Damages ALL targets except NEUTRAL (environmental hazards like lava)")]
    [SerializeField] protected Allegiance dealerAllegiance = Allegiance.NEUTRAL;
    
    protected Dictionary<IDamageable, float> lastDamageTimes = new Dictionary<IDamageable, float>(); // Track damage times per IDamageable
    
    // IDamageDealer implementation
    public float BaseDamage => damage;
    public float PoiseDamage => poiseDamage;
    public AttackElement ElementType => elementType;
    public int ElementalDamageBonus => elementalDamageBonus;
    public Transform DamageSource => damageSource != null ? damageSource : transform;
    public Allegiance DealerAllegiance => dealerAllegiance;

    protected virtual void Awake()
    {
        // Ensure collider and rigidbody are properly configured for trigger-based detection
        // This prevents NavMeshAgents from treating this as a physical obstacle
        ConfigureColliderAndRigidbody();
    }

    /// <summary>
    /// Ensures the collider and rigidbody are properly configured as triggers
    /// This allows enemies/NPCs to walk through damage areas like vomit pools
    /// Uses Water layer so footstep system can detect these surfaces for VFX
    /// </summary>
    private void ConfigureColliderAndRigidbody()
    {
        // Set the GameObject layer to "Water" so footstep raycasts can detect it
        // NPCs should have Water layer excluded from their obstacleLayerMasks to walk through
        // This allows footstep system to detect poison/water surfaces while NPCs walk through them
        if (gameObject.layer != GameConstants.Layers.WaterLayer)
        {
            gameObject.layer = GameConstants.Layers.WaterLayer;
            Debug.Log($"[DamageArea] Set layer to '{GameConstants.Layers.Water}' on {gameObject.name}");
        }
        
        // Get all colliders on this GameObject (there might be multiple)
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log($"[DamageArea] Set collider to trigger on {gameObject.name}");
            }
            
            // For mesh colliders, ensure they're convex (required for triggers with kinematic rigidbodies)
            MeshCollider meshCol = col as MeshCollider;
            if (meshCol != null && !meshCol.convex)
            {
                meshCol.convex = true;
                Debug.Log($"[DamageArea] Set mesh collider to convex on {gameObject.name}");
            }
        }
        
        // Ensure rigidbody is kinematic and has no gravity
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.isKinematic = true;
                Debug.Log($"[DamageArea] Set rigidbody to kinematic on {gameObject.name}");
            }
            if (rb.useGravity)
            {
                rb.useGravity = false;
                Debug.Log($"[DamageArea] Disabled gravity on {gameObject.name}");
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // Check if we have a record for this damageable
            if (!lastDamageTimes.ContainsKey(damageable))
            {
                lastDamageTimes[damageable] = 0f; // Initialize with 0 so it can take damage immediately
            }
            
            // Only apply damage if enough time has passed since last damage for this specific damageable
            if (Time.time >= lastDamageTimes[damageable] + damageInterval)
            {
                // Use the unified damage system
                DealDamage(damageable);
                lastDamageTimes[damageable] = Time.time;
            }
        }
    }

    public void SetDamage(float dmg, float pdm)
    {
        damage = dmg;
        poiseDamage = pdm;
    }

    public void SetDamageInterval(float interval)
    {
        damageInterval = interval;
    }
    
    public void SetElementalProperties(AttackElement element, int bonus)
    {
        elementType = element;
        elementalDamageBonus = bonus;
    }
    
    public void SetDamageSource(Transform source)
    {
        damageSource = source;
    }
    
    public void SetAllegiance(Allegiance allegiance)
    {
        dealerAllegiance = allegiance;
    }
    
    // IDamageDealer interface implementation
    public virtual void DealDamage(IDamageable target)
    {
        DamageUtils.DealDamage(this, target);
    }
    
    public virtual void DealDamage(IDamageable target, float damageAmount, float poiseAmount)
    {
        DamageUtils.DealDamage(this, target, damageAmount, poiseAmount);
    }

    void OnTriggerExit(Collider other)
    {
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();
        if (damageable != null && lastDamageTimes.ContainsKey(damageable))
        {
            // Remove the damageable from our tracking when it leaves the trigger
            lastDamageTimes.Remove(damageable);
        }
    }
}

