using UnityEngine;
using System;
using Enemies;
using System.Collections;

/// <summary>
/// A wall building that provides defensive barriers against zombie attacks.
/// Walls can be damaged and destroyed by enemies, and can be repaired and upgraded.
/// </summary>
public class WallBuilding : Building
{
    [Header("Wall Settings")]
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float wallThickness = 0.5f;
    
    [Header("Defense Parameters")]
    [SerializeField] private float damageReduction = 0.5f; // Reduces damage taken by this percentage
    [SerializeField] private bool blocksEnemyMovement = true; // Whether enemies can pass through when destroyed
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject wallModel;
    [SerializeField] private GameObject damagedWallModel;
    [SerializeField] private GameObject destroyedWallModel;
    
    // Events
    public event Action<WallBuilding> OnWallDestroyed;
    public event Action<WallBuilding> OnWallRepaired;
    
    public float WallHeight => wallHeight;
    public float WallThickness => wallThickness;
    public bool IsDestroyed => !IsOperational(); // Use base class operational state
    public bool BlocksEnemyMovement => blocksEnemyMovement && IsOperational(); // Use base class operational state
    
    // Flag to prevent targeting during destruction
    private bool isBeingDestroyed = false;
    public bool IsBeingDestroyed => isBeingDestroyed;
    
    protected override void Start()
    {
        base.Start();
        UpdateWallVisuals();
    }
    
    public override void SetupBuilding(BuildingScriptableObj buildingScriptableObj)
    {
        base.SetupBuilding(buildingScriptableObj);
        
        // Setup wall-specific properties
        if (buildingScriptableObj is WallBuildingScriptableObj wallData)
        {
            wallHeight = wallData.wallHeight;
            wallThickness = wallData.wallThickness;
            damageReduction = wallData.damageReduction;
            blocksEnemyMovement = wallData.blocksEnemyMovement;
        }
    }
    
    public override void TakeDamage(float amount, Transform damageSource = null)
    {
        // Apply damage reduction
        float reducedDamage = amount * (1f - damageReduction);
        
        // Call base TakeDamage with reduced damage
        base.TakeDamage(reducedDamage, damageSource);
        
        // Update visuals based on health state
        UpdateWallVisuals();
    }
    
    public override void Heal(float healAmount)
    {
        bool wasDestroyed = !IsOperational();
        
        base.Heal(healAmount);
        
        // If wall was destroyed and now has health, restore it
        if (wasDestroyed && IsOperational())
        {
            RestoreWall();
        }
        
        UpdateWallVisuals();
    }
    
    public override void Die()
    {
        // Mark as being destroyed to prevent targeting
        isBeingDestroyed = true;
        
        // Wall-specific destruction logic
        OnWallDestroyed?.Invoke(this);
        
        // Notify all enemies that this wall was destroyed
        EnemyBase.NotifyTargetDestroyed(transform);
        
        // Disable NavMeshObstacle when destroyed
        var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (obstacle != null)
        {
            obstacle.enabled = false;
            // Force NavMesh update after a short delay
            StartCoroutine(UpdateNavMeshAfterDestruction());
        }
        
        Debug.Log($"Wall at {transform.position} has been destroyed!");
        
        // Call base Die method to handle building destruction
        base.Die();
    }
    
    private System.Collections.IEnumerator UpdateNavMeshAfterDestruction()
    {
        // Wait a few frames to ensure the obstacle is disabled and NavMesh can update
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log($"NavMesh should be updated after wall destruction at {transform.position}");
    }
    
    private void RestoreWall()
    {
        OnWallRepaired?.Invoke(this);
        
        // Re-enable NavMeshObstacle when restored
        var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (obstacle != null)
        {
            obstacle.enabled = true;
        }
        
        Debug.Log($"Wall at {transform.position} has been restored!");
    }
    
    private void UpdateWallVisuals()
    {
        bool isOperational = IsOperational();
        float healthPercentage = GetHealthPercentage();
        
        if (wallModel != null) wallModel.SetActive(isOperational && healthPercentage > 0.5f);
        if (damagedWallModel != null) damagedWallModel.SetActive(isOperational && healthPercentage <= 0.5f && healthPercentage > 0);
        if (destroyedWallModel != null) destroyedWallModel.SetActive(!isOperational);
    }
    
    /// <summary>
    /// Check if a position is blocked by this wall
    /// </summary>
    public bool IsBlockingPosition(Vector3 position)
    {
        if (!IsOperational() || !blocksEnemyMovement)
            return false;
            
        // Simple distance check - can be improved with more sophisticated collision detection
        float distance = Vector3.Distance(transform.position, position);
        return distance <= wallThickness;
    }
    
    /// <summary>
    /// Get the closest point on the wall to a given position
    /// </summary>
    public Vector3 GetClosestPoint(Vector3 position)
    {
        Vector3 direction = (position - transform.position).normalized;
        direction.y = 0; // Keep it on the ground plane
        return transform.position + direction * wallThickness;
    }
}

/// <summary>
/// ScriptableObject for wall building data
/// </summary>
[CreateAssetMenu(fileName = "WallBuildingScriptableObj", menuName = "Scriptable Objects/Camp/WallBuildingScriptableObj")]
public class WallBuildingScriptableObj : BuildingScriptableObj
{
    [Header("Wall Properties")]
    public float wallHeight = 3f;
    public float wallThickness = 0.5f;
    public float damageReduction = 0.5f;
    public bool blocksEnemyMovement = true;
} 