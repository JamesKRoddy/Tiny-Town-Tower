using UnityEngine;
using System.Collections.Generic;
using Combat.Attacks;

namespace Weapons
{
    /// <summary>
    /// Attach this to a weapon model's collider to enable accurate hit detection.
    /// The collider should be slightly larger than the weapon mesh and set as a trigger.
    /// 
    /// SETUP (Manual - Recommended):
    /// 1. Add a trigger collider to your weapon prefab (Capsule/Box Collider with "Is Trigger" enabled)
    /// 2. Add this component to the same GameObject as the collider
    /// 3. The collider should be on a child of the weapon holder (so it moves with animations)
    /// 4. Make sure the weapon is on an appropriate layer (e.g., "Weapon" layer)
    /// 
    /// AUTOMATIC SETUP:
    /// If not present, WeaponAttack will automatically create a basic collider and add this component.
    /// However, manually setting up the collider allows you to fine-tune its shape and size.
    /// 
    /// USAGE:
    /// WeaponAttack will automatically find this component and enable/disable it during attacks.
    /// When enabled, it will detect hits via OnTriggerEnter and report them back to WeaponAttack.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponCollider : MonoBehaviour
    {
    [Header("Debug")]
    [Tooltip("Show debug logs for hit detection")]
    [SerializeField] private bool debugMode = false;
    
    [Tooltip("Show gizmo visualization of the collider")]
    [SerializeField] private bool showGizmo = true;
    
    // References
    private Collider weaponCollider;
    private HashSet<Collider> hitTargetsThisSwing = new HashSet<Collider>();
    private WeaponAttack weaponAttack;
    
    // State
    private bool isActive = false;
    
    #region Initialization
    
    private void Awake()
    {
        weaponCollider = GetComponent<Collider>();
        
        if (weaponCollider == null)
        {
            Debug.LogError($"[WeaponCollider] No collider found on {gameObject.name}!");
            return;
        }
        
        if (!weaponCollider.isTrigger)
        {
            Debug.LogWarning($"[WeaponCollider] Collider on {gameObject.name} is not set as trigger! Setting it now.");
            weaponCollider.isTrigger = true;
        }
        
        // Start disabled
        weaponCollider.enabled = false;
    }
    
    /// <summary>
    /// Initialize with the parent WeaponAttack component
    /// </summary>
    public void Initialize(WeaponAttack attack)
    {
        weaponAttack = attack;
        
        if (debugMode)
        {
            Debug.Log($"[WeaponCollider] Initialized on {gameObject.name} for weapon attack: {weaponAttack.name}");
        }
    }
    
    #endregion
    
    #region Hit Detection
    
    /// <summary>
    /// Enable the weapon collider for hit detection
    /// Called by WeaponAttack when attack starts
    /// </summary>
    public void EnableCollider()
    {
        if (weaponCollider == null) return;
        
        hitTargetsThisSwing.Clear();
        weaponCollider.enabled = true;
        isActive = true;
        
        if (debugMode)
        {
            Debug.Log($"[WeaponCollider] Collider ENABLED on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Disable the weapon collider
    /// Called by WeaponAttack when attack ends
    /// </summary>
    public void DisableCollider()
    {
        if (weaponCollider == null) return;
        
        weaponCollider.enabled = false;
        isActive = false;
        hitTargetsThisSwing.Clear();
        
        if (debugMode)
        {
            Debug.Log($"[WeaponCollider] Collider DISABLED on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Called when the weapon collider hits something
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || weaponAttack == null) return;
        
        // Prevent hitting the same target multiple times in one swing
        if (hitTargetsThisSwing.Contains(other))
        {
            if (debugMode)
            {
                Debug.Log($"[WeaponCollider] Already hit {other.gameObject.name} this swing, skipping");
            }
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"[WeaponCollider] Hit detected: {other.gameObject.name} on layer {LayerMask.LayerToName(other.gameObject.layer)}");
        }
        
        // Check for damageable targets
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // Check if this is a valid target (allegiance check)
            if (DamageUtils.IsValidTarget(damageable, weaponAttack))
            {
                // Deal damage using the weapon attack
                DamageUtils.DealDamage(weaponAttack, damageable);
                
                // Track this hit
                hitTargetsThisSwing.Add(other);
                
                if (debugMode)
                {
                    Debug.Log($"[WeaponCollider] ✅ Dealt damage to {other.gameObject.name}");
                }
                
                return;
            }
            else if (debugMode)
            {
                Debug.Log($"[WeaponCollider] Invalid target (allegiance): {other.gameObject.name}");
            }
        }
        
        // Check for hittable objects (projectiles, props, etc.)
        IHittable hittable = other.GetComponent<IHittable>();
        if (hittable != null && hittable.CanBeHit())
        {
            // Calculate hit info
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (hitPoint - transform.position).normalized;
            
            var hitInfo = new HitInfo(hitPoint, hitNormal, weaponAttack.DamageSource, weaponAttack.BaseDamage, weaponAttack);
            hittable.OnHit(hitInfo);
            
            // Track this hit
            hitTargetsThisSwing.Add(other);
            
            if (debugMode)
            {
                Debug.Log($"[WeaponCollider] ✅ Hit IHittable: {other.gameObject.name}");
            }
        }
    }
    
    /// <summary>
    /// Get the current hit targets for this swing (for external tracking)
    /// </summary>
    public HashSet<Collider> GetHitTargets()
    {
        return hitTargetsThisSwing;
    }
    
    #endregion
    
    #region Debug Visualization
    
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        Collider col = weaponCollider != null ? weaponCollider : GetComponent<Collider>();
        if (col == null) return;
        
        // Color based on state
        Gizmos.color = isActive ? Color.red : Color.gray;
        
        // Draw collider bounds
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
        }
        else if (col is CapsuleCollider capsule)
        {
            // Simple capsule visualization
            Gizmos.DrawWireSphere(transform.position + capsule.center, capsule.radius);
        }
    }
    
    #endregion
    }
}

