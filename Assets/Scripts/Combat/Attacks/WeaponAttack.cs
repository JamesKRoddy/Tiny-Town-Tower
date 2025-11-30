using UnityEngine;
using System.Collections.Generic;

namespace Combat.Attacks
{
    /// <summary>
    /// Attack component that uses a WeaponScriptableObj to define weapon stats and appearance.
    /// This is the unified weapon system that replaces WeaponBase for component-based weapons.
    /// 
    /// USAGE:
    /// 1. Add WeaponAttack component to character
    /// 2. Assign a WeaponScriptableObj to define weapon stats
    /// 3. Assign weaponHolder transform (e.g., hand bone)
    /// 4. Add WeaponCollider component to weapon prefab with trigger collider
    /// 5. The weapon will handle damage via collider and projectile deflection via BoxCast
    /// 
    /// FEATURES:
    /// - Uses WeaponScriptableObj for damage, poise, element
    /// - Spawns weapon model at specified holder transform
    /// - Collider-based damage detection (accurate, follows animation)
    /// - BoxCast-based projectile deflection (forgiving, easy to hit)
    /// - Supports mutation multipliers for scaling stats
    /// - Works with any IAttackOwner (players, NPCs, enemies)
    /// 
    /// HIT DETECTION:
    /// - Damage: WeaponCollider component on weapon prefab (trigger collider)
    /// - Projectile Deflection: Simple BoxCast in front of character (more forgiving)
    /// </summary>
    public class WeaponAttack : AnimationAttack
    {
        [Header("Weapon Configuration")]
        [Tooltip("The weapon data defining stats, appearance, and element")]
        [SerializeField] private WeaponScriptableObj weaponData;
        
        [Tooltip("Transform where the weapon model should be spawned (e.g., hand bone)")]
        [SerializeField] private Transform weaponHolder;
        
        [Tooltip("Offset position for weapon model relative to holder")]
        [SerializeField] private Vector3 weaponPositionOffset = Vector3.zero;
        
        [Tooltip("Offset rotation for weapon model relative to holder")]
        [SerializeField] private Vector3 weaponRotationOffset = Vector3.zero;

        [Header("Mutation Support")]
        [Tooltip("Current damage multiplier from mutations")]
        [SerializeField, ReadOnly] private float currentDamageMultiplier = 1f;
        
        [Tooltip("Current poise damage multiplier from mutations")]
        [SerializeField, ReadOnly] private float currentPoiseDamageMultiplier = 1f;
        
        [Tooltip("Current attack speed multiplier from mutations")]
        [SerializeField, ReadOnly] private float currentAttackSpeedMultiplier = 1f;
        
        [Tooltip("Current elemental damage multiplier from mutations")]
        [SerializeField, ReadOnly] private float currentElementalDamageMultiplier = 1f;

        // Runtime state
        private GameObject spawnedWeaponModel;
        private WeaponCollider weaponCollider; // Collider-based damage detection
        private HashSet<Collider> projectileHitTargets = new HashSet<Collider>(); // Only for projectile deflection
        private bool isWeaponActive = false;
        
        // Cached original values
        private int originalDamage;
        private float originalPoiseDamage;
        private int originalElementalBonus;
        private bool originalValuesStored = false;

        #region Public Properties

        /// <summary>
        /// The weapon data this attack uses
        /// </summary>
        public WeaponScriptableObj WeaponData => weaponData;
        
        /// <summary>
        /// The spawned weapon model instance
        /// </summary>
        public GameObject SpawnedWeaponModel => spawnedWeaponModel;
        
        /// <summary>
        /// Current effective damage after mutations
        /// </summary>
        public float EffectiveDamage => GetEffectiveDamage();
        
        /// <summary>
        /// Current effective poise damage after mutations
        /// </summary>
        public float EffectivePoiseDamage => GetEffectivePoiseDamage();

        #endregion

        #region Initialization

        protected override void Awake()
        {
            base.Awake();
            
            // Override attack type for weapon attacks
            if (attackType == 0 || attackType == 1) // Default or generic melee
            {
                attackType = 3; // Weapon attack type
            }
        }

