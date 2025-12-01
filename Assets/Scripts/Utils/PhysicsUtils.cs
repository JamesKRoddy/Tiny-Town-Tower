using UnityEngine;

/// <summary>
/// Utility class for common physics and collision setup patterns.
/// Centralizes Rigidbody and Collider configuration to avoid code duplication.
/// </summary>
public static class PhysicsUtils
{
    /// <summary>
    /// Rigidbody configuration presets for different use cases
    /// </summary>
    public enum RigidbodyPreset
    {
        KinematicTrigger,    // For trigger zones (e.g., weapon hitboxes, damage areas)
        PhysicsProjectile,   // For physics-based projectiles
        ManuallyControlled   // For objects moved manually (e.g., arc projectiles)
    }
    
    /// <summary>
    /// Ensures GameObject has a properly configured Rigidbody
    /// </summary>
    /// <param name="gameObject">GameObject to configure</param>
    /// <param name="preset">Configuration preset to use</param>
    /// <param name="debugName">Optional name for debug logging</param>
    /// <returns>The configured Rigidbody component</returns>
    public static Rigidbody EnsureRigidbody(GameObject gameObject, RigidbodyPreset preset, string debugName = null)
    {
        if (gameObject == null)
        {
            Debug.LogError("[PhysicsUtils] Cannot configure Rigidbody - GameObject is null!");
            return null;
        }
        
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        bool wasCreated = false;
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            wasCreated = true;
        }
        
        // Apply preset configuration
        switch (preset)
        {
            case RigidbodyPreset.KinematicTrigger:
                // CRITICAL: Kinematic children of animated bones MUST use:
                // - Interpolation: None (Interpolate causes drift from parent!)
                // - ContinuousSpeculative (designed for kinematic bodies)
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.None; // NEVER use Interpolate on animated children!
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // Better for kinematic
                break;
                
            case RigidbodyPreset.PhysicsProjectile:
                rb.isKinematic = false;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                break;
                
            case RigidbodyPreset.ManuallyControlled:
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.None; // Safer for manual control
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                break;
        }
        
        if (debugName != null)
        {
            if (wasCreated)
            {
                Debug.Log($"[PhysicsUtils] Added Rigidbody ({preset}) to {debugName}");
            }
            else
            {
                Debug.Log($"[PhysicsUtils] Configured existing Rigidbody ({preset}) on {debugName}");
            }
        }
        
