using UnityEngine;
using Managers;

/// <summary>
/// Door types that determine both visual appearance and unlock behavior
/// </summary>
public enum RogueLiteDoorType
{
    /// <summary>
    /// Where player entered from. Shows wall with hole but stays LOCKED forever (no backtracking).
    /// </summary>
    SPAWN,
    
    /// <summary>
    /// Exit to next room. Shows wall with hole, starts LOCKED, unlocks when enemies are defeated.
    /// </summary>
    PROGRESSION,
    
    /// <summary>
    /// Dead end / blocked path. Shows solid wall, permanently LOCKED forever.
    /// </summary>
    DEAD_END
}

public class RogueLikeRoomDoor : RogueLiteDoor
{
    [Header("Target Room")]
    [Tooltip("The NEXT room this door leads to (forward progression only, no backtracking)")]
    public RogueLiteRoomParent targetRoom;
    [Tooltip("Where the player spawns in the target room")]
    public Transform targetSpawnPoint;
    
    [Header("Door Type")]
    [Tooltip("SPAWN = entry point (locked, shows hole)\nPROGRESSION = exit (unlocks after enemies, shows hole)\nDEAD_END = blocked (locked forever, shows solid wall)")]
    [SerializeField] private RogueLiteDoorType rogueLiteDoorType = RogueLiteDoorType.PROGRESSION;
    
    [Header("Door Behavior")]
    [Tooltip("If true, this door exits to the overworld/camp instead of entering a new room")]
    [SerializeField] private bool exitToOverworld = false;

    [Header("Door Models - REQUIRED")]
    [Tooltip("Model shown for SPAWN and PROGRESSION doors (wall with hole/opening)")]
    [SerializeField] private GameObject progressionDoorModel;
    
    [Tooltip("Model shown for DEAD_END doors (solid wall/blocked)")]
    [SerializeField] private GameObject deadEndModel;

    [Header("Spawn Validation")]
    [Tooltip("Radius to check for obstacles at spawn point")]
    [SerializeField] private float spawnCheckRadius = 1f;
    [Tooltip("Maximum distance to search for a clear spawn point")]
    [SerializeField] private float maxSearchDistance = 5f;
    [Tooltip("Number of positions to check in a circle pattern")]
    [SerializeField] private int searchPositions = 8;
    [Tooltip("Layers to check for obstacles")]
    [SerializeField] private LayerMask obstacleLayer = ~0;

    [Header("Door Placement Validation")]
    [Tooltip("Distance behind the door to check for floor")]
    [SerializeField] private float floorCheckDistance = 1f;
    [Tooltip("Radius for floor detection raycast")]
    [SerializeField] private float floorCheckRadius = 0.5f;
    [Tooltip("Maximum downward distance to check for floor")]
    [SerializeField] private float floorRaycastDistance = 2f;
    [Tooltip("Layer mask for floor detection")]
    [SerializeField] private LayerMask floorLayer = ~0;

    private RogueLiteRoom parentRoom;

    protected override void Start()
    {
        base.Start();
        
        // Apply door models based on type
        ApplyDoorType();
        
        // Subscribe to enemy state changes
        RogueLiteManager.Instance.OnEnemySetupStateChanged += OnEnemySetupStateChanged;
        
        // Check current state immediately
        EnemySetupState currentState = RogueLiteManager.Instance.GetEnemySetupState();
        OnEnemySetupStateChanged(currentState);
    }

    public void Initialize(RogueLiteRoom room)
    {
        parentRoom = room;
    }
    
    /// <summary>
    /// Sets the door type and updates visuals accordingly.
    /// Call this during room setup to configure the door.
    /// </summary>
    public void SetDoorType(RogueLiteDoorType type)
    {
        rogueLiteDoorType = type;
        ApplyDoorType();
        Debug.Log($"[Door] '{gameObject.name}' set to {type}");
    }
    
    /// <summary>
    /// Gets the current door type
    /// </summary>
    public RogueLiteDoorType GetDoorType()
    {
        return rogueLiteDoorType;
    }
    
