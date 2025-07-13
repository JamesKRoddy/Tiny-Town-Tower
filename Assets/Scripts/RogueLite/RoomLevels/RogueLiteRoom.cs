using UnityEngine;
using System.Collections.Generic;
using Managers;

public class RogueLiteRoom : MonoBehaviour
{
    [Header("Room Components")]
    public List<RogueLikeRoomDoor> doors = new List<RogueLikeRoomDoor>();
    public List<ChestParent> chests = new List<ChestParent>();
    
    [Header("Room Bounds")]
    [SerializeField] private Bounds roomBounds;
    [SerializeField] private bool autoCalculateBounds = true;
    [SerializeField] private float boundsPadding = 2f;
    [SerializeField] private bool showRoomBounds = true;
    
    [Header("Room Connection Settings")]
    [SerializeField] private float connectionTolerance = 15f; // Allow up to 15% overlap for connections
    [SerializeField] private bool showConnectionDebug = false;
    
    private Collider[] roomColliders;
    private bool boundsCalculated = false;
    
    private void Awake()
    {
        // Cache all doors and chests in the room
        doors.AddRange(GetComponentsInChildren<RogueLikeRoomDoor>());
        chests.AddRange(GetComponentsInChildren<ChestParent>());
        
        // Cache room colliders for bounds calculation
        roomColliders = GetComponentsInChildren<Collider>();
        
        if (autoCalculateBounds)
        {
            CalculateRoomBounds();
        }
    }

    public void Setup()
    {                
        // Initialize all doors
        foreach (var door in doors)
        {
            door.Initialize(this);
        }
        
        // Initialize all chests
        foreach (var chest in chests)
        {
            chest.SetupChest(DifficultyManager.Instance.GetCurrentRoomDifficulty());
        }
    }
    
    /// <summary>
    /// Calculate the bounds of this room based on all its colliders
    /// </summary>
    public void CalculateRoomBounds()
    {
        // Refresh colliders in case they changed
        roomColliders = GetComponentsInChildren<Collider>();
        
        if (roomColliders == null || roomColliders.Length == 0)
        {
            roomBounds = new Bounds(transform.position, Vector3.one * 10f);
            boundsCalculated = true;
            return;
        }
        
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;
        
        foreach (var collider in roomColliders)
        {
            if (collider == null || !collider.enabled) continue;
            
            if (!boundsInitialized)
            {
                bounds = collider.bounds;
                boundsInitialized = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }
        
        // Add padding to the bounds
        bounds.Expand(boundsPadding);
        roomBounds = bounds;
        boundsCalculated = true;
    }
    
    /// <summary>
    /// Get the room's bounds in world space
    /// </summary>
    public Bounds GetWorldBounds()
    {
        if (!boundsCalculated)
        {
            CalculateRoomBounds();
        }
        return roomBounds;
    }
    
    /// <summary>
    /// Check if this room would overlap excessively with another room at the given position
    /// </summary>
    public bool WouldOverlapWith(RogueLiteRoom otherRoom, Vector3 thisPosition)
    {
        if (otherRoom == null) return false;
        
        Bounds thisBounds = GetWorldBounds();
        thisBounds.center = thisPosition;
        
        Bounds otherBounds = otherRoom.GetWorldBounds();
        
        // Allow some overlap for connections
        if (thisBounds.Intersects(otherBounds))
        {
            float overlapPercentage = CalculateOverlapPercentage(thisBounds, otherBounds);
            bool wouldOverlap = overlapPercentage > connectionTolerance;
            
            if (showConnectionDebug)
            {
                Debug.Log($"[RoomConnection] {gameObject.name} vs {otherRoom.gameObject.name}: {overlapPercentage:F1}% overlap " +
                         $"(tolerance: {connectionTolerance}%) -> {(wouldOverlap ? "BLOCKED" : "ALLOWED")}");
            }
            
            return wouldOverlap;
        }
        
        return false; // No intersection at all
    }
    
    /// <summary>
    /// Check if this room would overlap excessively with any bounds at the given position
    /// </summary>
    public bool WouldOverlapWith(Bounds otherBounds, Vector3 thisPosition)
    {
        Bounds thisBounds = GetWorldBounds();
        thisBounds.center = thisPosition;
        
        // Allow some overlap for connections
        if (thisBounds.Intersects(otherBounds))
        {
            float overlapPercentage = CalculateOverlapPercentage(thisBounds, otherBounds);
            return overlapPercentage > connectionTolerance;
        }
        
        return false; // No intersection at all
    }
    
    /// <summary>
    /// Calculate the percentage of overlap between two bounds
    /// </summary>
    private float CalculateOverlapPercentage(Bounds bounds1, Bounds bounds2)
    {
        // Calculate the intersection bounds
        Vector3 intersectionMin = Vector3.Max(bounds1.min, bounds2.min);
        Vector3 intersectionMax = Vector3.Min(bounds1.max, bounds2.max);
        
        // If no intersection, return 0
        if (intersectionMin.x >= intersectionMax.x || 
            intersectionMin.y >= intersectionMax.y || 
            intersectionMin.z >= intersectionMax.z)
        {
            return 0f;
        }
        
        // Calculate intersection volume
        Vector3 intersectionSize = intersectionMax - intersectionMin;
        float intersectionVolume = intersectionSize.x * intersectionSize.y * intersectionSize.z;
        
        // Calculate volumes of both bounds
        float volume1 = bounds1.size.x * bounds1.size.y * bounds1.size.z;
        float volume2 = bounds2.size.x * bounds2.size.y * bounds2.size.z;
        
        // Calculate overlap percentage relative to the smaller room
        float smallerVolume = Mathf.Min(volume1, volume2);
        float overlapPercentage = (intersectionVolume / smallerVolume) * 100f;
        
        return overlapPercentage;
    }
    
    /// <summary>
    /// Get the minimum distance needed from another room to avoid overlap
    /// </summary>
    public float GetMinDistanceFrom(RogueLiteRoom otherRoom)
    {
        if (otherRoom == null) return 0f;
        
        Bounds thisBounds = GetWorldBounds();
        Bounds otherBounds = otherRoom.GetWorldBounds();
        
        // Calculate the minimum distance needed in each axis
        float minX = (thisBounds.size.x + otherBounds.size.x) * 0.5f;
        float minZ = (thisBounds.size.z + otherBounds.size.z) * 0.5f;
        
        // Return the largest distance needed
        return Mathf.Max(minX, minZ);
    }
    
    // Public methods for inspector debugging
    public bool GetBoundsCalculated() => boundsCalculated;
    public Collider[] GetRoomColliders() => roomColliders;
    
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("CONTEXT/RogueLiteRoom/Recalculate Bounds")]
    private static void RecalculateBounds(UnityEditor.MenuCommand command)
    {
        RogueLiteRoom room = (RogueLiteRoom)command.context;
        room.boundsCalculated = false;
        room.CalculateRoomBounds();
        Debug.Log($"[RogueLiteRoom] Manually recalculated bounds for {room.gameObject.name}");
    }
    