        /// <summary>
        /// Initialize with IAttackOwner and optionally set weapon data
        /// </summary>
        public override void Initialize(IAttackOwner attackOwner)
        {
            base.Initialize(attackOwner);
            
            // Apply weapon data if available
            ApplyWeaponData();
            
            // Spawn weapon model if holder is set
            SpawnWeaponModel();
            
            Debug.Log($"[{attackOwner.gameObject.name}] WeaponAttack initialized | Weapon: {(weaponData != null ? weaponData.objectName : "None")} | Damage: {GetEffectiveDamage()} | Poise: {GetEffectivePoiseDamage()}");
        }

        /// <summary>
        /// Set weapon data at runtime (e.g., when equipping a new weapon)
        /// </summary>
        public void SetWeaponData(WeaponScriptableObj newWeaponData)
        {
            // Destroy old weapon model
            if (spawnedWeaponModel != null)
            {
                Destroy(spawnedWeaponModel);
                spawnedWeaponModel = null;
                weaponCollider = null;
            }
            
            weaponData = newWeaponData;
            originalValuesStored = false;
            
            ApplyWeaponData();
            SpawnWeaponModel();
        }

        /// <summary>
        /// Apply weapon data stats to this attack
        /// </summary>
        private void ApplyWeaponData()
        {
            if (weaponData == null) return;
            
            // Store original values for mutation system
            if (!originalValuesStored)
            {
                originalDamage = weaponData.damage;
                originalPoiseDamage = weaponData.poiseDamage;
                originalElementalBonus = weaponData.elementalDamageBonus;
                originalValuesStored = true;
            }
            
            // Apply base stats from weapon data
            damage = weaponData.damage;
            poiseDamage = weaponData.poiseDamage;
            attackElement = weaponData.weaponElement;
            elementalDamageBonus = weaponData.elementalDamageBonus;
            
            // Update attack range based on weapon reach if not customized
            if (maxRange == 1.5f) // Default value
            {
                // Use weapon's deflection range as approximate max range
                maxRange = weaponData.projectileDeflectionRange;
            }
        }

        /// <summary>
        /// Spawn the weapon model at the holder transform
        /// </summary>
        private void SpawnWeaponModel()
        {
            if (weaponData == null || weaponData.prefab == null || weaponHolder == null) return;
            
            // Destroy existing model
            if (spawnedWeaponModel != null)
            {
                Destroy(spawnedWeaponModel);
            }
            
            // Spawn new model
            spawnedWeaponModel = Instantiate(weaponData.prefab, weaponHolder);
            spawnedWeaponModel.transform.localPosition = weaponPositionOffset;
            spawnedWeaponModel.transform.localRotation = Quaternion.Euler(weaponRotationOffset);
            
            // Find WeaponCollider component for accurate hit detection
            weaponCollider = spawnedWeaponModel.GetComponentInChildren<WeaponCollider>();
            if (weaponCollider != null)
            {
                weaponCollider.Initialize(this);
                Debug.Log($"[{OwnerTransform?.gameObject.name}] Spawned weapon with collider-based detection: {weaponData.objectName}");
            }
            else
            {
                Debug.LogWarning($"[{OwnerTransform?.gameObject.name}] No WeaponCollider found on weapon prefab! Add WeaponCollider component with trigger collider for accurate hit detection.");
            }
        }

        #endregion

        #region Attack Execution

        public override void OnAttack()
        {
            // Clear projectile tracking for this attack
            projectileHitTargets.Clear();
            isWeaponActive = true;
            
            // Call base to play effects
            base.OnAttack();
            
            // Enable weapon collider for damage detection
            if (weaponCollider != null)
            {
                weaponCollider.EnableCollider();
            }
            else
            {
                Debug.LogWarning($"[{OwnerTransform.gameObject.name}] No WeaponCollider available! Weapon will not deal damage.");
            }
            
            // Check for projectile deflection (separate from damage)
            if (weaponData != null && weaponData.canDeflectProjectiles)
            {
                PerformProjectileDeflectionCheck();
            }
        }

        public override void OnAttackEnd()
        {
            isWeaponActive = false;
            
            // Disable weapon collider
            if (weaponCollider != null)
            {
                weaponCollider.DisableCollider();
            }
            
            base.OnAttackEnd();
        }

