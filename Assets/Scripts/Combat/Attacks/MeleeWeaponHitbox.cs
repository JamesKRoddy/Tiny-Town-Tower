using UnityEngine;
using System.Collections.Generic;

namespace Combat.Attacks
{
    /// <summary>
    /// Handles trigger-based hit detection for melee weapons.
    /// Automatically configures Rigidbody and Collider for proper collision detection.
    /// 
    /// ⚠️ IMPORTANT - PARENTING:
    /// This component should be on a CHILD GameObject of the weapon model, NOT the weapon root.
    /// Adding a Rigidbody to the weapon root breaks the transform hierarchy and causes the weapon
    /// to detach from the hand bone. PhysicsUtils.SetupWeaponHitboxChild() handles this correctly.
    /// 
    /// AUTOMATIC SETUP (Recommended):
    /// WeaponAttack will automatically create and configure this component:
    /// - Creates a child GameObject "WeaponHitbox" under the weapon model
    /// - Adds trigger collider and Rigidbody to the CHILD only
    /// - Weapon root stays a pure transform (no Rigidbody = perfect parenting)
    /// 
    /// MANUAL SETUP (For fine-tuned control):
    /// 1. Create a child GameObject under your weapon prefab (e.g., "Hitbox")
    /// 2. Add a trigger collider to the CHILD (Capsule/Box with "Is Trigger" enabled)
    /// 3. Add this component to the CHILD GameObject (same GameObject as collider)
    /// 4. Component will automatically add Rigidbody to the child in Awake()
    /// 
    /// WEAPON HIERARCHY:
    /// RightHand (Hand Bone)
    /// └── WeaponModel (NO Rigidbody - follows parent perfectly)
    ///     ├── Visual Mesh
    ///     └── Hitbox Child (HAS Rigidbody + Collider + MeleeWeaponHitbox)
    /// 
    /// PHYSICS REQUIREMENTS:
    /// - Trigger Collider (on this GameObject)
    /// - Kinematic Rigidbody (added automatically in Awake to THIS GameObject)
    /// - ContinuousDynamic collision mode (prevents fast weapon tunneling)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MeleeWeaponHitbox : MonoBehaviour
    {
    [Header("Debug")]
    [Tooltip("Show debug logs for hit detection (including layer collision matrix info)")]
    [SerializeField] private bool debugMode = true; // Default to true for initial setup
    
    [Tooltip("Show gizmo visualization of the collider in Scene view")]
    [SerializeField] private bool showGizmo = true;
        
        // References
        private Collider hitboxCollider;
        private Rigidbody hitboxRigidbody;
        private HashSet<Collider> hitTargetsThisSwing = new HashSet<Collider>();
        private WeaponAttack weaponAttack;
        
        // State
        private bool isActive = false;
        
        #region Initialization
        
        private void Awake()
        {
            // STEP 1: Set to Weapon layer if it exists in the project
            int weaponLayer = LayerMask.NameToLayer("Weapon");
            if (weaponLayer >= 0)
            {
                if (gameObject.layer != weaponLayer)
                {
                    gameObject.layer = weaponLayer;
                    if (debugMode)
                    {
                        Debug.Log($"[MeleeWeaponHitbox] Set layer to 'Weapon' on {gameObject.name}");
                    }
                }
            }
            // If Weapon layer doesn't exist, just use whatever layer it's on (likely Default)
            
            // STEP 2: Get or create collider
            hitboxCollider = GetComponent<Collider>();
            
            if (hitboxCollider == null)
            {
                Debug.LogError($"[MeleeWeaponHitbox] No collider found on {gameObject.name}! Add a trigger collider.");
                return;
            }
            
            // STEP 3: Ensure collider is a trigger
            if (!hitboxCollider.isTrigger)
            {
                Debug.LogWarning($"[MeleeWeaponHitbox] Collider on {gameObject.name} is not a trigger! Setting it now.");
                hitboxCollider.isTrigger = true;
            }
            
            // STEP 4: CRITICAL - Configure Rigidbody for trigger detection
            // Unity REQUIRES at least one Rigidbody for OnTriggerEnter to work
            // IMPORTANT: Must use Interpolation.None to prevent drift from animated parent!
            hitboxRigidbody = GetComponent<Rigidbody>();
            if (hitboxRigidbody == null)
            {
                hitboxRigidbody = gameObject.AddComponent<Rigidbody>();
                Debug.Log($"[MeleeWeaponHitbox] Added Rigidbody to {gameObject.name}");
            }
            
            // ALWAYS force correct settings (in case Rigidbody was pre-existing with wrong settings)
            hitboxRigidbody.isKinematic = true;
            hitboxRigidbody.useGravity = false;
            hitboxRigidbody.interpolation = RigidbodyInterpolation.None; // CRITICAL: Prevents drift from animated parent!
            hitboxRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // Best for kinematic
            
            // Start with collider disabled
            hitboxCollider.enabled = false;
            
            if (debugMode)
            {
                Debug.Log($"[MeleeWeaponHitbox] ✅ Initialized on {gameObject.name} | " +
                    $"Collider: {hitboxCollider.GetType().Name} | " +
                    $"Rigidbody: {(hitboxRigidbody != null ? "✅" : "❌")} | " +
                    $"Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer}) | " +
                    $"IsTrigger: {hitboxCollider.isTrigger} | " +
                    $"Interpolation: {hitboxRigidbody.interpolation} | " +
                    $"CollisionMode: {hitboxRigidbody.collisionDetectionMode}");
                    
                // Log layer collision matrix info
                LogLayerCollisionCapabilities();
            }
        }
        
        /// <summary>
        /// Logs which layers this hitbox can collide with (for debugging layer matrix issues)
        /// </summary>
        private void LogLayerCollisionCapabilities()
        {
            int currentLayer = gameObject.layer;
            string layerName = LayerMask.LayerToName(currentLayer);
            
            // Check common enemy/player layers
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int playerLayer = LayerMask.NameToLayer("Player");
            int settlerLayer = LayerMask.NameToLayer("Settler");
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"[MeleeWeaponHitbox] Layer Collision Matrix for '{layerName}' ({currentLayer}):");
            
            if (enemyLayer >= 0)
            {
                bool canHitEnemy = !Physics.GetIgnoreLayerCollision(currentLayer, enemyLayer);
                sb.AppendLine($"  - Enemy layer: {(canHitEnemy ? "✅ CAN collide" : "❌ CANNOT collide")}");
            }
            
            if (playerLayer >= 0)
            {
                bool canHitPlayer = !Physics.GetIgnoreLayerCollision(currentLayer, playerLayer);
                sb.AppendLine($"  - Player layer: {(canHitPlayer ? "✅ CAN collide" : "❌ CANNOT collide")}");
            }
            
            if (settlerLayer >= 0)
            {
                bool canHitSettler = !Physics.GetIgnoreLayerCollision(currentLayer, settlerLayer);
                sb.AppendLine($"  - Settler layer: {(canHitSettler ? "✅ CAN collide" : "❌ CANNOT collide")}");
            }
            
            Debug.Log(sb.ToString());
        }
        
