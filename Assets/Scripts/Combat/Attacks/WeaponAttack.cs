using UnityEngine;
using System.Collections.Generic;

namespace Combat.Attacks
{
    /// <summary>
    /// Attack component that uses a WeaponScriptableObj to define weapon stats and appearance.
    /// This is the unified weapon system that replaces WeaponBase for component-based weapons.
    /// 
    /// USAGE (Standalone - for enemies, simple NPCs):
    /// 1. Add WeaponAttack component to character
    /// 2. Assign weaponData (WeaponScriptableObj) in inspector
    /// 3. Assign weaponHolder transform (e.g., hand bone) in inspector
    /// 4. Weapon will automatically spawn and set up on Start()
    /// 
    /// USAGE (With CharacterInventory - for players, complex NPCs):
    /// 1. Add WeaponAttack component to character
    /// 2. CharacterInventory will call SetWeaponData() to equip weapons at runtime
    /// 3. Leave weaponData empty in inspector (will be set by inventory)
    /// 
    /// OPTIONAL:
    /// - Add MeleeWeaponHitbox component to weapon prefab with trigger collider for precise control
    /// - If not present, collider and Rigidbody will be created automatically
    /// - MeleeWeaponHitbox automatically configures physics for collision detection
    /// 
    /// FEATURES:
    /// - Uses WeaponScriptableObj for damage, poise, element
    /// - Spawns weapon model at specified holder transform
    /// - Collider-based damage detection (accurate, follows animation)
    /// - BoxCast-based projectile deflection (forgiving, easy to hit)
    /// - Automatic collider and Rigidbody setup if not present on prefab
    /// - Supports mutation multipliers for scaling stats
    /// - Works standalone OR with CharacterInventory
    /// - Works with any IAttackOwner (players, NPCs, enemies)
    /// 
    /// HIT DETECTION:
    /// - Damage: MeleeWeaponHitbox component on weapon prefab (auto-created with Rigidbody if missing)
    /// - Projectile Deflection: Simple BoxCast in front of character (more forgiving)
    /// </summary>
    public class WeaponAttack : AnimationAttack
    {
        [Header("Weapon Configuration")]
        [Tooltip("The weapon data defining stats, appearance, and element. Assign in inspector for standalone use, or leave empty if using CharacterInventory (will be set at runtime).")]
        [SerializeField] private WeaponScriptableObj weaponData;
        
        [Tooltip("Transform where the weapon model should be spawned (e.g., hand bone). Required for standalone use, optional if CharacterInventory provides it.")]
        [SerializeField] private Transform weaponHolder;
        
        [Tooltip("Offset position for weapon model relative to holder")]
        [SerializeField] private Vector3 weaponPositionOffset = Vector3.zero;
        
        [Tooltip("Offset rotation for weapon model relative to holder")]
        [SerializeField] private Vector3 weaponRotationOffset = Vector3.zero;
        
        [Header("Hitbox Configuration")]
        [Tooltip("Capsule collider direction for auto-generated hitboxes: 0=X-axis (side-to-side), 1=Y-axis (up/down - typical for swords/bats), 2=Z-axis (forward/back - spears/lances)")]
        [Range(0, 2)]
        [SerializeField] private int hitboxDirection = 1; // Default to Y-axis (up/down)

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
        private MeleeWeaponHitbox weaponHitbox; // Collider-based damage detection
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
        
        /// <summary>
        /// Start is called before the first frame update
        /// If weaponData and weaponHolder are assigned in inspector, set up the weapon automatically
        /// This allows WeaponAttack to work standalone without CharacterInventory
        /// </summary>
        private void Start()
        {
            // If weapon data is assigned in inspector but not spawned yet, set it up
            if (weaponData != null && weaponHolder != null && spawnedWeaponModel == null)
            {
                Debug.Log($"[WeaponAttack] {gameObject.name} has weapon assigned in inspector, setting up automatically");
                ApplyWeaponData();
                SpawnWeaponModel();
                UpdateAnimatorWeaponType();
            }
        }

