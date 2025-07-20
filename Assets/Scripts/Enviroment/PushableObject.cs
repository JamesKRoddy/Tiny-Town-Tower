using UnityEngine;

public class PushableObject : MonoBehaviour
{
    [Header("Push Settings")]
    [SerializeField] private float pushSpeed = 2f;
    [SerializeField] private float pushDistance = 2f; // Distance to push in one action
    [SerializeField] private PushDirection allowedDirections = PushDirection.All;
    [SerializeField] private int maxPushesPerDirection = 3; // Maximum times it can be pushed in one direction
    
    [Header("Gizmo Settings")]
    [SerializeField] private bool showBoundaryArea = true; // Show the boundary area gizmo
    
    [Header("Audio & Effects")]
    [SerializeField] private AudioClip pushStartSound;
    [SerializeField] private AudioClip pushEndSound;
    [SerializeField] private ParticleSystem pushEffect;
    
    [Header("Collision Detection")]
    [SerializeField] private LayerMask obstacleLayerMask = -1; // Layers to check for obstacles
    [SerializeField] private float collisionCheckMargin = 0.1f; // Small margin for collision detection
    [SerializeField] private bool showCollisionDebug = false; // Show collision detection debug info
    
    [System.Flags]
    public enum PushDirection
    {
        None = 0,
        North = 1,
        South = 2,
        East = 4,
        West = 8,
        All = North | South | East | West
    }
    
    // Current state
    private bool isBeingPushed = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private float pushProgress = 0f;
    private HumanCharacterController currentPusher = null;
    
    // Position tracking
    private Vector3 originalPosition;
    
    // Components
    private AudioSource audioSource;
    
