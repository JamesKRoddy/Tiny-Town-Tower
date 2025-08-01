using UnityEngine;
using Enemies;

/// <summary>
/// Hostile room where enemies spawn and combat occurs
/// </summary>
public class HostileRoom : RogueLiteRoom
{
    [Header("Hostile Room Settings")]
    [SerializeField] private EnemySpawnPoint[] enemySpawnPoints;
    [SerializeField] private bool showEnemySpawnGizmos = true;
    
    public override RogueLikeRoomType RoomType => RogueLikeRoomType.HOSTILE;
    
    protected override void OnRoomAwake()
    {
        RefreshEnemySpawnPoints();
    }
    
    /// <summary>
    /// Refresh the cached enemy spawn points (useful for editor and runtime)
    /// </summary>
    public void RefreshEnemySpawnPoints()
    {
        // Cache enemy spawn points
        enemySpawnPoints = GetComponentsInChildren<EnemySpawnPoint>();
        
        if (enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning($"[HostileRoom] No enemy spawn points found in hostile room: {gameObject.name}");
        }
    }
    
    protected override void OnRoomSetup()
    {
        // Initialize enemy spawn points
        foreach (var spawnPoint in enemySpawnPoints)
        {
            if (spawnPoint != null)
            {
                // Enemy spawn points are typically initialized by the EnemySpawnManager
                // but we can do any hostile room-specific setup here
            }
        }
        
        Debug.Log($"[HostileRoom] Hostile room '{gameObject.name}' setup complete with {enemySpawnPoints.Length} spawn points");
    }
    
    /// <summary>
    /// Get all enemy spawn points in this hostile room
    /// </summary>
    public EnemySpawnPoint[] GetEnemySpawnPoints()
    {
        // If cache is empty (common in editor), refresh it
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            RefreshEnemySpawnPoints();
        }
        
        return enemySpawnPoints ?? new EnemySpawnPoint[0];
    }
    
    /// <summary>
    /// Check if this room has any valid enemy spawn points
    /// </summary>
    public bool HasEnemySpawnPoints()
    {
        return enemySpawnPoints != null && enemySpawnPoints.Length > 0;
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showEnemySpawnGizmos) return;
        
        // Draw enemy spawn points
        if (enemySpawnPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (var spawnPoint in enemySpawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.transform.position, 0.5f);
                    Gizmos.DrawRay(spawnPoint.transform.position, Vector3.up * 2f);
                }
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showEnemySpawnGizmos) return;
        
        // Draw more detailed spawn point info when selected
        if (enemySpawnPoints != null)
        {
            for (int i = 0; i < enemySpawnPoints.Length; i++)
            {
                var spawnPoint = enemySpawnPoints[i];
                if (spawnPoint != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(spawnPoint.transform.position, 0.3f);
                    
                    #if UNITY_EDITOR
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(spawnPoint.transform.position + Vector3.up * 2.5f, 
                        $"Enemy Spawn {i + 1}");
                    #endif
                }
            }
        }
    }
    #endif
} 