    /// <summary>
    /// Applies the door type - sets models and lock state
    /// </summary>
    private void ApplyDoorType()
    {
        // All doors start LOCKED
        doorType = DoorStatus.LOCKED;
        isLocked = true;
        
        // Set models based on door type
        // SPAWN and PROGRESSION show the progression model (wall with hole)
        // DEAD_END shows the dead end model (solid wall)
        bool showProgressionModel = (rogueLiteDoorType == RogueLiteDoorType.SPAWN || 
                                     rogueLiteDoorType == RogueLiteDoorType.PROGRESSION);
        bool showDeadEndModel = (rogueLiteDoorType == RogueLiteDoorType.DEAD_END);
        
        if (progressionDoorModel != null)
        {
            progressionDoorModel.SetActive(showProgressionModel);
        }
        
        if (deadEndModel != null)
        {
            deadEndModel.SetActive(showDeadEndModel);
        }
        
        // Log warning if models aren't assigned
        if (progressionDoorModel == null && deadEndModel == null)
        {
            Debug.LogWarning($"[Door] '{gameObject.name}' has no model references! Assign progressionDoorModel and deadEndModel in prefab.");
        }
    }
    
    /// <summary>
    /// Set whether this door exits to the overworld when used
    /// </summary>
    public void SetExitToOverworld(bool shouldExit)
    {
        exitToOverworld = shouldExit;
    }