        /// <summary>
        /// Initialize with IAttackOwner and optionally set weapon data
        /// </summary>
        public override void Initialize(IAttackOwner attackOwner)
        {
            base.Initialize(attackOwner);
            
            // Only spawn weapon if not already spawned from inspector setup
            if (weaponData != null && spawnedWeaponModel == null)
            {
            // Apply weapon data if available
            ApplyWeaponData();
            
            // Spawn weapon model if holder is set
            SpawnWeaponModel();
            
            // Update animator weapon type
            UpdateAnimatorWeaponType();
            }
            
            Debug.Log($"[{attackOwner.gameObject.name}] WeaponAttack initialized | Weapon: {(weaponData != null ? weaponData.objectName : "None")} | Damage: {GetEffectiveDamage()} | Poise: {GetEffectivePoiseDamage()}");
        }

        /// <summary>
        /// Set weapon data at runtime (e.g., when equipping a new weapon)
        /// This is the central method for equipping weapons - handles all setup including animator updates
        /// </summary>
        public void SetWeaponData(WeaponScriptableObj newWeaponData, Transform holder = null)
        {
            if (newWeaponData == null)
        {
                Debug.LogWarning($"[WeaponAttack] SetWeaponData called with null weapon data on {gameObject.name}");
                return;
            }
            
            // Destroy old weapon model
            if (spawnedWeaponModel != null)
            {
                Debug.Log($"[WeaponAttack] Destroying old weapon model: {spawnedWeaponModel.name}");
                Destroy(spawnedWeaponModel);
                spawnedWeaponModel = null;
                weaponHitbox = null;
            }
            
            weaponData = newWeaponData;
            originalValuesStored = false;
            
            // Update weapon holder if provided
            if (holder != null)
            {
                weaponHolder = holder;
                Debug.Log($"[WeaponAttack] Weapon holder set to: {weaponHolder.name}");
            }
            else if (weaponHolder == null)
            {
                Debug.LogWarning($"[WeaponAttack] No weapon holder provided and weaponHolder is null! Weapon will not spawn.");
            }
            
            ApplyWeaponData();
            SpawnWeaponModel();
            UpdateAnimatorWeaponType();
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
        }

        /// <summary>
        /// Spawn the weapon model at the holder transform
        /// </summary>
        private void SpawnWeaponModel()
        {
            if (weaponData == null)
            {
                Debug.LogWarning($"[WeaponAttack] Cannot spawn weapon - weaponData is null on {gameObject.name}");
                return;
            }
            
            if (weaponData.prefab == null)
            {
                Debug.LogWarning($"[WeaponAttack] Cannot spawn weapon - prefab is null on weaponData '{weaponData.objectName}'");
                return;
            }
            
            if (weaponHolder == null)
            {
                Debug.LogError($"[WeaponAttack] Cannot spawn weapon - weaponHolder is null on {gameObject.name}! Assign weaponHolder in inspector or pass it via SetWeaponData().");
                return;
            }
            
            // Destroy existing model
            if (spawnedWeaponModel != null)
            {
                Destroy(spawnedWeaponModel);
            }
            
            // Spawn new model
            spawnedWeaponModel = Instantiate(weaponData.prefab, weaponHolder);
            spawnedWeaponModel.transform.localPosition = weaponPositionOffset;
            spawnedWeaponModel.transform.localRotation = Quaternion.Euler(weaponRotationOffset);
            
            // Find or create MeleeWeaponHitbox component for accurate hit detection
            weaponHitbox = spawnedWeaponModel.GetComponentInChildren<MeleeWeaponHitbox>();
            if (weaponHitbox == null)
            {
                // Try to create MeleeWeaponHitbox automatically
                weaponHitbox = SetupWeaponHitboxAutomatically();
            }
            
            if (weaponHitbox != null)
            {
                weaponHitbox.Initialize(this);
                Debug.Log($"[{OwnerTransform?.gameObject.name}] Weapon equipped with melee hitbox: {weaponData.objectName}");
            }
            else
            {
                Debug.LogWarning($"[{OwnerTransform?.gameObject.name}] Failed to setup MeleeWeaponHitbox! Weapon will not deal damage.");
            }
        }
        
