using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Managers;
using Enemies;

/// <summary>
/// Base class for all placeable structures (buildings and turrets) in the camp.
/// Handles common functionality like construction, damage, repair, and upgrade.
/// </summary>
[RequireComponent(typeof(StructureRepairTask))]
[RequireComponent(typeof(StructureUpgradeTask))]
public abstract class PlaceableStructure<T> : MonoBehaviour, IDamageable, IBuildingDamageable, IPlaceableStructure where T : PlaceableObjectParent
{
    #region Serialized Fields
    
    [Header("Structure Configuration")]
    [SerializeField] private T structureScriptableObj;
    [SerializeField] protected CampBuildingCategory buildingCategory = CampBuildingCategory.BASIC_BUILDING;

    [Header("Structure State")]
    [SerializeField, ReadOnly] protected bool isOperational = false;
    [SerializeField, ReadOnly] protected bool isUnderConstruction = true;
    [SerializeField, ReadOnly] protected float currentHealth;
    [SerializeField, ReadOnly] protected WorkTask currentWorkTask;

    [Header("Work Tasks")]
    [SerializeField, ReadOnly] protected StructureRepairTask repairTask;
    [SerializeField, ReadOnly] protected StructureUpgradeTask upgradeTask;

    [Header("Damage Effects")]
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private AnimationCurve shakeDecay = AnimationCurve.EaseInOut(0, 1, 1, 0);

    #endregion

    #region Private Fields
    
    private MeshRenderer[] meshRenderers;
    private Vector3[] originalMeshPositions;
    private Coroutine currentShakeCoroutine;
    
    #endregion

    #region Properties
    
    protected T StructureScriptableObj => structureScriptableObj;
    
    public float Health 
    { 
        get => currentHealth; 
        set => currentHealth = Mathf.Clamp(value, 0, MaxHealth); 
    }
    
    public virtual float MaxHealth
    {
        get => structureScriptableObj != null ? structureScriptableObj.maxHealth : 100f;
        set { /* Override in derived classes if needed */ }
    }
    
    // IDamageable implementation - for compatibility with existing systems
    public CharacterType CharacterType => CharacterType.NONE;
    
    // IBuildingDamageable implementation - for building-specific effects
    public CampBuildingCategory BuildingCategory => buildingCategory;
    
    public Allegiance GetAllegiance() => Allegiance.FRIENDLY;
    public WorkTask GetCurrentWorkTask() => currentWorkTask;

    #endregion

    #region Gizmos for Development

