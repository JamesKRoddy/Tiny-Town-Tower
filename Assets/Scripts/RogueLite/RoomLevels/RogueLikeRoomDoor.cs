using UnityEngine;
using Managers;

public class RogueLikeRoomDoor : RogueLiteDoor
{
    [Header("Target Room")]
    [Tooltip("The NEXT room this door leads to (forward progression only, no backtracking)")]
    public RogueLiteRoomParent targetRoom;
    [Tooltip("Where the player spawns in the target room")]
    public Transform targetSpawnPoint;
    
    [Header("Door Behavior")]
    [Tooltip("If true, this door exits to the overworld/camp instead of entering a new room")]
    [SerializeField] private bool exitToOverworld = false;
    
    [Tooltip("If true, this is the door the player entered from - stays locked to prevent backtracking")]
    [SerializeField] private bool isSpawnDoor = false;

    [Header("Spawn Validation")]
    [Tooltip("Radius to check for obstacles at spawn point")]
    [SerializeField] private float spawnCheckRadius = 1f;
    [Tooltip("Maximum distance to search for a clear spawn point")]
    [SerializeField] private float maxSearchDistance = 5f;
    [Tooltip("Number of positions to check in a circle pattern")]
    [SerializeField] private int searchPositions = 8;
    [Tooltip("Layers to check for obstacles")]
    [SerializeField] private LayerMask obstacleLayer = ~0; // All layers by default

    [Header("Door Placement Validation")]
    [Tooltip("Distance behind the door to check for floor (opposite side from player spawn)")]
    [SerializeField] private float floorCheckDistance = 1f;
    [Tooltip("Radius for floor detection raycast")]
    [SerializeField] private float floorCheckRadius = 0.5f;
    [Tooltip("Maximum downward distance to check for floor")]
    [SerializeField] private float floorRaycastDistance = 2f;
    [Tooltip("Layer mask for floor detection")]
    [SerializeField] private LayerMask floorLayer = ~0; // All layers by default

    private RogueLiteRoom parentRoom;

    protected override void Start()
    {
        base.Start();
        
        RogueLiteManager.Instance.OnEnemySetupStateChanged += OnEnemySetupStateChanged;
        
        // Check current state immediately in case it already changed before this door was ready
        EnemySetupState currentState = RogueLiteManager.Instance.GetEnemySetupState();
        OnEnemySetupStateChanged(currentState);
    }

    public void Initialize(RogueLiteRoom room)
    {
        parentRoom = room;
    }
    
    /// <summary>
    /// Set whether this door exits to the overworld when used
    /// </summary>
    public void SetExitToOverworld(bool shouldExit)
    {
        exitToOverworld = shouldExit;
    }
    
    /// <summary>
    /// Mark this door as the spawn door (where player entered from) - stays locked to prevent backtracking
    /// </summary>
    public void SetAsSpawnDoor(bool isSpawn)
    {
        isSpawnDoor = isSpawn;
    }

    private void OnEnemySetupStateChanged(EnemySetupState state)
    {
        if(state == EnemySetupState.ALL_WAVES_CLEARED)
        {
            // Unlock all locked doors EXCEPT the spawn door (prevents backtracking)
            if (doorType == DoorStatus.LOCKED && !isSpawnDoor)
            {
                doorType = DoorStatus.UNLOCKED;
                isLocked = false; // Update the cached locked state!
                Debug.Log($"[RogueLikeRoomDoor] Unlocked progression door '{gameObject.name}' after clearing waves");
            }
            ShowDoorEffects();
        }
        else
        {
            HideDoorEffects();
        }
    }

    public override void OnDoorEntered()
    {
        // Only allow interaction if door is unlocked
        if (doorType == DoorStatus.LOCKED) 
        {
            Debug.Log($"[RogueLikeRoomDoor] Door '{gameObject.name}' is locked - cannot enter");
            return;
        }

        Debug.Log($"[RogueLikeRoomDoor] Entering door '{gameObject.name}' (exitToOverworld: {exitToOverworld})");

        // Check if this door should exit to overworld (boss room completion, building end, etc.)
        if (exitToOverworld)
        {
            // Exit to overworld (like finishing a building)
            // Player keeps their NPC and inventory - still in ROGUE_LITE mode
            RogueLiteManager.Instance.OverworldManager.ExitedBuilding();
        }
        else
        {
            // Enter the next room
            RogueLiteManager.Instance.EnterRoomWithTransition(this);
        }
    }

    public override bool CanInteract()
    {
        EnemySetupState currentState = RogueLiteManager.Instance.GetEnemySetupState();
        bool wavesCleared = currentState == EnemySetupState.ALL_WAVES_CLEARED;
        bool baseCanInteract = base.CanInteract();
        bool doorUnlocked = doorType == DoorStatus.UNLOCKED;
        
        // Door can be used when waves are cleared AND door is unlocked
        return wavesCleared && baseCanInteract && doorUnlocked;
    }

    public override string GetInteractionText()
    {
        if (doorType == DoorStatus.LOCKED)
        {
            return "Door Locked";
        }
        
        // Unlocked doors show different text based on their purpose
        return exitToOverworld ? "Exit to Overworld" : "Enter Room";
    }