    // Events
    public System.Action<Vector3, Vector3> OnPushStart; // From position, to position
    public System.Action<Vector3> OnPushComplete; // Final position
    public System.Action OnPushBlocked; // When push is blocked by obstacle
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        originalPosition = transform.position;
    }
    
    private void Update()
    {
        if (isBeingPushed)
        {
            UpdatePushMovement();
        }
    }
    
    /// <summary>
    /// Checks if this object should be pushed from the given direction
    /// </summary>
    /// <param name="direction">World direction to push</param>
    /// <returns>True if should be pushed</returns>
    public bool ShouldBePushed(Vector3 direction)
    {
        Vector3 pushDir = GetCardinalDirection(direction);
        return pushDir != Vector3.zero && IsDirectionAllowed(pushDir) && CanPushInDirection(pushDir);
    }

    /// <summary>
    /// Attempts to start pushing the object in the specified direction
    /// </summary>
    /// <param name="direction">World direction to push</param>
    /// <param name="pusher">Character attempting to push</param>
    /// <returns>True if push started successfully</returns>
    public bool TryStartPush(Vector3 direction, HumanCharacterController pusher)
    {
        if (isBeingPushed)
            return false;
        
        // Normalize and snap direction to cardinal directions
        Vector3 pushDir = GetCardinalDirection(direction);
        if (pushDir == Vector3.zero)
            return false;
        
        // Check if this direction is allowed
        if (!IsDirectionAllowed(pushDir))
            return false;
        
        // Check if we can push in this direction (within limits)
        if (!CanPushInDirection(pushDir))
            return false;
        
        // Calculate target position
        Vector3 targetPos = transform.position + pushDir * pushDistance;
        
        // Check if target position is clear using the improved collision detection method
        if (!IsPositionClear(targetPos))
        {
            OnPushBlocked?.Invoke();
            return false;
        }
        
        // Start the push
        StartPush(targetPos, pusher);
        
        return true;
    }
    
    /// <summary>
    /// Checks if we can push in the given direction without exceeding limits
    /// </summary>
    /// <param name="direction">Cardinal direction to check</param>
    /// <returns>True if push is allowed</returns>
    private bool CanPushInDirection(Vector3 direction)
    {
        // Calculate where we would be after this push
        Vector3 futurePosition = transform.position + direction * pushDistance;
        
        // Calculate net distance from original in this direction
        Vector3 toFuture = futurePosition - originalPosition;
        float netDistance = 0f;
        
        // Get the distance in the specific direction we're pushing
        if (direction == Vector3.forward)
            netDistance = toFuture.z;
        else if (direction == Vector3.back)
            netDistance = -toFuture.z; // Negative because we're going in opposite direction
        else if (direction == Vector3.right)
            netDistance = toFuture.x;
        else if (direction == Vector3.left)
            netDistance = -toFuture.x;
        
        // Check if this would exceed our limit
        float maxDistance = maxPushesPerDirection * pushDistance;
        
        // Allow push if:
        // 1. We're within the distance limit, OR
        // 2. We're moving back towards the original position
        return netDistance <= maxDistance || IsMovingTowardOriginal(futurePosition);
    }
    
    /// <summary>
    /// Checks if the future position is closer to original than current position
    /// </summary>
    /// <param name="futurePosition">Position after the push</param>
    /// <returns>True if moving closer to original</returns>
    private bool IsMovingTowardOriginal(Vector3 futurePosition)
    {
        float currentDistance = Vector3.Distance(transform.position, originalPosition);
        float futureDistance = Vector3.Distance(futurePosition, originalPosition);
        return futureDistance < currentDistance;
    }
    
    /// <summary>
    /// Starts the pushing process
    /// </summary>
    private void StartPush(Vector3 target, HumanCharacterController pusher)
    {
        isBeingPushed = true;
        startPosition = transform.position;
        targetPosition = target;
        pushProgress = 0f;
        currentPusher = pusher;
        
        // Play effects
        if (pushStartSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pushStartSound);
        }
        
        if (pushEffect != null)
        {
            pushEffect.Play();
        }
        
        OnPushStart?.Invoke(startPosition, targetPosition);
    }
    
    /// <summary>
    /// Updates the push movement
    /// </summary>
    private void UpdatePushMovement()
    {
        if (!isBeingPushed)
            return;
        
        // Update push progress
        pushProgress += pushSpeed * Time.deltaTime;
        pushProgress = Mathf.Clamp01(pushProgress);
        
        // Update position
        transform.position = Vector3.Lerp(startPosition, targetPosition, pushProgress);
        
        // Check if push is complete
        if (pushProgress >= 1f)
        {
            CompletePush();
        }
    }
    
    /// <summary>
    /// Completes the push movement
    /// </summary>
    private void CompletePush()
    {
        transform.position = targetPosition;
        isBeingPushed = false;
        
        // Notify the pusher that the push is complete
        if (currentPusher != null)
        {
            currentPusher.OnPushComplete();
        }
        
        currentPusher = null;
        
        // Play completion effects
        if (pushEndSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pushEndSound);
        }
        
        if (pushEffect != null)
        {
            pushEffect.Stop();
        }
        
        OnPushComplete?.Invoke(targetPosition);
    }
    
    /// <summary>
    /// Converts a world direction to the nearest cardinal direction
    /// </summary>
    private Vector3 GetCardinalDirection(Vector3 worldDirection)
    {
        worldDirection.y = 0; // Ignore vertical component
        worldDirection = worldDirection.normalized;
        
        if (worldDirection.magnitude < 0.1f)
            return Vector3.zero;
        
        // Find the closest cardinal direction
        Vector3[] cardinals = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        Vector3 closest = Vector3.zero;
        float maxDot = 0.5f; // Minimum threshold for cardinal direction
        
        foreach (Vector3 cardinal in cardinals)
        {
            float dot = Vector3.Dot(worldDirection, cardinal);
            if (dot > maxDot)
            {
                maxDot = dot;
                closest = cardinal;
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// Checks if a direction is allowed based on the allowed directions setting
    /// </summary>
    private bool IsDirectionAllowed(Vector3 direction)
    {
        if (direction == Vector3.forward)
            return (allowedDirections & PushDirection.North) != 0;
        if (direction == Vector3.back)
            return (allowedDirections & PushDirection.South) != 0;
        if (direction == Vector3.right)
            return (allowedDirections & PushDirection.East) != 0;
        if (direction == Vector3.left)
            return (allowedDirections & PushDirection.West) != 0;
        
        return false;
    }
    

    
    /// <summary>
    /// Improved collision detection method that works better with different collider types
    /// </summary>
    private bool IsPositionClear(Vector3 position)
    {
        Collider objectCollider = GetComponent<Collider>();
        if (objectCollider == null) return true;
        
        // Calculate the movement vector
        Vector3 movement = position - transform.position;
        
        // Use Physics.ComputePenetration for more accurate collision detection
        // This works well with different collider types (Box, Sphere, Capsule, etc.)
        
        // Get all colliders in the scene that could be obstacles
        Collider[] allColliders = FindObjectsOfType<Collider>();
        
        foreach (Collider otherCollider in allColliders)
        {
            // Skip this object's collider and triggers
            if (otherCollider == objectCollider || otherCollider.isTrigger || otherCollider.gameObject == gameObject)
                continue;
            
            // Skip if the other collider is on a layer we're not checking
            if (((1 << otherCollider.gameObject.layer) & obstacleLayerMask) == 0)
                continue;
            
            // Skip the current pusher
            if (currentPusher != null && otherCollider.gameObject == currentPusher.gameObject)
                continue;
            
            // Temporarily move our object to the target position
            Vector3 originalPos = transform.position;
            transform.position = position;
            
            // Check for penetration between the two colliders
            Vector3 direction;
            float distance;
            bool hasPenetration = Physics.ComputePenetration(
                objectCollider, transform.position, transform.rotation,
                otherCollider, otherCollider.transform.position, otherCollider.transform.rotation,
                out direction, out distance
            );
            
            // Restore original position
            transform.position = originalPos;
            
            // If there's penetration and it's significant, the position is not clear
            if (hasPenetration && distance > collisionCheckMargin)
            {
                return false;
            }
        }
        
        return true;
    }
    
    // Public getters
    public bool IsBeingPushed => isBeingPushed;
    public HumanCharacterController CurrentPusher => currentPusher;
    public float PushProgress => pushProgress;
    public Vector3 OriginalPosition => originalPosition;
    public float DistanceFromOrigin => Vector3.Distance(transform.position, originalPosition);
    
    /// <summary>
    /// Gets the current collider bounds for debugging
    /// </summary>
    public Bounds GetCurrentColliderBounds()
    {
        Collider col = GetComponent<Collider>();
        return col != null ? col.bounds : new Bounds(transform.position, Vector3.one);
    }
    
    /// <summary>
    /// Resets the object back to its original position
    /// </summary>
    [ContextMenu("Reset to Original Position")]
    public void ResetToOriginalPosition()
    {
        if (isBeingPushed) return; // Don't reset while being pushed
        
        transform.position = originalPosition;
    }
    
    /// <summary>
    /// Toggles the boundary area gizmo display
    /// </summary>
    [ContextMenu("Toggle Boundary Display")]
    public void ToggleBoundaryDisplay()
    {
        showBoundaryArea = !showBoundaryArea;
        Debug.Log($"Boundary area display: {(showBoundaryArea ? "ON" : "OFF")}");
    }
    

    
    /// <summary>
    /// Toggles collision debug visualization
    /// </summary>
    [ContextMenu("Toggle Collision Debug")]
    public void ToggleCollisionDebug()
    {
        showCollisionDebug = !showCollisionDebug;
        Debug.Log($"Collision debug visualization: {(showCollisionDebug ? "ON" : "OFF")}");
    }
    
    private void OnDrawGizmos()
    {
        // Draw allowed push directions
        Vector3 center = transform.position;
        
        if ((allowedDirections & PushDirection.North) != 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + Vector3.forward * 1.5f);
            Gizmos.DrawWireSphere(center + Vector3.forward * 1.5f, 0.2f);
        }
        
        if ((allowedDirections & PushDirection.South) != 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + Vector3.back * 1.5f);
            Gizmos.DrawWireSphere(center + Vector3.back * 1.5f, 0.2f);
        }
        
        if ((allowedDirections & PushDirection.East) != 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + Vector3.right * 1.5f);
            Gizmos.DrawWireSphere(center + Vector3.right * 1.5f, 0.2f);
        }
        
        if ((allowedDirections & PushDirection.West) != 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + Vector3.left * 1.5f);
            Gizmos.DrawWireSphere(center + Vector3.left * 1.5f, 0.2f);
        }
        
        // Draw current push target if being pushed
        if (isBeingPushed)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        
        // Show original position
        Gizmos.color = Color.blue;
        Vector3 originPos = Application.isPlaying ? originalPosition : transform.position;
        Gizmos.DrawWireSphere(originPos, 0.3f);
        
        // Show push limits (max distance in each direction)
        if (maxPushesPerDirection > 0 && showBoundaryArea)
        {
            float maxDistance = maxPushesPerDirection * pushDistance;
            
            // Show overall boundary area
            Gizmos.color = new Color(1f, 0.5f, 0.5f, 0.3f); // Semi-transparent red
            Vector3 boundarySize = Vector3.zero;
            
            // Calculate boundary size based on allowed directions
            if ((allowedDirections & PushDirection.North) != 0 || (allowedDirections & PushDirection.South) != 0)
                boundarySize.z = maxDistance * 2f; // Can go max distance in both directions
            if ((allowedDirections & PushDirection.East) != 0 || (allowedDirections & PushDirection.West) != 0)
                boundarySize.x = maxDistance * 2f; // Can go max distance in both directions
            boundarySize.y = 0.1f; // Small height for visibility
            
            // Draw boundary area as wire cube centered on original position
            if (boundarySize.magnitude > 0)
            {
                Gizmos.DrawWireCube(originPos, boundarySize);
            }
            
            // Draw boundary lines connecting max positions
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f); // Darker red for lines
            if ((allowedDirections & PushDirection.North) != 0)
            {
                Vector3 northMax = originPos + Vector3.forward * maxDistance;
                Gizmos.DrawLine(originPos, northMax);
                
                // Connect to east/west if they exist
                if ((allowedDirections & PushDirection.East) != 0)
                    Gizmos.DrawLine(northMax, originPos + Vector3.forward * maxDistance + Vector3.right * maxDistance);
                if ((allowedDirections & PushDirection.West) != 0)
                    Gizmos.DrawLine(northMax, originPos + Vector3.forward * maxDistance + Vector3.left * maxDistance);
            }
            
            if ((allowedDirections & PushDirection.South) != 0)
            {
                Vector3 southMax = originPos + Vector3.back * maxDistance;
                Gizmos.DrawLine(originPos, southMax);
                
                // Connect to east/west if they exist
                if ((allowedDirections & PushDirection.East) != 0)
                    Gizmos.DrawLine(southMax, originPos + Vector3.back * maxDistance + Vector3.right * maxDistance);
                if ((allowedDirections & PushDirection.West) != 0)
                    Gizmos.DrawLine(southMax, originPos + Vector3.back * maxDistance + Vector3.left * maxDistance);
            }
            
            if ((allowedDirections & PushDirection.East) != 0)
            {
                Vector3 eastMax = originPos + Vector3.right * maxDistance;
                Gizmos.DrawLine(originPos, eastMax);
            }
            
            if ((allowedDirections & PushDirection.West) != 0)
            {
                Vector3 westMax = originPos + Vector3.left * maxDistance;
                Gizmos.DrawLine(originPos, westMax);
            }
            
            // Show individual max positions as smaller cubes
            Gizmos.color = Color.red;
            if ((allowedDirections & PushDirection.North) != 0)
                Gizmos.DrawWireCube(originPos + Vector3.forward * maxDistance, Vector3.one * 0.2f);
            if ((allowedDirections & PushDirection.South) != 0)
                Gizmos.DrawWireCube(originPos + Vector3.back * maxDistance, Vector3.one * 0.2f);
            if ((allowedDirections & PushDirection.East) != 0)
                Gizmos.DrawWireCube(originPos + Vector3.right * maxDistance, Vector3.one * 0.2f);
            if ((allowedDirections & PushDirection.West) != 0)
                Gizmos.DrawWireCube(originPos + Vector3.left * maxDistance, Vector3.one * 0.2f);
        }
        
        // Show target position when selected (only if not at limit)
        if (Application.isPlaying == false)
        {
            // Preview where object would move in each allowed direction
            Gizmos.color = Color.yellow;
            if ((allowedDirections & PushDirection.North) != 0)
                Gizmos.DrawWireCube(center + Vector3.forward * pushDistance, Vector3.one * 0.3f);
            if ((allowedDirections & PushDirection.South) != 0)
                Gizmos.DrawWireCube(center + Vector3.back * pushDistance, Vector3.one * 0.3f);
            if ((allowedDirections & PushDirection.East) != 0)
                Gizmos.DrawWireCube(center + Vector3.right * pushDistance, Vector3.one * 0.3f);
            if ((allowedDirections & PushDirection.West) != 0)
                Gizmos.DrawWireCube(center + Vector3.left * pushDistance, Vector3.one * 0.3f);
        }
        
        // Show distance from original position in play mode
        if (Application.isPlaying)
        {
            // Draw line from original to current position
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(originalPosition, transform.position);
            
            // Show collider bounds if debug is enabled
            if (showCollisionDebug)
            {
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Semi-transparent cyan
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                }
            }
            
            #if UNITY_EDITOR
            Vector3 textPos = transform.position + Vector3.up * 1.2f;
            float distanceFromOrigin = Vector3.Distance(transform.position, originalPosition);
            string positionInfo = $"Distance: {distanceFromOrigin:F1}";
            
            UnityEditor.Handles.Label(textPos, positionInfo, new GUIStyle()
            {
                normal = { textColor = Color.white },
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            });
            #endif
        }
    }
} 