    private void OnEnemySetupStateChanged(EnemySetupState state)
    {
        if (state == EnemySetupState.ALL_WAVES_CLEARED)
        {
            // Only PROGRESSION doors unlock when enemies are cleared
            if (rogueLiteDoorType == RogueLiteDoorType.PROGRESSION)
            {
                doorType = DoorStatus.UNLOCKED;
                isLocked = false;
                Debug.Log($"[Door] '{gameObject.name}' UNLOCKED (enemies cleared)");
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
        if (doorType == DoorStatus.LOCKED) 
        {
            Debug.Log($"[Door] '{gameObject.name}' is locked - cannot enter");
            return;
        }

        Debug.Log($"[Door] Entering '{gameObject.name}' (exitToOverworld: {exitToOverworld})");

        if (exitToOverworld)
        {
            RogueLiteManager.Instance.OverworldManager.ExitedBuilding();
        }
        else
        {
            RogueLiteManager.Instance.EnterRoomWithTransition(this);
        }
    }

    public override bool CanInteract()
    {
        EnemySetupState currentState = RogueLiteManager.Instance.GetEnemySetupState();
        bool wavesCleared = currentState == EnemySetupState.ALL_WAVES_CLEARED;
        bool doorUnlocked = doorType == DoorStatus.UNLOCKED;
        
        return wavesCleared && doorUnlocked;
    }

    public override string GetInteractionText()
    {
        if (doorType == DoorStatus.LOCKED)
        {
            return rogueLiteDoorType == RogueLiteDoorType.DEAD_END ? "Blocked" : "Door Locked";
        }
        
        return exitToOverworld ? "Exit to Overworld" : "Enter Room";
    }

    /// <summary>
    /// Gets a valid spawn position, checking if the target spawn point is blocked.
    /// </summary>
    public Vector3 GetValidSpawnPosition()
    {
        if (targetSpawnPoint == null)
        {
            Debug.LogWarning($"Door {gameObject.name} has no target spawn point set!");
            return Vector3.zero;
        }

        Vector3 targetPosition = targetSpawnPoint.position;

        if (IsPositionClear(targetPosition))
        {
            return targetPosition;
        }

        Debug.LogWarning($"Spawn point at {gameObject.name} is blocked! Searching for clear position...");

        Vector3 clearPosition = FindClearPosition(targetPosition);
        
        if (clearPosition != Vector3.zero)
        {
            Debug.Log($"Found clear spawn position at distance {Vector3.Distance(targetPosition, clearPosition):F2}m from original spawn point");
            return clearPosition;
        }

        Debug.LogError($"Could not find clear spawn position near {gameObject.name}! Using original position anyway.");
        return targetPosition;
    }

    private bool IsPositionClear(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, spawnCheckRadius, obstacleLayer);
        
        foreach (Collider col in colliders)
        {
            if (!col.isTrigger)
            {
                return false;
            }
        }
        
        return true;
    }

    private Vector3 FindClearPosition(Vector3 centerPosition)
    {
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
                
                Vector3 testPosition = centerPosition + offset;
                
                if (IsPositionClear(testPosition))
                {
                    return testPosition;
                }
            }
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Validates if this door has a valid floor behind it.
    /// </summary>
    public bool HasValidFloorBehindDoor()
    {
        if (playerSpawn == null)
        {
            Debug.LogWarning($"Door {gameObject.name} has no player spawn point set!");
            return false;
        }

        Vector3 doorPosition = transform.position;
        Vector3 spawnPosition = playerSpawn.position;
        Vector3 doorToSpawn = (spawnPosition - doorPosition).normalized;
        Vector3 behindDoorPosition = doorPosition - (doorToSpawn * floorCheckDistance);
        
        RaycastHit hit;
        Vector3 rayStart = behindDoorPosition + Vector3.up * 0.5f;
        
        if (Physics.SphereCast(rayStart, floorCheckRadius, Vector3.down, out hit, floorRaycastDistance, floorLayer))
        {
            if (!hit.collider.isTrigger)
            {
                return true;
            }
        }

        Debug.LogWarning($"Door {gameObject.name} at {doorPosition} has no valid floor behind it!");
        return false;
    }

    /// <summary>
    /// Always visible gizmo showing door type
    /// </summary>
    private void OnDrawGizmos()
    {
        Vector3 pos = transform.position + Vector3.up * 2f;
        
        // Color based on door type
        switch (rogueLiteDoorType)
        {
            case RogueLiteDoorType.SPAWN:
                Gizmos.color = Color.blue;
                break;
            case RogueLiteDoorType.PROGRESSION:
                Gizmos.color = Color.green;
                break;
            case RogueLiteDoorType.DEAD_END:
                Gizmos.color = Color.red;
                break;
        }
        
        // Draw a cube above the door
        Gizmos.DrawCube(pos, Vector3.one * 0.5f);
        Gizmos.DrawWireCube(pos, Vector3.one * 0.6f);
        
        // Draw arrow pointing in door's forward direction
        Vector3 arrowStart = transform.position + Vector3.up * 0.5f;
        Vector3 arrowEnd = arrowStart + transform.forward * 1.5f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        Gizmos.DrawSphere(arrowEnd, 0.15f);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw door type label using Handles (requires UnityEditor)
        #if UNITY_EDITOR
        Vector3 labelPos = transform.position + Vector3.up * 2.8f;
        string label = rogueLiteDoorType.ToString();
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = rogueLiteDoorType switch
        {
            RogueLiteDoorType.SPAWN => Color.cyan,
            RogueLiteDoorType.PROGRESSION => Color.green,
            RogueLiteDoorType.DEAD_END => Color.red,
            _ => Color.white
        };
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        UnityEditor.Handles.Label(labelPos, label, style);
        #endif
        
        // Draw spawn check
        if (targetSpawnPoint != null)
        {
            Vector3 spawnPos = targetSpawnPoint.position;
            Gizmos.color = IsPositionClear(spawnPos) ? Color.green : Color.red;
            Gizmos.DrawWireSphere(spawnPos, spawnCheckRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPos, maxSearchDistance);
        }

        // Draw floor validation
        if (playerSpawn != null)
        {
            Vector3 doorPosition = transform.position;
            Vector3 spawnPosition = playerSpawn.position;
            Vector3 doorToSpawn = (spawnPosition - doorPosition).normalized;
            Vector3 behindDoorPosition = doorPosition - (doorToSpawn * floorCheckDistance);
            Vector3 rayStart = behindDoorPosition + Vector3.up * 0.5f;

            bool hasFloor = HasValidFloorBehindDoor();
            Gizmos.color = hasFloor ? Color.green : Color.red;
            Gizmos.DrawWireSphere(behindDoorPosition, floorCheckRadius);
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * floorRaycastDistance);
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