        return rb;
    }
    
    /// <summary>
    /// Ensures GameObject has a trigger collider configured properly
    /// </summary>
    /// <param name="gameObject">GameObject to configure</param>
    /// <param name="ensureIsTrigger">Force collider to be a trigger</param>
    /// <param name="debugName">Optional name for debug logging</param>
    /// <returns>The collider component (null if none found)</returns>
    public static Collider EnsureTriggerCollider(GameObject gameObject, bool ensureIsTrigger = true, string debugName = null)
    {
        if (gameObject == null)
        {
            Debug.LogError("[PhysicsUtils] Cannot configure collider - GameObject is null!");
            return null;
        }
        
        Collider col = gameObject.GetComponent<Collider>();
        
        if (col == null)
        {
            if (debugName != null)
            {
                Debug.LogWarning($"[PhysicsUtils] No collider found on {debugName}");
            }
            return null;
        }
        
        // Ensure it's a trigger if requested
        if (ensureIsTrigger && !col.isTrigger)
        {
            col.isTrigger = true;
            if (debugName != null)
            {
                Debug.Log($"[PhysicsUtils] Set collider to trigger on {debugName}");
            }
        }
        
        // For mesh colliders, ensure they're convex (required for triggers with kinematic rigidbodies)
        MeshCollider meshCol = col as MeshCollider;
        if (meshCol != null && ensureIsTrigger && !meshCol.convex)
        {
            meshCol.convex = true;
            if (debugName != null)
            {
                Debug.Log($"[PhysicsUtils] Set mesh collider to convex on {debugName}");
            }
        }
        
        return col;
    }
    
    /// <summary>
    /// Configures all colliders on GameObject to be triggers
    /// </summary>
    /// <param name="gameObject">GameObject to configure</param>
    /// <param name="debugName">Optional name for debug logging</param>
    /// <returns>Array of configured colliders</returns>
    public static Collider[] EnsureAllTriggerColliders(GameObject gameObject, string debugName = null)
    {
        if (gameObject == null)
        {
            Debug.LogError("[PhysicsUtils] Cannot configure colliders - GameObject is null!");
            return null;
        }
        
        Collider[] colliders = gameObject.GetComponents<Collider>();
        
        foreach (Collider col in colliders)
        {
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                if (debugName != null)
                {
                    Debug.Log($"[PhysicsUtils] Set {col.GetType().Name} to trigger on {debugName}");
                }
            }
            
            // For mesh colliders, ensure they're convex
            MeshCollider meshCol = col as MeshCollider;
            if (meshCol != null && !meshCol.convex)
            {
                meshCol.convex = true;
                if (debugName != null)
                {
                    Debug.Log($"[PhysicsUtils] Set mesh collider to convex on {debugName}");
                }
            }
        }
        
        return colliders;
    }
    
    /// <summary>
    /// Creates a default capsule collider for weapon hitboxes
    /// </summary>
    /// <param name="parent">Parent GameObject to attach collider to</param>
    /// <param name="name">Name for the collider GameObject</param>
    /// <param name="radius">Collider radius</param>
    /// <param name="height">Collider height</param>
    /// <param name="centerOffset">Center offset (typically upward for weapons)</param>
    /// <param name="direction">Capsule direction (0=X, 1=Y, 2=Z). Default is 1 (Y-axis) for most weapons</param>
    /// <returns>The created GameObject with collider</returns>
    public static GameObject CreateCapsuleCollider(GameObject parent, string name = "Hitbox", 
        float radius = 0.15f, float height = 1.2f, Vector3? centerOffset = null, int direction = 1)
    {
        if (parent == null)
        {
            Debug.LogError("[PhysicsUtils] Cannot create collider - parent is null!");
            return null;
        }
        
        GameObject colliderObject = new GameObject(name);
        colliderObject.transform.SetParent(parent.transform, false);
        colliderObject.transform.localPosition = Vector3.zero;
        colliderObject.transform.localRotation = Quaternion.identity;
        
        CapsuleCollider capsule = colliderObject.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.radius = radius;
        capsule.height = height;
        capsule.direction = direction; // 0=X-axis, 1=Y-axis (up/down), 2=Z-axis (forward/back)
        
        // Default center based on direction
        if (centerOffset == null)
        {
            if (direction == 0) centerOffset = new Vector3(height * 0.5f, 0, 0); // Extend right
            else if (direction == 1) centerOffset = new Vector3(0, height * 0.5f, 0); // Extend up
            else centerOffset = new Vector3(0, 0, height * 0.5f); // Extend forward
        }
        
        capsule.center = centerOffset.Value;
        
        string directionName = direction == 0 ? "X" : direction == 1 ? "Y" : "Z";
        Debug.Log($"[PhysicsUtils] Created CapsuleCollider on {parent.name}/{name} (r={radius:F2}, h={height:F2}, direction={directionName}-axis)");
        
        return colliderObject;
    }
    
    /// <summary>
    /// Finds or creates a collider on a GameObject, ensuring it's configured as a trigger
    /// </summary>
    /// <param name="gameObject">GameObject to search/configure</param>
    /// <param name="searchChildren">Also search child GameObjects</param>
    /// <param name="createDefault">Create a default capsule collider if none found</param>
    /// <param name="debugName">Optional name for debug logging</param>
    /// <returns>Tuple of (collider, GameObject containing collider)</returns>
    public static (Collider collider, GameObject gameObject) FindOrCreateTriggerCollider(
        GameObject gameObject, bool searchChildren = true, bool createDefault = true, string debugName = null)
    {
        if (gameObject == null)
        {
            Debug.LogError("[PhysicsUtils] Cannot find/create collider - GameObject is null!");
            return (null, null);
        }
        
        // Try to find existing collider
        Collider existingCollider = searchChildren ? 
            gameObject.GetComponentInChildren<Collider>() : 
            gameObject.GetComponent<Collider>();
        
        if (existingCollider != null)
        {
            // Configure existing collider as trigger
            EnsureTriggerCollider(existingCollider.gameObject, true, debugName);
            
            if (debugName != null)
            {
                Debug.Log($"[PhysicsUtils] Found existing collider on {existingCollider.gameObject.name}");
            }
            
            return (existingCollider, existingCollider.gameObject);
        }
        
        // No collider found, create default if requested
        if (createDefault)
        {
            GameObject colliderObj = CreateCapsuleCollider(gameObject, "AutoCollider");
            Collider newCollider = colliderObj.GetComponent<Collider>();
            
            if (debugName != null)
            {
                Debug.Log($"[PhysicsUtils] Created default collider for {debugName}");
            }
            
            return (newCollider, colliderObj);
        }
        
        return (null, null);
    }
    
    /// <summary>
    /// Sets up a weapon hitbox properly, ensuring Rigidbody is on a child GameObject to avoid breaking parenting.
    /// This is the recommended way to setup weapon hitboxes to prevent the weapon from detaching from the hand.
    /// </summary>
    /// <param name="weaponModelRoot">The weapon model root GameObject (spawned prefab)</param>
    /// <param name="capsuleDirection">Direction for auto-created capsule colliders (0=X, 1=Y, 2=Z)</param>
    /// <param name="debugName">Optional name for debug logging</param>
    /// <returns>GameObject with properly configured hitbox (collider + Rigidbody ready)</returns>
    public static GameObject SetupWeaponHitboxChild(GameObject weaponModelRoot, int capsuleDirection = 1, string debugName = null)
    {
        if (weaponModelRoot == null)
        {
            Debug.LogError("[PhysicsUtils] Cannot setup weapon hitbox - weaponModelRoot is null!");
            return null;
        }
        
        // Get Weapon layer if it exists, otherwise use Default
        int weaponLayer = LayerMask.NameToLayer("Weapon");
        if (weaponLayer < 0)
        {
            weaponLayer = 0; // Default layer - Weapon layer not configured in project
        }
        
        // Check if weapon model root has a collider
        Collider rootCollider = weaponModelRoot.GetComponent<Collider>();
        GameObject hitboxObject = null;
        
        if (rootCollider != null)
        {
            // CRITICAL: Never add Rigidbody to weapon root - it breaks parenting!
            // Create a dedicated child GameObject for the hitbox instead
            if (debugName != null)
            {
                Debug.Log($"[PhysicsUtils] Found collider on weapon root - creating dedicated hitbox child for '{debugName}'");
            }
            
            // Create child GameObject for hitbox
            hitboxObject = new GameObject("WeaponHitbox");
            hitboxObject.transform.SetParent(weaponModelRoot.transform, false);
            hitboxObject.transform.localPosition = Vector3.zero;
            hitboxObject.transform.localRotation = Quaternion.identity;
            hitboxObject.layer = weaponLayer; // CRITICAL: Set to Weapon layer for collision matrix
            
            // Copy collider settings to child
            // For capsule colliders, we can override the direction for weapon-specific orientation
            CopyColliderToGameObject(rootCollider, hitboxObject, capsuleDirection);
            
            // Disable root collider to avoid duplicate detection
            rootCollider.enabled = false;
            
            if (debugName != null)
            {
                Debug.Log($"[PhysicsUtils] Created hitbox child (Layer: {LayerMask.LayerToName(weaponLayer)}) and disabled root collider for '{debugName}'");
            }
        }
        else
        {
            // No root collider, search children or create new
            Collider existingCollider = weaponModelRoot.GetComponentInChildren<Collider>();
            
            if (existingCollider != null)
            {
                // Use existing child collider
                hitboxObject = existingCollider.gameObject;
                hitboxObject.layer = weaponLayer; // Ensure correct layer
                EnsureTriggerCollider(hitboxObject, true, debugName);
            }
            else
            {
                // No collider found anywhere, create new child with collider
                hitboxObject = CreateCapsuleCollider(weaponModelRoot, "WeaponHitbox", direction: capsuleDirection);
                hitboxObject.layer = weaponLayer; // Set to Weapon layer
                
                string directionName = capsuleDirection == 0 ? "X" : capsuleDirection == 1 ? "Y" : "Z";
                if (debugName != null)
                {
                    Debug.Log($"[PhysicsUtils] Created new hitbox child (Layer: {LayerMask.LayerToName(weaponLayer)}) with {directionName}-axis capsule collider for '{debugName}'");
                }
            }
        }
        
        return hitboxObject;
    }
    
    /// <summary>
    /// Copies collider settings from source to target GameObject
    /// </summary>
    /// <param name="source">Source collider to copy from</param>
    /// <param name="target">Target GameObject to add collider to</param>
    /// <param name="overrideCapsuleDirection">Optional override for capsule direction (0=X, 1=Y, 2=Z). If null, uses source direction.</param>
    private static void CopyColliderToGameObject(Collider source, GameObject target, int? overrideCapsuleDirection = null)
    {
        if (source is CapsuleCollider sourceCapsule)
        {
            CapsuleCollider targetCapsule = target.AddComponent<CapsuleCollider>();
            targetCapsule.isTrigger = true;
            targetCapsule.radius = sourceCapsule.radius;
            targetCapsule.height = sourceCapsule.height;
            
            // Use override direction if provided, otherwise copy from source
            int direction = overrideCapsuleDirection ?? sourceCapsule.direction;
            targetCapsule.direction = direction;
            
            // Adjust center based on new direction if overridden
            if (overrideCapsuleDirection.HasValue && overrideCapsuleDirection.Value != sourceCapsule.direction)
            {
                // Recalculate center for new direction
                float halfHeight = sourceCapsule.height * 0.5f;
                if (direction == 0) targetCapsule.center = new Vector3(halfHeight, 0, 0);
                else if (direction == 1) targetCapsule.center = new Vector3(0, halfHeight, 0);
                else targetCapsule.center = new Vector3(0, 0, halfHeight);
            }
            else
            {
                targetCapsule.center = sourceCapsule.center;
            }
        }
        else if (source is BoxCollider sourceBox)
        {
            BoxCollider targetBox = target.AddComponent<BoxCollider>();
            targetBox.isTrigger = true;
            targetBox.size = sourceBox.size;
            targetBox.center = sourceBox.center;
        }
        else if (source is SphereCollider sourceSphere)
        {
            SphereCollider targetSphere = target.AddComponent<SphereCollider>();
            targetSphere.isTrigger = true;
            targetSphere.radius = sourceSphere.radius;
            targetSphere.center = sourceSphere.center;
        }
        else
        {
            // Unknown collider type, create default capsule along Y-axis (typical for weapons)
            CapsuleCollider capsule = target.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.radius = 0.15f;
            capsule.height = 1.2f;
            capsule.direction = 1; // Y-axis (up/down - typical for swords, bats, etc.)
            capsule.center = new Vector3(0, 0.6f, 0); // Offset upward
            
            Debug.LogWarning($"[PhysicsUtils] Unknown collider type on {source.gameObject.name}, created default Y-axis capsule");
        }
    }
}

