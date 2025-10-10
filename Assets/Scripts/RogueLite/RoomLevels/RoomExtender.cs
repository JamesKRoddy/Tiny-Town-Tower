using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A special room type that extends the building by adding additional spawn points
/// for more rooms to be placed. This allows for larger, more complex buildings.
/// </summary>
public class RoomExtender : RogueLiteRoom
{
    [Header("Room Extender Settings")]
    [SerializeField] private Transform additionalSpawnPointsParent;
    [Tooltip("If true, this extender can only spawn basic rooms (no other extenders)")]
    [SerializeField] private bool basicRoomsOnly = true;
    [Tooltip("Visual indicator for spawn points in the scene view")]
    [SerializeField] private bool showExtenderSpawnPoints = true;
    [SerializeField] private Color spawnPointGizmoColor = Color.magenta;
    
    [Header("Room Type (Set by Parent)")]
    [SerializeField, ReadOnly] private RogueLikeRoomType extenderRoomType = RogueLikeRoomType.HOSTILE;
    
    private Transform[] additionalSpawnPoints;
    private bool spawnPointsInitialized = false;
    
    /// <summary>
    /// Room extenders inherit their type from the parent building
    /// </summary>
    public override RogueLikeRoomType RoomType => extenderRoomType;
    
    /// <summary>
    /// Set the room type for this extender (called by RogueLiteRoomParent)
    /// </summary>
    public void SetRoomType(RogueLikeRoomType type)
    {
        extenderRoomType = type;
        Debug.Log($"[RoomExtender] {gameObject.name} room type set to: {type}");
    }
    
    /// <summary>
    /// Check if this extender has spawn points available
    /// </summary>
    public bool HasSpawnPoints()
    {
        InitializeSpawnPoints();
        return additionalSpawnPoints != null && additionalSpawnPoints.Length > 0;
    }
    
    /// <summary>
    /// Get the additional spawn points this extender provides
    /// </summary>
    public Transform[] GetAdditionalSpawnPoints()
    {
        InitializeSpawnPoints();
        return additionalSpawnPoints;
    }
    
    /// <summary>
    /// Whether this extender should only allow basic rooms (no other extenders)
    /// </summary>
    public bool BasicRoomsOnly => basicRoomsOnly;
    
    protected override void OnRoomAwake()
    {
        InitializeSpawnPoints();
        Debug.Log($"[RoomExtender] {gameObject.name} initialized with {(additionalSpawnPoints != null ? additionalSpawnPoints.Length : 0)} additional spawn points");
    }
    
    protected override void OnRoomSetup()
    {
        // Room extenders don't need special setup beyond what the base class does
        Debug.Log($"[RoomExtender] {gameObject.name} setup complete as {extenderRoomType} room");
    }
    
    private void InitializeSpawnPoints()
    {
        if (spawnPointsInitialized) return;
        
        if (additionalSpawnPointsParent != null)
        {
            additionalSpawnPoints = new Transform[additionalSpawnPointsParent.childCount];
            for (int i = 0; i < additionalSpawnPointsParent.childCount; i++)
            {
                additionalSpawnPoints[i] = additionalSpawnPointsParent.GetChild(i);
            }
            
            Debug.Log($"[RoomExtender] Found {additionalSpawnPoints.Length} spawn points in {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[RoomExtender] {gameObject.name} has no Additional Spawn Points Parent assigned!");
            additionalSpawnPoints = new Transform[0];
        }
        
        spawnPointsInitialized = true;
    }
    
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("CONTEXT/RoomExtender/Log Extender Info")]
    private static void LogExtenderInfo(UnityEditor.MenuCommand command)
    {
        RoomExtender extender = (RoomExtender)command.context;
        extender.InitializeSpawnPoints();
        
        Debug.Log($"[RoomExtender] === Extender Info for {extender.gameObject.name} ===");
        Debug.Log($"Room Type: {extender.extenderRoomType}");
        Debug.Log($"Basic Rooms Only: {extender.basicRoomsOnly}");
        Debug.Log($"Additional Spawn Points: {(extender.additionalSpawnPoints != null ? extender.additionalSpawnPoints.Length : 0)}");
        
        if (extender.additionalSpawnPoints != null)
        {
            for (int i = 0; i < extender.additionalSpawnPoints.Length; i++)
            {
                Transform spawn = extender.additionalSpawnPoints[i];
                if (spawn != null)
                {
                    Debug.Log($"  Spawn {i}: {spawn.name} at position {spawn.position}, forward: {spawn.forward}");
                }
            }
        }
    }
    #endif
    
    protected override void OnDrawGizmos()
    {
        // Draw base room gizmos first
        base.OnDrawGizmos();
        
        if (!showExtenderSpawnPoints) return;
        
        // Draw extender-specific gizmos
        if (additionalSpawnPointsParent != null)
        {
            Gizmos.color = spawnPointGizmoColor;
            
            for (int i = 0; i < additionalSpawnPointsParent.childCount; i++)
            {
                Transform spawnPoint = additionalSpawnPointsParent.GetChild(i);
                if (spawnPoint != null)
                {
                    // Draw sphere at spawn point
                    Gizmos.DrawWireSphere(spawnPoint.position, 1f);
                    
                    // Draw direction arrow
                    Vector3 forward = spawnPoint.forward * 3f;
                    Gizmos.DrawRay(spawnPoint.position, forward);
                    
                    // Draw arrow head
                    Vector3 right = spawnPoint.right * 0.5f;
                    Vector3 arrowEnd = spawnPoint.position + forward;
                    Gizmos.DrawLine(arrowEnd, arrowEnd - forward.normalized * 1f + right);
                    Gizmos.DrawLine(arrowEnd, arrowEnd - forward.normalized * 1f - right);
                    
                    #if UNITY_EDITOR
                    // Label
                    UnityEditor.Handles.color = spawnPointGizmoColor;
                    UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 2f, $"Extender Spawn {i}");
                    #endif
                }
            }
        }
    }
    
    protected override void OnDrawGizmosSelected()
    {
        // Draw base room selected gizmos
        base.OnDrawGizmosSelected();
        
        if (!showExtenderSpawnPoints) return;
        
        // Highlight spawn points when selected
        if (additionalSpawnPointsParent != null)
        {
            Gizmos.color = new Color(spawnPointGizmoColor.r, spawnPointGizmoColor.g, spawnPointGizmoColor.b, 0.3f);
            
            for (int i = 0; i < additionalSpawnPointsParent.childCount; i++)
            {
                Transform spawnPoint = additionalSpawnPointsParent.GetChild(i);
                if (spawnPoint != null)
                {
                    // Draw a larger, semi-transparent sphere
                    Gizmos.DrawSphere(spawnPoint.position, 1.5f);
                }
            }
        }
    }
}