        /// <summary>
        /// Initialize with the parent WeaponAttack component
        /// </summary>
        public void Initialize(WeaponAttack attack)
        {
            weaponAttack = attack;
            
            if (debugMode)
            {
                Debug.Log($"[MeleeWeaponHitbox] Linked to WeaponAttack: {weaponAttack.name}");
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
            if (hitboxCollider == null)
            {
                Debug.LogError($"[MeleeWeaponHitbox] Cannot enable collider - collider is null on {gameObject.name}!");
                return;
            }
            
            hitTargetsThisSwing.Clear();
            hitboxCollider.enabled = true;
            isActive = true;
            
            if (debugMode)
            {
                Debug.Log($"[MeleeWeaponHitbox] ✅ ENABLED on {gameObject.name} | Rigidbody: {(hitboxRigidbody != null ? hitboxRigidbody.isKinematic ? "Kinematic" : "Dynamic" : "NULL")}");
            }
        }
        
        /// <summary>
        /// Disable the weapon collider
        /// Called by WeaponAttack when attack ends
        /// </summary>
        public void DisableCollider()
        {
            if (hitboxCollider == null) return;
            
            hitboxCollider.enabled = false;
            isActive = false;
            hitTargetsThisSwing.Clear();
            
            if (debugMode)
            {
                Debug.Log($"[MeleeWeaponHitbox] ❌ DISABLED on {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Called when the weapon collider hits something
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (debugMode)
            {
                string otherRbInfo = other.attachedRigidbody != null ? 
                    $"Rigidbody: {(other.attachedRigidbody.isKinematic ? "Kinematic" : "Dynamic")}" : 
                    "Rigidbody: NONE";
                    
                Debug.Log($"[MeleeWeaponHitbox] 🎯 OnTriggerEnter: {other.gameObject.name} | " +
                    $"Layer: {LayerMask.LayerToName(other.gameObject.layer)} | " +
                    $"{otherRbInfo} | " +
                    $"Active: {isActive} | " +
                    $"HasWeaponAttack: {weaponAttack != null} | " +
                    $"ThisRigidbody: {(hitboxRigidbody != null ? "✅" : "❌")}");
            }
            
            if (!isActive || weaponAttack == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[MeleeWeaponHitbox] Ignoring hit on {other.gameObject.name} - not active or weaponAttack is null");
                }
                return;
            }
            
            // Prevent hitting the same target multiple times in one swing
            if (hitTargetsThisSwing.Contains(other))
            {
                if (debugMode)
                {
                    Debug.Log($"[MeleeWeaponHitbox] Already hit {other.gameObject.name} this swing, skipping");
                }
                return;
            }
            
            if (debugMode)
            {
                Debug.Log($"[MeleeWeaponHitbox] Processing hit on: {other.gameObject.name} | Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
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
                        Debug.Log($"[MeleeWeaponHitbox] ✅ DAMAGE DEALT to {other.gameObject.name}!");
                    }
                    
                    return;
                }
                else if (debugMode)
                {
                    Debug.Log($"[MeleeWeaponHitbox] Invalid target (allegiance): {other.gameObject.name}");
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
                    Debug.Log($"[MeleeWeaponHitbox] ✅ Hit IHittable: {other.gameObject.name}");
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
            
            DrawWeaponHitboxGizmo(false);
            
            #if UNITY_EDITOR
            // Draw label showing hitbox state
            if (Application.isPlaying)
            {
                string statusText = isActive ? "⚔️ ACTIVE" : "💤 Inactive";
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, statusText);
            }
            else
            {
                // In editor, show layer info
                string layerInfo = $"Layer: {LayerMask.LayerToName(gameObject.layer)}";
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, layerInfo);
            }
            #endif
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showGizmo) return;
            
            DrawWeaponHitboxGizmo(true);
            
            #if UNITY_EDITOR
            // Draw more detailed info when selected
            string info = $"Hitbox: {gameObject.name}\n";
            info += $"Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})\n";
            
            Collider col = hitboxCollider != null ? hitboxCollider : GetComponent<Collider>();
            if (col != null)
            {
                info += $"Collider: {col.GetType().Name}\n";
                info += $"IsTrigger: {col.isTrigger}\n";
            }
            
            Rigidbody rb = hitboxRigidbody != null ? hitboxRigidbody : GetComponent<Rigidbody>();
            if (rb != null)
            {
                info += $"Rigidbody: {(rb.isKinematic ? "Kinematic" : "Dynamic")}\n";
                info += $"Collision: {rb.collisionDetectionMode}";
            }
            else
            {
                info += "Rigidbody: MISSING ⚠️";
            }
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, info);
            #endif
        }
        
        /// <summary>
        /// Draws the weapon hitbox gizmo - always visible in Scene view
        /// </summary>
        private void DrawWeaponHitboxGizmo(bool selected)
        {
            // Get collider at runtime or in editor
            Collider col = hitboxCollider != null ? hitboxCollider : GetComponent<Collider>();
            if (col == null)
            {
                // Draw a small red sphere to show this object exists but has no collider
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.1f);
                return;
            }
            
            // Color scheme based on state
            Color wireColor;
            Color fillColor;
            
            if (Application.isPlaying && isActive)
            {
                // ACTIVE during gameplay - bright red (dealing damage!)
                wireColor = new Color(1f, 0f, 0f, 1f);
                fillColor = new Color(1f, 0f, 0f, 0.3f);
            }
            else if (Application.isPlaying && !isActive)
            {
                // INACTIVE during gameplay - dim cyan
                wireColor = new Color(0f, 1f, 1f, 0.4f);
                fillColor = new Color(0f, 1f, 1f, 0.1f);
            }
            else
            {
                // IN EDITOR - bright cyan (always visible)
                wireColor = new Color(0f, 1f, 1f, selected ? 1f : 0.6f);
                fillColor = new Color(0f, 1f, 1f, selected ? 0.2f : 0.1f);
            }
            
            // Draw collider bounds in world space
            Matrix4x4 originalMatrix = Gizmos.matrix;
            
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                
                // Draw wireframe
                Gizmos.color = wireColor;
                Gizmos.DrawWireCube(box.center, box.size);
                
                // Draw filled
                Gizmos.color = fillColor;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Vector3 worldCenter = transform.TransformPoint(sphere.center);
                float worldRadius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                
                // Draw wireframe
                Gizmos.color = wireColor;
                Gizmos.DrawWireSphere(worldCenter, worldRadius);
                
                // Draw filled
                Gizmos.color = fillColor;
                Gizmos.DrawSphere(worldCenter, worldRadius);
            }
            else if (col is CapsuleCollider capsule)
            {
                DrawCapsuleGizmo(capsule, wireColor, fillColor);
            }
            
            Gizmos.matrix = originalMatrix;
        }
        
        /// <summary>
        /// Helper to draw a proper capsule collider visualization
        /// </summary>
        private void DrawCapsuleGizmo(CapsuleCollider capsule, Color wireColor, Color fillColor)
        {
            Vector3 worldCenter = transform.TransformPoint(capsule.center);
            
            // Calculate scaled dimensions
            float scaledRadius = capsule.radius;
            float scaledHeight = capsule.height;
            
            // Apply scale based on capsule direction
            if (capsule.direction == 0) // X-axis
            {
                scaledRadius *= Mathf.Max(transform.lossyScale.y, transform.lossyScale.z);
                scaledHeight *= transform.lossyScale.x;
            }
            else if (capsule.direction == 1) // Y-axis
            {
                scaledRadius *= Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
                scaledHeight *= transform.lossyScale.y;
            }
            else // Z-axis
            {
                scaledRadius *= Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
                scaledHeight *= transform.lossyScale.z;
            }
            
            // Calculate offset direction
            Vector3 offsetDir = Vector3.zero;
            if (capsule.direction == 0) offsetDir = Vector3.right;
            else if (capsule.direction == 1) offsetDir = Vector3.up;
            else offsetDir = Vector3.forward;
            
            Vector3 worldOffsetDir = transform.TransformDirection(offsetDir);
            float halfHeight = Mathf.Max(0, scaledHeight * 0.5f - scaledRadius);
            Vector3 offset = worldOffsetDir * halfHeight;
            
            // Draw end spheres
            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(worldCenter + offset, scaledRadius);
            Gizmos.DrawWireSphere(worldCenter - offset, scaledRadius);
            
            Gizmos.color = fillColor;
            Gizmos.DrawSphere(worldCenter + offset, scaledRadius);
            Gizmos.DrawSphere(worldCenter - offset, scaledRadius);
            
            // Draw connecting lines
            Gizmos.color = wireColor;
            Vector3 perpDir1, perpDir2;
            if (capsule.direction == 1) // Y-axis
            {
                perpDir1 = transform.TransformDirection(Vector3.right);
                perpDir2 = transform.TransformDirection(Vector3.forward);
            }
            else if (capsule.direction == 0) // X-axis
            {
                perpDir1 = transform.TransformDirection(Vector3.up);
                perpDir2 = transform.TransformDirection(Vector3.forward);
            }
            else // Z-axis
            {
                perpDir1 = transform.TransformDirection(Vector3.right);
                perpDir2 = transform.TransformDirection(Vector3.up);
            }
            
            // Draw 4 lines connecting the spheres
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 radialDir = perpDir1 * Mathf.Cos(angle) + perpDir2 * Mathf.Sin(angle);
                Vector3 lineStart = worldCenter + offset + radialDir * scaledRadius;
                Vector3 lineEnd = worldCenter - offset + radialDir * scaledRadius;
                Gizmos.DrawLine(lineStart, lineEnd);
            }
        }
        
        #endregion
    }
}
