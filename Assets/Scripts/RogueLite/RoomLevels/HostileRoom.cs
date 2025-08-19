using UnityEngine;

/// <summary>
/// Hostile room where enemies spawn and combat occurs
/// </summary>
public class HostileRoom : RogueLiteRoom
{
    public override RogueLikeRoomType RoomType => RogueLikeRoomType.HOSTILE;
    
    protected override void OnRoomAwake()
    {
        // No longer need to refresh spawn points since we use random NavMesh spawning
        Debug.Log($"[HostileRoom] Hostile room '{gameObject.name}' awake complete.");
    }
    
    protected override void OnRoomSetup()
    {
        Debug.Log($"[HostileRoom] Hostile room '{gameObject.name}' setup complete.");
    }
    
} 