        /// <summary>
        /// Perform a simple BoxCast check for projectile deflection
        /// This is separate from damage detection and more forgiving
        /// </summary>
        private void PerformProjectileDeflectionCheck()
        {
            if (OwnerTransform == null || weaponData == null) return;
            
            // Simple box in front of the character
            Vector3 boxCenter = OwnerTransform.position + 
                               OwnerTransform.forward * (weaponData.projectileDeflectionRange * 0.5f) +
                               Vector3.up * 1f; // Center at chest height
            
            Vector3 boxSize = new Vector3(
                weaponData.projectileDeflectionRange,
                weaponData.projectileDeflectionHeight,
                weaponData.projectileDeflectionRange
            );
            
            // Use DamageUtils projectile reflection detection
            int projectilesDeflected = DamageUtils.PerformProjectileReflectionDetection(
                this,
                boxCenter,
                boxSize,
                OwnerTransform.rotation,
                projectileHitTargets
            );
            
            if (projectilesDeflected > 0)
            {
                Debug.Log($"[{OwnerTransform.gameObject.name}] Deflected {projectilesDeflected} projectile(s)!");
            }
        }

        #endregion

        #region Mutation Support

        /// <summary>
        /// Apply mutation multipliers to weapon stats
        /// </summary>
        public void ApplyMutationMultipliers(float damageMultiplier = 1f, float poiseDamageMultiplier = 1f, 
            float attackSpeedMultiplier = 1f, float elementalDamageMultiplier = 1f, int activeInstances = 1)
        {
            if (weaponData == null) return;
            
            currentDamageMultiplier = Mathf.Pow(damageMultiplier, activeInstances);
            currentPoiseDamageMultiplier = Mathf.Pow(poiseDamageMultiplier, activeInstances);
            currentAttackSpeedMultiplier = Mathf.Pow(attackSpeedMultiplier, activeInstances);
            currentElementalDamageMultiplier = Mathf.Pow(elementalDamageMultiplier, activeInstances);
            
            // Update the base damage/poise values
            damage = Mathf.RoundToInt(originalDamage * currentDamageMultiplier);
            poiseDamage = originalPoiseDamage * currentPoiseDamageMultiplier;
            elementalDamageBonus = Mathf.RoundToInt(originalElementalBonus * currentElementalDamageMultiplier);
        }

        /// <summary>
        /// Restore original weapon stats
        /// </summary>
        public void RestoreOriginalStats()
        {
            if (!originalValuesStored) return;
            
            currentDamageMultiplier = 1f;
            currentPoiseDamageMultiplier = 1f;
            currentAttackSpeedMultiplier = 1f;
            currentElementalDamageMultiplier = 1f;
            
            damage = originalDamage;
            poiseDamage = originalPoiseDamage;
            elementalDamageBonus = originalElementalBonus;
        }

        /// <summary>
        /// Get effective damage after mutations
        /// </summary>
        private float GetEffectiveDamage()
        {
            if (weaponData != null)
            {
                return Mathf.RoundToInt(originalDamage * currentDamageMultiplier);
            }
            return damage;
        }

        /// <summary>
        /// Get effective poise damage after mutations
        /// </summary>
        private float GetEffectivePoiseDamage()
        {
            if (weaponData != null)
            {
                return originalPoiseDamage * currentPoiseDamageMultiplier;
            }
            return poiseDamage;
        }

        /// <summary>
        /// Get current attack speed (for animation speed adjustment)
        /// </summary>
        public float GetCurrentAttackSpeed()
        {
            if (weaponData != null)
            {
                return weaponData.attackSpeed * currentAttackSpeedMultiplier;
            }
            return 1f;
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (spawnedWeaponModel != null)
            {
                Destroy(spawnedWeaponModel);
            }
        }

        #endregion

        #region Debug Visualization

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            if (weaponData == null) return;
            
            Transform drawTransform = OwnerTransform ?? transform;
            
            // Draw weapon holder position
            if (weaponHolder != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(weaponHolder.position, 0.1f);
            }
            
            // Draw projectile deflection box in yellow (if enabled)
            if (weaponData.canDeflectProjectiles)
            {
                Vector3 boxCenter = drawTransform.position + 
                                   drawTransform.forward * (weaponData.projectileDeflectionRange * 0.5f) +
                                   Vector3.up * 1f;
                
                Vector3 boxSize = new Vector3(
                    weaponData.projectileDeflectionRange,
                    weaponData.projectileDeflectionHeight,
                    weaponData.projectileDeflectionRange
                );
                
                Gizmos.color = Color.yellow;
                Gizmos.matrix = Matrix4x4.TRS(boxCenter, drawTransform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, boxSize);
                Gizmos.matrix = Matrix4x4.identity;
            }
            
            // Note: Weapon collider visualization is handled by WeaponCollider component itself
        }

        #endregion
    }
}