    [UnityEditor.MenuItem("CONTEXT/RogueLiteRoom/Log Room Info")]
    private static void LogRoomInfo(UnityEditor.MenuCommand command)
    {
        RogueLiteRoom room = (RogueLiteRoom)command.context;
        Debug.Log($"[RogueLiteRoom] === Room Info for {room.gameObject.name} ===");
        Debug.Log($"Bounds Calculated: {room.boundsCalculated}");
        Debug.Log($"Auto Calculate: {room.autoCalculateBounds}");
        Debug.Log($"Bounds Padding: {room.boundsPadding}");
        Debug.Log($"Current Bounds: Center={room.roomBounds.center}, Size={room.roomBounds.size}");
        Debug.Log($"Colliders Found: {(room.roomColliders != null ? room.roomColliders.Length : 0)}");
        
        if (room.roomColliders != null)
        {
            for (int i = 0; i < room.roomColliders.Length; i++)
            {
                var collider = room.roomColliders[i];
                if (collider != null)
                {
                    Debug.Log($"  Collider {i}: {collider.name} (enabled: {collider.enabled}) bounds: {collider.bounds}");
                }
            }
        }
    }
    #endif
    
    private void OnDrawGizmos()
    {
        if (!showRoomBounds) return;

        // Always draw a basic gizmo to show the room exists
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // Try to calculate bounds if not done yet (works in both edit and play mode)
        if (!boundsCalculated)
        {
            try
            {
                CalculateRoomBounds();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RogueLiteRoom] Error calculating bounds for {gameObject.name}: {e.Message}");
            }
        }
        
        // Draw room bounds (always visible)
        if (boundsCalculated)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(roomBounds.center, roomBounds.size);
        }
        else
        {
            // Draw fallback bounds if calculation failed
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 10f);
        }
        
        // Draw bounds info
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        string statusText = boundsCalculated ? "✓ Calculated" : "✗ Not Calculated";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, 
            $"Room: {gameObject.name}\nStatus: {statusText}\nSize: {roomBounds.size}");
        #endif
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showRoomBounds) return;
        
        // Draw more detailed info when selected
        if (!boundsCalculated)
        {
            CalculateRoomBounds();
        }
        
        // Draw filled bounds with transparency when selected
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawCube(roomBounds.center, roomBounds.size);
        
        // Draw individual collider bounds
        if (roomColliders != null)
        {
            Gizmos.color = Color.red;
            foreach (var collider in roomColliders)
            {
                if (collider != null && collider.enabled)
                {
                    Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
                }
            }
        }
        
        // Draw padding visualization
        Gizmos.color = Color.blue;
        Bounds unpaddedBounds = roomBounds;
        unpaddedBounds.Expand(-boundsPadding);
        Gizmos.DrawWireCube(unpaddedBounds.center, unpaddedBounds.size);
    }
}