    private void OnDrawGizmos()
    {
        if (structureScriptableObj == null) return;
        
        // Draw the structure size boundary
        DrawStructureSizeGizmo(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (structureScriptableObj == null) return;
        
        // Draw the structure size boundary with more prominent colors when selected
        DrawStructureSizeGizmo(true);
    }

    private void DrawStructureSizeGizmo(bool isSelected)
    {
        Vector2Int size = structureScriptableObj.size;
        Vector3 center = transform.position;
        
        // Get the actual grid unit size from CampManager
        float gridUnitSize = 2f; // Default fallback
        if (CampManager.Instance != null)
        {
            gridUnitSize = CampManager.Instance.SharedGridSize;
        }
        
        // Calculate the actual world space size based on grid units
        Vector3 worldSize = new Vector3(size.x * gridUnitSize, 0, size.y * gridUnitSize);
        Vector3 halfWorldSize = worldSize * 0.5f;
        
        // Calculate the corners of the structure based on actual world size
        Vector3 topLeft = center + new Vector3(-halfWorldSize.x, 0, halfWorldSize.z);
        Vector3 topRight = center + new Vector3(halfWorldSize.x, 0, halfWorldSize.z);
        Vector3 bottomLeft = center + new Vector3(-halfWorldSize.x, 0, -halfWorldSize.z);
        Vector3 bottomRight = center + new Vector3(halfWorldSize.x, 0, -halfWorldSize.z);
        
        // Set gizmo color based on selection state
        if (isSelected)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow
        }
        
        // Draw the boundary rectangle
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
        
        // Draw center point
        Gizmos.color = isSelected ? Color.red : new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(center, 0.1f);
        
        // Draw size labels if selected
        if (isSelected)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(center + Vector3.up * 0.5f, $"Grid: {size.x}x{size.y} | World: {worldSize.x:F1}x{worldSize.z:F1}");
            #endif
        }
    }

    #endregion

    #region Unity Lifecycle

    protected virtual void Start()
    {
        // Override in derived classes if needed
    }

    protected virtual void OnDestroy()
    {
        // Stop any ongoing shake effects
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
            RestoreOriginalMeshPositions();
        }
        
        // Unregister from CampManager target tracking
        if (CampManager.Instance != null)
        {
            CampManager.Instance.UnregisterTarget(this);
        }
        
        // Free up grid slots when structure is destroyed
        if (structureScriptableObj != null && CampManager.Instance != null)
        {
            CampManager.Instance.MarkSharedGridSlotsUnoccupied(transform.position, structureScriptableObj.size);
        }
    }

    #endregion

    #region Structure Setup

    public virtual void SetupStructure(T scriptableObj)
    {
        this.structureScriptableObj = scriptableObj;
        currentHealth = GetMaxHealthFromScriptableObject();

        SetupNavmeshObstacle();
        SetupCollider();
        OnStructureSetup();
    }

    protected virtual void OnStructureSetup()
    {
        SetupRepairTask();
        SetupUpgradeTask();
        SetupDamageShakeEffect();
    }

    private void SetupDamageShakeEffect()
    {
        // Find all mesh renderers in this building and its children
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        originalMeshPositions = new Vector3[meshRenderers.Length];
        
        // Store original positions
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            originalMeshPositions[i] = meshRenderers[i].transform.localPosition;
        }
        
        Debug.Log($"[{gameObject.name}] Setup damage shake effect for {meshRenderers.Length} mesh renderers");
    }

    private void SetupCollider()
    {
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();        
        }
    }

    private void SetupNavmeshObstacle()
    {
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            Debug.LogWarning($"Adding NavMeshObstacle to {gameObject.name}");
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.size = new Vector3(structureScriptableObj.size.x, 1.0f, structureScriptableObj.size.y);
        }
        else
        {
            obstacle.carving = true;
        }        
    }

    private void SetupRepairTask()
    {
        repairTask = GetComponent<StructureRepairTask>();
        if (repairTask == null)
        {
            repairTask = gameObject.AddComponent<StructureRepairTask>();
        }
        repairTask.transform.position = transform.position;
        
        // Convert game hours to real seconds using TimeManager
        float repairTimeInSeconds = Managers.TimeManager.ConvertGameHoursToSecondsStatic(StructureScriptableObj.repairTimeInGameHours);
        
        repairTask.SetupRepairTask(
            repairTimeInSeconds,
            StructureScriptableObj.healthRestoredPerRepair
        );
    }

    private void SetupUpgradeTask()
    {
        upgradeTask = GetComponent<StructureUpgradeTask>();
        if (upgradeTask == null)
        {
            upgradeTask = gameObject.AddComponent<StructureUpgradeTask>();
        }
        upgradeTask.transform.position = transform.position;
        
        // Set up the upgrade task with the upgrade target
        if (StructureScriptableObj.upgradeTarget != null)
        {
            upgradeTask.SetupUpgradeTask(StructureScriptableObj.upgradeTarget);
        }
    }

    #endregion

    #region Structure State Management

    public virtual void CompleteConstruction()
    {
        isUnderConstruction = false;
        isOperational = true;
        
        // Register with CampManager for target tracking
        if (CampManager.Instance != null)
        {
            CampManager.Instance.RegisterTarget(this);
        }
        
        // Play construction complete effect (size-based via BuildManager)
        Vector3 effectPosition = transform.position + Vector3.up * 1.5f;
        Vector3 effectNormal = Vector3.up;
        Vector2Int structureSize = StructureScriptableObj != null ? StructureScriptableObj.size : new Vector2Int(1, 1);
        CampManager.Instance?.BuildManager?.PlayConstructionCompleteEffect(effectPosition, effectNormal, structureSize);
    }

    public bool IsOperational() => isOperational;
    public bool IsUnderConstruction() => isUnderConstruction;
    public float GetHealthPercentage() => currentHealth / MaxHealth;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => MaxHealth;

    public virtual bool CanInteract()
    {
        return !isUnderConstruction && isOperational;
    }

    #endregion

    #region Damage & Health

    public virtual void TakeDamage(float amount, Transform damageSource = null)
    {
        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        
        OnDamageTaken?.Invoke(amount, currentHealth);
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);
        
        // Calculate actual hit point and surface normal
        Vector3 hitPoint;
        Vector3 hitNormal;
        
        if (damageSource != null)
        {
            // Calculate hit point on building surface
            hitPoint = CalculateHitPoint(damageSource.position);
            // Calculate surface normal at hit point
            hitNormal = CalculateSurfaceNormal(hitPoint, damageSource.position);
        }
        else
        {
            // Fallback for damage without source (like environmental damage)
            hitPoint = transform.position + Vector3.up * 1.5f;
            hitNormal = Vector3.up;
        }
        
        // Play hit VFX using building effects at actual hit point
        EffectManager.Instance?.PlayHitEffect(hitPoint, hitNormal, buildingCategory);
        
        // Trigger damage shake effect
        TriggerDamageShake(amount);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Triggers the damage shake effect on all mesh renderers
    /// </summary>
    private void TriggerDamageShake(float damageAmount)
    {
        if (meshRenderers == null || meshRenderers.Length == 0) return;
        
        // Stop any existing shake
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
        }
        
        // Scale shake intensity based on damage amount and current health percentage
        float healthPercentage = GetHealthPercentage();
        float damageIntensityMultiplier = Mathf.Clamp01(damageAmount / MaxHealth * 10f); // Scale based on relative damage
        float healthIntensityMultiplier = Mathf.Lerp(0.5f, 1.5f, 1f - healthPercentage); // Shake more when damaged
        
        float finalIntensity = shakeIntensity * damageIntensityMultiplier * healthIntensityMultiplier;
        
        // Start shake coroutine
        currentShakeCoroutine = StartCoroutine(ShakeMeshRenderers(finalIntensity));
    }

    /// <summary>
    /// Coroutine that shakes all mesh renderers for the specified duration
    /// </summary>
    private IEnumerator ShakeMeshRenderers(float intensity)
    {
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            float currentIntensity = intensity * shakeDecay.Evaluate(progress);
            
            // Apply random shake to each mesh renderer
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i] != null)
                {
                    Vector3 randomOffset = new Vector3(
                        UnityEngine.Random.Range(-currentIntensity, currentIntensity),
                        UnityEngine.Random.Range(-currentIntensity, currentIntensity),
                        UnityEngine.Random.Range(-currentIntensity, currentIntensity)
                    );
                    
                    meshRenderers[i].transform.localPosition = originalMeshPositions[i] + randomOffset;
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restore original positions
        RestoreOriginalMeshPositions();
        currentShakeCoroutine = null;
    }

    /// <summary>
    /// Restores all mesh renderers to their original positions
    /// </summary>
    private void RestoreOriginalMeshPositions()
    {
        if (meshRenderers == null || originalMeshPositions == null) return;
        
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] != null)
            {
                meshRenderers[i].transform.localPosition = originalMeshPositions[i];
            }
        }
    }

    /// <summary>
    /// Calculates the actual hit point on the building's surface closest to the damage source
    /// </summary>
    private Vector3 CalculateHitPoint(Vector3 damageSourcePosition)
    {
        Collider buildingCollider = GetComponent<Collider>();
        if (buildingCollider == null)
        {
            // Fallback if no collider
            return transform.position + Vector3.up * 1.5f;
        }

        // Try to get the damage source's collider (enemy attack cube)
        GameObject damageSourceObj = null;
        Collider damageSourceCollider = null;
        
        // Look for damage source in nearby area (enemies typically have colliders)
        Collider[] nearbyColliders = Physics.OverlapSphere(damageSourcePosition, 2f);
        foreach (var collider in nearbyColliders)
        {
            // Look for enemy colliders or weapon colliders
            if (collider.gameObject.layer == LayerMask.NameToLayer("Enemy") ||
                collider.gameObject.layer == LayerMask.NameToLayer("Weapon") ||
                collider.CompareTag("Enemy") ||
                collider.name.Contains("Attack") ||
                collider.name.Contains("Weapon"))
            {
                damageSourceCollider = collider;
                damageSourceObj = collider.gameObject;
                break;
            }
        }

        Vector3 hitPoint;

        if (damageSourceCollider != null)
        {
            // Calculate intersection center between damage source collider and building collider
            hitPoint = CalculateColliderIntersectionCenter(buildingCollider, damageSourceCollider);
        }
        else
        {
            // Fallback: use closest point but offset it upward to a more reasonable height
            Vector3 closestPoint = buildingCollider.ClosestPoint(damageSourcePosition);
            
            // If the closest point is too low, adjust it to a more reasonable height
            Bounds buildingBounds = buildingCollider.bounds;
            float minHeight = buildingBounds.min.y + buildingBounds.size.y * 0.2f; // 20% up from bottom
            float maxHeight = buildingBounds.max.y - buildingBounds.size.y * 0.1f; // 90% of total height
            
            if (closestPoint.y < minHeight)
            {
                closestPoint.y = minHeight;
            }
            else if (closestPoint.y > maxHeight)
            {
                closestPoint.y = maxHeight;
            }
            
            hitPoint = closestPoint;
        }
        
        return hitPoint;
    }

    /// <summary>
    /// Calculates the center point of intersection between two colliders
    /// </summary>
    private Vector3 CalculateColliderIntersectionCenter(Collider buildingCollider, Collider damageSourceCollider)
    {
        Bounds buildingBounds = buildingCollider.bounds;
        Bounds damageBounds = damageSourceCollider.bounds;
        
        // Calculate the intersection bounds
        Vector3 intersectionMin = Vector3.Max(buildingBounds.min, damageBounds.min);
        Vector3 intersectionMax = Vector3.Min(buildingBounds.max, damageBounds.max);
        
        // Check if there's actually an intersection
        if (intersectionMin.x <= intersectionMax.x && 
            intersectionMin.y <= intersectionMax.y && 
            intersectionMin.z <= intersectionMax.z)
        {
            // Return the center of the intersection volume
            Vector3 intersectionCenter = (intersectionMin + intersectionMax) * 0.5f;
            
            // Project this center point onto the building's surface
            Vector3 surfacePoint = buildingCollider.ClosestPoint(intersectionCenter);
            
            // If the surface point is too different from our intersection center,
            // use the intersection center but clamp it to the building bounds
            float distance = Vector3.Distance(intersectionCenter, surfacePoint);
            if (distance > 0.5f) // If too far from surface
            {
                // Move the intersection center to the building surface
                Vector3 directionToCenter = (intersectionCenter - buildingBounds.center).normalized;
                surfacePoint = buildingBounds.center + directionToCenter * (buildingBounds.size.magnitude * 0.5f);
                surfacePoint = buildingCollider.ClosestPoint(surfacePoint);
            }
            
            return surfacePoint;
        }
        else
        {
            // No intersection found, fall back to closest point with height adjustment
            Vector3 closestPoint = buildingCollider.ClosestPoint(damageBounds.center);
            
            // Adjust height to be more in the middle of the building vertically
            float targetHeight = Mathf.Lerp(buildingBounds.min.y, buildingBounds.max.y, 0.4f); // 40% up the building
            closestPoint.y = Mathf.Max(closestPoint.y, targetHeight);
            
            return closestPoint;
        }
    }

    /// <summary>
    /// Calculates the surface normal at the hit point, facing outward from the building
    /// </summary>
    private Vector3 CalculateSurfaceNormal(Vector3 hitPoint, Vector3 damageSourcePosition)
    {
        Collider buildingCollider = GetComponent<Collider>();
        if (buildingCollider == null)
        {
            // Fallback: direction from building center to damage source
            return (damageSourcePosition - transform.position).normalized;
        }

        // Cast a ray from slightly inside the building toward the hit point to get surface normal
        Vector3 rayOrigin = Vector3.Lerp(transform.position, hitPoint, 0.9f);
        Vector3 rayDirection = (hitPoint - rayOrigin).normalized;
        
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, 10f))
        {
            if (hit.collider == buildingCollider)
            {
                return hit.normal;
            }
        }
        
        // Alternative method: use the direction from building center to hit point
        Vector3 outwardDirection = (hitPoint - transform.position).normalized;
        
        // For box colliders, we can snap to the closest face normal
        if (buildingCollider is BoxCollider)
        {
            return SnapToBoxFaceNormal(outwardDirection);
        }
        
        return outwardDirection;
    }

    /// <summary>
    /// Snaps a direction to the closest box face normal for more accurate effects on rectangular buildings
    /// </summary>
    private Vector3 SnapToBoxFaceNormal(Vector3 direction)
    {
        // Transform direction to local space
        Vector3 localDirection = transform.InverseTransformDirection(direction);
        
        // Find the axis with the largest absolute component
        float absX = Mathf.Abs(localDirection.x);
        float absY = Mathf.Abs(localDirection.y);
        float absZ = Mathf.Abs(localDirection.z);
        
        Vector3 snappedLocal;
        if (absX >= absY && absX >= absZ)
        {
            // X axis is dominant
            snappedLocal = new Vector3(Mathf.Sign(localDirection.x), 0, 0);
        }
        else if (absY >= absX && absY >= absZ)
        {
            // Y axis is dominant
            snappedLocal = new Vector3(0, Mathf.Sign(localDirection.y), 0);
        }
        else
        {
            // Z axis is dominant
            snappedLocal = new Vector3(0, 0, Mathf.Sign(localDirection.z));
        }
        
        // Transform back to world space
        return transform.TransformDirection(snappedLocal);
    }

    public virtual void Heal(float amount)
    {
        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        
        OnHeal?.Invoke(amount, currentHealth);
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);
        OnStructureRepaired?.Invoke();
    }

    public virtual void Repair(float amount)
    {
        Heal(amount);
        
        // Play repair effect
        Vector3 repairPoint = transform.position + Vector3.up * 1.5f;
        Vector3 repairNormal = Vector3.up;
        EffectManager.Instance?.PlayRepairEffect(repairPoint, repairNormal, structureScriptableObj.size);
    }

    public virtual void Die()
    {
        Debug.Log($"{gameObject.name} has been destroyed!");
        OnDeath?.Invoke();
        OnDestroyed?.Invoke();
        OnStructureDestroyed?.Invoke();
        
        // Notify all enemies that this structure was destroyed
        EnemyBase.NotifyTargetDestroyed(transform);
        
        // Play destruction VFX using building effects
        Vector3 destructionPoint = transform.position + Vector3.up * 1.5f;
        Vector3 destructionNormal = Vector3.up;
        EffectManager.Instance?.PlayDestructionEffect(destructionPoint, destructionNormal, structureScriptableObj.size);
        
        // Destroy the structure
        Destroy(gameObject);
    }

    public virtual void Destroy()
    {
        Die();
    }

    #endregion

    #region Events
    
    public event Action OnStructureDestroyed;
    public event Action OnStructureRepaired;
    public event Action OnStructureUpgraded;
    public event Action<float> OnHealthChanged;
    public event Action<float, float> OnDamageTaken;
    public event Action<float, float> OnHeal;
    public event Action OnDeath;
    public event Action OnDestroyed;

    #endregion

    #region Work Task Management
    
    public virtual void SetCurrentWorkTask(WorkTask workTask)
    {
        currentWorkTask = workTask;
    }
    
    #endregion

    #region Upgrade System

    public virtual bool CanUpgrade()
    {
        return structureScriptableObj != null && 
               structureScriptableObj.upgradeTarget != null;
    }

    public void TriggerUpgradeEvent()
    {
        OnStructureUpgraded?.Invoke();
        
        // Play upgrade effect
        Vector3 upgradePoint = transform.position + Vector3.up * 1.5f;
        Vector3 upgradeNormal = Vector3.up;
        EffectManager.Instance?.PlayUpgradeEffect(upgradePoint, upgradeNormal, structureScriptableObj.size);
    }

    public void TriggerRepairEvent()
    {
        OnStructureRepaired?.Invoke();
    }

    public void TriggerDestroyEvent()
    {
        OnStructureDestroyed?.Invoke();
    }

    #endregion

    #region Destruction

    public virtual void StartDestruction()
    {
        UnassignWorkers();

        GameObject destructionPrefab = CampManager.Instance.BuildManager.GetDestructionPrefab(StructureScriptableObj.size);
        if (destructionPrefab != null)
        {
            CreateDestructionTask(destructionPrefab);
        }
    }

    private void UnassignWorkers()
    {
        if (repairTask != null && repairTask.IsOccupied)
        {
            repairTask.UnassignNPC();
        }
        if (upgradeTask != null && upgradeTask.IsOccupied)
        {
            upgradeTask.UnassignNPC();
        }
    }

    private void CreateDestructionTask(GameObject destructionPrefab)
    {
        GameObject destructionTaskObj = Instantiate(destructionPrefab, transform.position, transform.rotation);
        
        StructureDestructionTask destructionTask = destructionTaskObj.AddComponent<StructureDestructionTask>();
        destructionTask.SetupDestructionTask(this);

        if (CampManager.Instance != null)
        {
            CampManager.Instance.WorkManager.AddWorkTask(destructionTask);
        }
        else
        {
            Debug.LogError("CampManager.Instance is null. Cannot add destruction task.");
        }

        Destroy(gameObject);
    }

    #endregion

    #region Utility Methods

    protected virtual float GetMaxHealthFromScriptableObject()
    {
        return structureScriptableObj != null ? structureScriptableObj.maxHealth : 100f;
    }

    public T GetStructureScriptableObj()
    {
        return structureScriptableObj;
    }

    PlaceableObjectParent IPlaceableStructure.GetStructureScriptableObj()
    {
        return structureScriptableObj;
    }

    string IPlaceableStructure.GetSaveId()
    {
        return GetSaveId();
    }

    void IPlaceableStructure.RestoreFromSaveData(object saveData)
    {
        RestoreFromSaveData(saveData);
    }

    public virtual string GetInteractionText()
    {
        if (isUnderConstruction) return "Structure under construction";
        if (!isOperational) return "Structure not operational";
        
        string text = "Structure Options:\n";
        if (repairTask != null && repairTask.CanPerformTask())
            text += "- Repair\n";
        if (upgradeTask != null && upgradeTask.CanPerformTask())
            text += "- Upgrade\n";
        if (CanUpgrade())
            text += "- Upgrade to Next Level\n";
        return text;
    }

    public StructureRepairTask GetRepairTask() => repairTask;
    public StructureUpgradeTask GetUpgradeTask() => upgradeTask;

    #endregion

    #region Save/Load Implementation

    public virtual string GetSaveId()
    {
        return gameObject.GetInstanceID().ToString();
    }

    public virtual void RestoreFromSaveData(object saveData)
    {
        if (saveData is BuildingSaveData buildingData)
        {
            currentHealth = buildingData.health;
            isOperational = buildingData.isOperational;
            isUnderConstruction = buildingData.isUnderConstruction;
            
            // Restore position
            transform.position = buildingData.position;
        }
    }

    #endregion
}