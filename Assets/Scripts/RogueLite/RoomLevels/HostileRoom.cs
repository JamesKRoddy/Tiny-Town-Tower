using UnityEngine;

/// <summary>
/// Hostile room where enemies spawn and combat occurs
/// </summary>
public class HostileRoom : RogueLiteRoom
{
    [Header("Hostile Room Settings")]
    [SerializeField] private bool showEnemySpawnGizmos = true;
    
    public override RogueLikeRoomType RoomType => RogueLikeRoomType.HOSTILE;
    
    protected override void OnRoomAwake()
    {
        // No longer need to refresh spawn points since we use random NavMesh spawning
    }
    
    protected override void OnRoomSetup()
    {
        Debug.Log($"[HostileRoom] Hostile room '{gameObject.name}' setup complete. Using random NavMesh spawning system.");
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showEnemySpawnGizmos) return;
        
        // Draw enemy spawn points
        // No legacy spawn points to draw
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showEnemySpawnGizmos) return;
        
        // Draw more detailed spawn point info when selected
        // No legacy spawn points to draw
    }
    #endif
} 