    /// <summary>
    /// Gets a valid spawn position, checking if the target spawn point is blocked.
    /// If blocked, searches for a nearby clear position.
    /// </summary>
    /// <returns>A clear spawn position, or the original position if no obstacles found</returns>
    public Vector3 GetValidSpawnPosition()
    {
        if (targetSpawnPoint == null)
        {
            Debug.LogWarning($"Door {gameObject.name} has no target spawn point set!");
            return Vector3.zero;
        }

        Vector3 targetPosition = targetSpawnPoint.position;

        // Check if the target spawn point is clear
        if (IsPositionClear(targetPosition))
        {
            return targetPosition;
        }

        Debug.LogWarning($"Spawn point at {gameObject.name} is blocked! Searching for clear position...");

        // Search for a clear position in a circular pattern
        Vector3 clearPosition = FindClearPosition(targetPosition);
        
        if (clearPosition != Vector3.zero)
        {
            Debug.Log($"Found clear spawn position at distance {Vector3.Distance(targetPosition, clearPosition):F2}m from original spawn point");
            return clearPosition;
        }

        // If no clear position found, return original position and log error
        Debug.LogError($"Could not find clear spawn position near {gameObject.name}! Using original position anyway.");
        return targetPosition;
    }

    /// <summary>
    /// Checks if a position is clear of obstacles
    /// </summary>
    private bool IsPositionClear(Vector3 position)
    {
        // Check for colliders at the position
        Collider[] colliders = Physics.OverlapSphere(position, spawnCheckRadius, obstacleLayer);
        
        // Filter out trigger colliders as they shouldn't block spawning
        foreach (Collider col in colliders)
        {
            if (!col.isTrigger)
            {
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Searches for a clear position in a circular pattern around the target position
    /// </summary>
    private Vector3 FindClearPosition(Vector3 centerPosition)
    {
        // Try positions in expanding circles
        int rings = 3; // Number of rings to search
        float ringSpacing = maxSearchDistance / rings;

        for (int ring = 1; ring <= rings; ring++)
        {
            float currentRadius = ring * ringSpacing;
            
            for (int i = 0; i < searchPositions; i++)
            {
                float angle = (360f / searchPositions) * i;
                float radians = angle * Mathf.Deg2Rad;
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(radians) * currentRadius,
                    0,
                    Mathf.Sin(radians) * currentRadius
                );
                
                Vector3 testPosition = centerPosition + offset;
                
                if (IsPositionClear(testPosition))
                {
                    return testPosition;
                }
            }
        }

        return Vector3.zero; // No clear position found
    }

    /// <summary>
    /// Validates if this door has a valid floor behind it (opposite side from player spawn).
    /// This prevents doors on walls in the middle of rooms from being enabled.
    /// </summary>
    /// <returns>True if there's a valid floor behind the door, false otherwise</returns>
    public bool HasValidFloorBehindDoor()
    {
        if (playerSpawn == null)
        {
            Debug.LogWarning($"Door {gameObject.name} has no player spawn point set!");
            return false;
        }

        // Calculate the direction from player spawn to door (this is the "forward" direction)
        Vector3 doorPosition = transform.position;
        Vector3 spawnPosition = playerSpawn.position;
        Vector3 doorToSpawn = (spawnPosition - doorPosition).normalized;

        // Calculate position behind the door (opposite from player spawn)
        Vector3 behindDoorPosition = doorPosition - (doorToSpawn * floorCheckDistance);
        
        // Perform a spherecast downward to check for floor
        RaycastHit hit;
        Vector3 rayStart = behindDoorPosition + Vector3.up * 0.5f; // Start slightly above to account for door height
        
        if (Physics.SphereCast(rayStart, floorCheckRadius, Vector3.down, out hit, floorRaycastDistance, floorLayer))
        {
            // Check if we hit a non-trigger collider (actual floor)
            if (!hit.collider.isTrigger)
            {
                return true;
            }
        }

        Debug.LogWarning($"Door {gameObject.name} at {doorPosition} has no valid floor behind it! " +
                        $"Check position: {behindDoorPosition}, Ray start: {rayStart}");
        return false;
    }

    /// <summary>
    /// Draws debug gizmos to visualize spawn point validation
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (targetSpawnPoint == null) return;

        Vector3 spawnPos = targetSpawnPoint.position;

        // Draw the spawn check radius
        Gizmos.color = IsPositionClear(spawnPos) ? Color.green : Color.red;
        Gizmos.DrawWireSphere(spawnPos, spawnCheckRadius);

        // Draw the search area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnPos, maxSearchDistance);

        // Draw search positions
        Gizmos.color = Color.cyan;
        int rings = 3;
        float ringSpacing = maxSearchDistance / rings;

        for (int ring = 1; ring <= rings; ring++)
        {
            float currentRadius = ring * ringSpacing;
            
            for (int i = 0; i < searchPositions; i++)
            {
                float angle = (360f / searchPositions) * i;
                float radians = angle * Mathf.Deg2Rad;
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(radians) * currentRadius,
                    0,
                    Mathf.Sin(radians) * currentRadius
                );
                
                Vector3 testPosition = spawnPos + offset;
                Gizmos.DrawWireSphere(testPosition, 0.2f);
            }
        }

        // Draw floor validation check
        if (playerSpawn != null)
        {
            Vector3 doorPosition = transform.position;
            Vector3 spawnPosition = playerSpawn.position;
            Vector3 doorToSpawn = (spawnPosition - doorPosition).normalized;
            Vector3 behindDoorPosition = doorPosition - (doorToSpawn * floorCheckDistance);
            Vector3 rayStart = behindDoorPosition + Vector3.up * 0.5f;

            // Draw the check position
            bool hasFloor = HasValidFloorBehindDoor();
            Gizmos.color = hasFloor ? Color.green : Color.red;
            Gizmos.DrawWireSphere(behindDoorPosition, floorCheckRadius);
            
            // Draw the raycast line
            Gizmos.color = hasFloor ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * floorRaycastDistance);
            
            // Draw arrow pointing to check position
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(doorPosition, behindDoorPosition);
        }
    }

    void OnDestroy()
    {
        if (RogueLiteManager.Instance != null)
        {
            RogueLiteManager.Instance.OnEnemySetupStateChanged -= OnEnemySetupStateChanged;
        }
    }
} 