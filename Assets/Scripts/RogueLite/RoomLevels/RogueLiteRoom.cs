using UnityEngine;
using System.Collections.Generic;
using Managers;

public abstract class RogueLiteRoom : MonoBehaviour
{
    [Header("Room Components")]
    [SerializeField, ReadOnly] private List<RogueLikeRoomDoor> doors = new List<RogueLikeRoomDoor>();
    [SerializeField, ReadOnly] private List<ChestParent> chests = new List<ChestParent>();
    
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
    
    // Abstract property that concrete classes must implement
    public abstract RogueLikeRoomType RoomType { get; }
    
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
        
        // Call abstract method for room-specific initialization
        OnRoomAwake();
    }

    public virtual void Setup()
    {                
        // Initialize all doors
        foreach (var door in doors)
        {
            door.Initialize(this);
        }
        
        // Initialize all chests
        foreach (var chest in chests)
        {
            chest.SetupChest(GameManager.Instance.DifficultyManager.GetCurrentRoomDifficulty());
        }
        
        // Randomize props
        var propRandomizers = GetComponentsInChildren<PropRandomizer>();
        foreach (var propRandomizer in propRandomizers)
        {
            propRandomizer.RandomizeProps();
        }
        
        // Randomize materials
        var materialManager = GetComponent<RoomMaterialManager>();
        if (materialManager != null)
        {
            materialManager.RandomizeAllMaterials();
        }
        
        // Call abstract method for room-specific setup
        OnRoomSetup();
    }
    
    /// <summary>
    /// Abstract method for room-specific initialization during Awake
    /// </summary>
    protected abstract void OnRoomAwake();
    
    /// <summary>
    /// Abstract method for room-specific setup
    /// </summary>
    protected abstract void OnRoomSetup();
    
    /// <summary>
    /// Calculate the bounds of this room based on all its colliders
    /// </summary>
    public void CalculateRoomBounds()
    {
        // Debug warning if this is being called on a prefab
        if (gameObject.scene.name == null)
        {
            Debug.LogWarning($"[RogueLiteRoom] CalculateRoomBounds called on prefab {gameObject.name}! This will modify the prefab. Use CalculateTestBounds for testing.");
        }
        
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
    /// Calculate bounds for testing without modifying the original roomBounds field.
    /// Uses default rotation (no rotation applied).
    /// </summary>
    public Bounds CalculateTestBounds(Vector3 testPosition)
    {
        return CalculateTestBounds(testPosition, Quaternion.identity);
    }
    
    /// <summary>
    /// Calculate bounds for testing without modifying the original roomBounds field.
    /// Accounts for rotation by calculating the axis-aligned bounding box of the rotated colliders.
    /// </summary>
    public Bounds CalculateTestBounds(Vector3 testPosition, Quaternion testRotation)
    {
        // Refresh colliders in case they changed
        var testColliders = GetComponentsInChildren<Collider>();
        
        if (testColliders == null || testColliders.Length == 0)
        {
            return new Bounds(testPosition, Vector3.one * 10f);
        }
        
        // Calculate the rotation difference from current to test rotation
        Quaternion currentRotation = transform.rotation;
        Quaternion rotationDelta = testRotation * Quaternion.Inverse(currentRotation);
        
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;
        
        foreach (var collider in testColliders)
        {
            if (collider == null || !collider.enabled) continue;
            
            // Get the collider's current world bounds
            Bounds colliderBounds = collider.bounds;
            
            // Get the collider's position relative to this room's transform
            Vector3 relativeCenter = colliderBounds.center - transform.position;
            
            // Apply rotation to the relative center
            Vector3 rotatedRelativeCenter = rotationDelta * relativeCenter;
            
            // Calculate the new center at the test position
            Vector3 newCenter = testPosition + rotatedRelativeCenter;
            
            // For the size, we need to account for the rotation of the bounding box
            // The AABB size changes when rotated because it must stay axis-aligned
            Vector3 size = colliderBounds.size;
            Vector3 rotatedSize = CalculateRotatedAABBSize(size, rotationDelta);
            
            Bounds rotatedBounds = new Bounds(newCenter, rotatedSize);
            
            if (!boundsInitialized)
            {
                bounds = rotatedBounds;
                boundsInitialized = true;
            }
            else
            {
                bounds.Encapsulate(rotatedBounds);
            }
        }
        
        // Add padding to the bounds
        bounds.Expand(boundsPadding);
        
        return bounds;
    }
    
    /// <summary>
    /// Calculate the size of an axis-aligned bounding box after rotation.
    /// When a box is rotated, its AABB must grow to encompass all rotated corners.
    /// </summary>
    private Vector3 CalculateRotatedAABBSize(Vector3 originalSize, Quaternion rotation)
    {
        // Half extents of the original box
        Vector3 halfExtents = originalSize * 0.5f;
        
        // The 8 corners of the original box (relative to center)
        Vector3[] corners = new Vector3[8]
        {
            new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z),
            new Vector3(-halfExtents.x, -halfExtents.y,  halfExtents.z),
            new Vector3(-halfExtents.x,  halfExtents.y, -halfExtents.z),
            new Vector3(-halfExtents.x,  halfExtents.y,  halfExtents.z),
            new Vector3( halfExtents.x, -halfExtents.y, -halfExtents.z),
            new Vector3( halfExtents.x, -halfExtents.y,  halfExtents.z),
            new Vector3( halfExtents.x,  halfExtents.y, -halfExtents.z),
            new Vector3( halfExtents.x,  halfExtents.y,  halfExtents.z)
        };
        
        // Rotate each corner and find the new AABB
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        
        foreach (Vector3 corner in corners)
        {
            Vector3 rotatedCorner = rotation * corner;
            min = Vector3.Min(min, rotatedCorner);
            max = Vector3.Max(max, rotatedCorner);
        }
        
        return max - min;
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
        
        // Calculate the offset between the room's transform and its bounds center
        Vector3 boundsOffset = thisBounds.center - transform.position;
        
        // Apply this offset to the new position
        thisBounds.center = thisPosition + boundsOffset;
        
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
        
        // Calculate the offset between the room's transform and its bounds center
        Vector3 boundsOffset = thisBounds.center - transform.position;
        
        // Apply this offset to the new position
        thisBounds.center = thisPosition + boundsOffset;
        
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
    
    /// <summary>
    /// Draw an arrow gizmo to show position and rotation for parent connection
    /// </summary>
    private void DrawConnectionArrow(Vector3 position, Vector3 forward, float length)
    {
        // Draw the main arrow shaft
        Vector3 arrowEnd = position + forward * length;
        Gizmos.DrawLine(position, arrowEnd);
        
        // Draw arrow head
        float arrowHeadLength = length * 0.3f;
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        
        Vector3 arrowHeadRight = arrowEnd - forward * arrowHeadLength + right * arrowHeadLength * 0.5f;
        Vector3 arrowHeadLeft = arrowEnd - forward * arrowHeadLength - right * arrowHeadLength * 0.5f;
        
        Gizmos.DrawLine(arrowEnd, arrowHeadRight);
        Gizmos.DrawLine(arrowEnd, arrowHeadLeft);
        
        // Draw a small sphere at the base for better visibility
        Gizmos.DrawWireSphere(position, 0.3f);
    }
    
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
        Debug.Log($"Room Type: {room.RoomType}");
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
    
    protected virtual void OnDrawGizmos()
    {
        if (!showRoomBounds) return;

        // Draw an arrow to show position and rotation for matching with parent
        Gizmos.color = RoomType == RogueLikeRoomType.FRIENDLY ? Color.green : Color.cyan;
        DrawConnectionArrow(transform.position, transform.forward, 2f);
        
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
            Gizmos.color = RoomType == RogueLikeRoomType.FRIENDLY ? Color.green : Color.yellow;
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
            $"Room: {gameObject.name}\nType: {RoomType}\nStatus: {statusText}\nSize: {roomBounds.size}");
        #endif
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        if (!showRoomBounds) return;
        
        // Draw more detailed info when selected
        if (!boundsCalculated)
        {
            CalculateRoomBounds();
        }
        
        // Draw filled bounds with transparency when selected
        Color roomColor = RoomType == RogueLikeRoomType.FRIENDLY ? new Color(0, 1, 0, 0.1f) : new Color(1, 1, 0, 0.1f);
        Gizmos.color = roomColor;
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