        /// <summary>
        /// Automatically setup MeleeWeaponHitbox if not present on the weapon prefab
        /// Delegates to PhysicsUtils to handle all the complex setup logic
        /// </summary>
        private MeleeWeaponHitbox SetupWeaponHitboxAutomatically()
        {
            if (spawnedWeaponModel == null)
            {
                Debug.LogError("[WeaponAttack] Cannot setup hitbox - no spawned weapon model!");
                return null;
            }
            
            // Use PhysicsUtils to setup the hitbox child properly (handles parenting issues)
            GameObject hitboxObject = PhysicsUtils.SetupWeaponHitboxChild(
                spawnedWeaponModel,
                capsuleDirection: hitboxDirection,
                debugName: $"weapon '{weaponData.objectName}'"
            );
            
            if (hitboxObject == null)
            {
                Debug.LogError($"[WeaponAttack] Failed to setup hitbox for weapon '{weaponData.objectName}'");
                return null;
            }
            
            // Add MeleeWeaponHitbox component (it will handle Rigidbody setup in its Awake())
            MeleeWeaponHitbox hitbox = hitboxObject.GetComponent<MeleeWeaponHitbox>();
            if (hitbox == null)
            {
                hitbox = hitboxObject.AddComponent<MeleeWeaponHitbox>();
                Debug.Log($"[WeaponAttack] Added MeleeWeaponHitbox to: {hitboxObject.name}");
            }
            
            return hitbox;
        }
        
        /// <summary>
        /// Update the animator's WeaponType parameter based on the equipped weapon's animation type
        /// This ensures the animator uses the correct animation set for the weapon (one-handed, two-handed, etc.)
        /// Works for all character types: NPCs (via HumanCharacterController), enemies, and players
        /// </summary>
        private void UpdateAnimatorWeaponType()
        {
            if (weaponData == null || animator == null)
            {
                return;
            }
            
            // Check if the animator has the WeaponType parameter
            if (animator.runtimeAnimatorController == null)
            {
                return;
            }
            
            // Check if the parameter exists
            bool hasParameter = false;
            foreach (var param in animator.parameters)
            {
                if (param.nameHash == GameConstants.AnimatorParams.WeaponTypeHash && param.type == AnimatorControllerParameterType.Int)
                {
                    hasParameter = true;
                    break;
                }
            }
            
            if (!hasParameter)
            {
                Debug.LogWarning($"[WeaponAttack] {OwnerTransform?.gameObject.name ?? gameObject.name} - Animator does not have WeaponType parameter! Weapon will not animate correctly.");
                // Silently fail if the parameter doesn't exist (not all characters may have it)
                return;
            }
            
            // Update the animator parameter with the weapon's animation type
            int weaponTypeInt = (int)weaponData.animationType;
            animator.SetInteger(GameConstants.AnimatorParams.WeaponTypeHash, weaponTypeInt);
            Debug.Log($"[WeaponAttack] {OwnerTransform?.gameObject.name ?? gameObject.name} - Updated animator WeaponType to {weaponData.animationType} ({weaponTypeInt})");
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
            
            // Enable weapon hitbox for damage detection
            if (weaponHitbox != null)
            {
                weaponHitbox.EnableCollider();
                Debug.Log($"[{OwnerTransform?.gameObject.name}] MeleeWeaponHitbox enabled for attack!");
            }
            else
            {
                Debug.LogWarning($"[{OwnerTransform?.gameObject.name}] No MeleeWeaponHitbox available! Weapon will not deal damage. " +
                    $"SpawnedWeaponModel: {(spawnedWeaponModel != null ? spawnedWeaponModel.name : "NULL")}, " +
                    $"WeaponData: {(weaponData != null ? weaponData.objectName : "NULL")}, " +
                    $"WeaponHolder: {(weaponHolder != null ? weaponHolder.name : "NULL")}");
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
            
            // Disable weapon hitbox
            if (weaponHitbox != null)
            {
                weaponHitbox.DisableCollider();
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

