using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using Managers;

/// <summary>
/// Utility class for shared navigation logic used by enemies and NPCs
/// </summary>
public static class NavigationUtils
{
    /// <summary>
    /// Calculate the effective distance required to reach a target, considering NavMesh obstacles and their bounds
    /// </summary>
    /// <param name="agentPosition">The position of the agent (enemy/NPC)</param>
    /// <param name="target">The target transform</param>
    /// <param name="baseStoppingDistance">The base stopping distance for the agent</param>
    /// <param name="obstacleBoundsOffset">Additional distance to add to obstacle bounds</param>
    /// <returns>The effective distance required to reach this target</returns>
    public static float CalculateEffectiveReachDistance(Vector3 agentPosition, Transform target, float baseStoppingDistance, float obstacleBoundsOffset = 1f)
    {
        if (target == null) return baseStoppingDistance;

        // Start with the base stopping distance
        float effectiveDistance = baseStoppingDistance;

        // Check if the target has a NavMeshObstacle component
        NavMeshObstacle obstacle = target.GetComponent<NavMeshObstacle>();
        if (obstacle != null)
        {
            // Use the NavMeshObstacle's size directly
            Vector3 obstacleSize = obstacle.size;
            
            // Calculate the radius of the obstacle (using the largest dimension)
            float obstacleRadius = Mathf.Max(obstacleSize.x, obstacleSize.z) * 0.5f;
            
            // Add the obstacle radius plus our offset to the effective distance
            effectiveDistance = obstacleRadius + obstacleBoundsOffset;
            
            // Ensure we don't go below the minimum stopping distance
            effectiveDistance = Mathf.Max(effectiveDistance, baseStoppingDistance);
        }
        else
        {
            // For targets without NavMeshObstacle, check if they have a collider
            Collider targetCollider = target.GetComponent<Collider>();
            if (targetCollider != null)
            {
                Bounds targetBounds = targetCollider.bounds;
                float targetRadius = Mathf.Max(targetBounds.extents.x, targetBounds.extents.z);
                
                // Add the target radius plus our offset to the effective distance
                effectiveDistance = targetRadius + obstacleBoundsOffset;
                effectiveDistance = Mathf.Max(effectiveDistance, baseStoppingDistance);
            }
        }

        return effectiveDistance;
    }

    /// <summary>
    /// Check if an agent is close enough to reach/interact with a target
    /// </summary>
    /// <param name="agentPosition">The position of the agent</param>
    /// <param name="target">The target to check distance to</param>
    /// <param name="baseStoppingDistance">The base stopping distance for the agent</param>
    /// <param name="obstacleBoundsOffset">Additional distance to add to obstacle bounds</param>
    /// <returns>True if close enough to reach the target</returns>
    public static bool IsCloseEnoughToReach(Vector3 agentPosition, Transform target, float baseStoppingDistance, float obstacleBoundsOffset = 1f)
    {
        if (target == null) return false;

        float effectiveDistance = CalculateEffectiveReachDistance(agentPosition, target, baseStoppingDistance, obstacleBoundsOffset);
        float distanceToTarget = Vector3.Distance(agentPosition, target.position);
        
        return distanceToTarget <= effectiveDistance;
    }

    /// <summary>
    /// Check if a target position is reachable via NavMesh pathfinding
    /// </summary>
    /// <param name="startPosition">Starting position</param>
    /// <param name="targetPosition">Target position</param>
    /// <param name="maxSampleDistance">Maximum distance to sample for nearby positions</param>
    /// <returns>True if the target is reachable</returns>
    public static bool IsTargetReachable(Vector3 startPosition, Vector3 targetPosition, float maxSampleDistance = 5f)
    {
        // Try direct pathfinding first
        NavMeshPath path = new NavMeshPath();
        bool pathFound = NavMesh.CalculatePath(startPosition, targetPosition, NavMesh.AllAreas, path);
        
        if (pathFound && path.status == NavMeshPathStatus.PathComplete)
        {
            return true;
        }
        
        // Try nearby positions if direct path fails
        Vector3 directionToTarget = (targetPosition - startPosition).normalized;
        float[] testDistances = { 2f, 3f, 4f, maxSampleDistance };
        
        foreach (float distance in testDistances)
        {
            Vector3 nearTargetPosition = targetPosition - directionToTarget * distance;
            
            NavMeshPath nearPath = new NavMeshPath();
            bool nearPathFound = NavMesh.CalculatePath(startPosition, nearTargetPosition, NavMesh.AllAreas, nearPath);
            
            if (nearPathFound && nearPath.status == NavMeshPathStatus.PathComplete)
            {
                return true;
            }
        }
        
        // Fallback for very close targets
        float distanceToTarget = Vector3.Distance(startPosition, targetPosition);
        if (distanceToTarget < 10f)
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Find the closest reachable target from a list of potential targets
    /// </summary>
    /// <param name="agentPosition">The position of the agent</param>
    /// <param name="potentialTargets">List of potential targets</param>
    /// <param name="maxSampleDistance">Maximum distance to sample for nearby positions</param>
    /// <returns>The closest reachable target, or null if none found</returns>
    public static Transform FindClosestReachableTarget(Vector3 agentPosition, List<Transform> potentialTargets, float maxSampleDistance = 5f)
    {
        Transform closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var target in potentialTargets)
        {
            if (target == null) continue;
            
            float distance = Vector3.Distance(agentPosition, target.position);
            
            // Only consider targets that are closer than our current closest
            if (distance < closestDistance)
            {
                bool isReachable = IsTargetReachable(agentPosition, target.position, maxSampleDistance);
                
                if (isReachable)
                {
                    closestTarget = target;
                    closestDistance = distance;
                }
            }
        }
        
        return closestTarget;
    }

    /// <summary>
    /// Check if a NavMeshAgent has reached its destination considering effective reach distance
    /// </summary>
    /// <param name="agent">The NavMeshAgent to check</param>
    /// <param name="target">The target transform</param>
    /// <param name="baseStoppingDistance">The base stopping distance for the agent</param>
    /// <param name="obstacleBoundsOffset">Additional distance to add to obstacle bounds</param>
    /// <returns>True if the agent has reached the target</returns>
    public static bool HasReachedDestination(NavMeshAgent agent, Transform target, float baseStoppingDistance, float obstacleBoundsOffset = 1f)
    {
        if (agent == null) return false;

        // Check if agent is on NavMesh before accessing NavMeshAgent properties
        if (!agent.isOnNavMesh)
        {
            return false; // Agent is not on NavMesh, cannot determine if destination is reached
        }

        // Check if the agent has reached its destination using NavMeshAgent's built-in logic
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            return true;
        }

        // For targets without a transform, use the agent's built-in logic only
        if (target == null)
        {
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
        }

        // Also check using our sophisticated distance calculation, but cap it at a reasonable maximum
        float effectiveDistance = CalculateEffectiveReachDistance(agent.transform.position, target, baseStoppingDistance, obstacleBoundsOffset);
        
        // Cap the effective distance to prevent issues with very large buildings like bunkers
        // This ensures NPCs can still reach large buildings without requiring them to be unreasonably close
        // Set to 6f to accommodate enemy building attack ranges (5f) while still providing reasonable limits
        effectiveDistance = Mathf.Min(effectiveDistance, 6f);
        
        float distanceToTarget = Vector3.Distance(agent.transform.position, target.position);
        
        return distanceToTarget <= effectiveDistance;
    }

    /// <summary>
    /// Get the effective stopping distance for a NavMeshAgent based on its target
    /// </summary>
    /// <param name="agent">The NavMeshAgent</param>
    /// <param name="target">The target transform</param>
    /// <param name="baseStoppingDistance">The base stopping distance</param>
    /// <param name="obstacleBoundsOffset">Additional distance to add to obstacle bounds</param>
    /// <returns>The effective stopping distance</returns>
    public static float GetEffectiveStoppingDistance(NavMeshAgent agent, Transform target, float baseStoppingDistance, float obstacleBoundsOffset = 1f)
    {
        if (agent == null) return baseStoppingDistance;
        if (target == null) return baseStoppingDistance;

        return CalculateEffectiveReachDistance(agent.transform.position, target, baseStoppingDistance, obstacleBoundsOffset);
    }

    #region Rotation Utilities

    /// <summary>
    /// Rotate a transform towards a target position with smooth interpolation
    /// </summary>
    /// <param name="transform">The transform to rotate</param>
    /// <param name="targetPosition">The target position to face</param>
    /// <param name="rotationSpeed">Rotation speed in degrees per second</param>
    /// <param name="ignoreYAxis">Whether to ignore Y-axis differences (keep rotation horizontal)</param>
    /// <returns>True if rotation is complete (within 1 degree)</returns>
    public static bool RotateTowardsTarget(Transform transform, Vector3 targetPosition, float rotationSpeed, bool ignoreYAxis = true)
    {
        if (transform == null) return false;

        Vector3 direction = (targetPosition - transform.position).normalized;
        
        if (ignoreYAxis)
        {
            direction.y = 0;
        }
        
        if (direction == Vector3.zero) return true;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        // Check if rotation is complete (within 1 degree)
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
        return angleDifference <= 1f;
    }

    /// <summary>
    /// Rotate a transform towards a target transform with smooth interpolation
    /// </summary>
    /// <param name="transform">The transform to rotate</param>
    /// <param name="target">The target transform to face</param>
    /// <param name="rotationSpeed">Rotation speed in degrees per second</param>
    /// <param name="ignoreYAxis">Whether to ignore Y-axis differences (keep rotation horizontal)</param>
    /// <returns>True if rotation is complete (within 1 degree)</returns>
    public static bool RotateTowardsTarget(Transform transform, Transform target, float rotationSpeed, bool ignoreYAxis = true)
    {
        if (target == null) return false;
        return RotateTowardsTarget(transform, target.position, rotationSpeed, ignoreYAxis);
    }

    /// <summary>
    /// Check if a transform is facing a target within a specified angle threshold
    /// </summary>
    /// <param name="transform">The transform to check</param>
    /// <param name="targetPosition">The target position</param>
    /// <param name="angleThreshold">Maximum angle difference in degrees</param>
    /// <param name="ignoreYAxis">Whether to ignore Y-axis differences</param>
    /// <returns>True if facing the target within the threshold</returns>
    public static bool IsFacingTarget(Transform transform, Vector3 targetPosition, float angleThreshold = 5f, bool ignoreYAxis = true)
    {
        if (transform == null) return false;

        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        
        if (ignoreYAxis)
        {
            directionToTarget.y = 0;
            directionToTarget = directionToTarget.normalized;
        }
        
        if (directionToTarget == Vector3.zero) return true;

        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        return angleToTarget <= angleThreshold;
    }

    /// <summary>
    /// Check if a transform is facing a target transform within a specified angle threshold
    /// </summary>
    /// <param name="transform">The transform to check</param>
    /// <param name="target">The target transform</param>
    /// <param name="angleThreshold">Maximum angle difference in degrees</param>
    /// <param name="ignoreYAxis">Whether to ignore Y-axis differences</param>
    /// <returns>True if facing the target within the threshold</returns>
    public static bool IsFacingTarget(Transform transform, Transform target, float angleThreshold = 5f, bool ignoreYAxis = true)
    {
        if (target == null) return false;
        return IsFacingTarget(transform, target.position, angleThreshold, ignoreYAxis);
    }

    /// <summary>
    /// Rotate towards target with enhanced speed for specific actions (like attack preparation)
    /// </summary>
    /// <param name="transform">The transform to rotate</param>
    /// <param name="target">The target transform</param>
    /// <param name="baseRotationSpeed">Base rotation speed in degrees per second</param>
    /// <param name="speedMultiplier">Multiplier for enhanced rotation speed</param>
    /// <param name="angleThreshold">Angle threshold to consider rotation complete</param>
    /// <param name="ignoreYAxis">Whether to ignore Y-axis differences</param>
    /// <returns>True if rotation is complete and ready for action</returns>
    public static bool RotateTowardsTargetForAction(Transform transform, Transform target, float baseRotationSpeed, float speedMultiplier = 3f, float angleThreshold = 5f, bool ignoreYAxis = true)
    {
        if (target == null) return false;

        Vector3 direction = (target.position - transform.position).normalized;
        
        if (ignoreYAxis)
        {
            direction.y = 0;
        }
        
        if (direction == Vector3.zero) return true;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float enhancedRotationSpeed = baseRotationSpeed * speedMultiplier;
        
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, enhancedRotationSpeed * Time.deltaTime);
        
        return IsFacingTarget(transform, target, angleThreshold, ignoreYAxis);
    }

    /// <summary>
    /// Rotate towards a work point (position and rotation)
    /// </summary>
    /// <param name="transform">The transform to rotate</param>
    /// <param name="workPosition">The work position</param>
    /// <param name="workRotation">The work rotation</param>
    /// <param name="rotationSpeed">Rotation speed in degrees per second</param>
    /// <returns>True if rotation is complete</returns>
    public static bool RotateTowardsWorkPoint(Transform transform, Vector3 workPosition, Quaternion workRotation, float rotationSpeed)
    {
        if (transform == null) return false;

        // Use the work rotation directly
        transform.rotation = Quaternion.RotateTowards(transform.rotation, workRotation, rotationSpeed * Time.deltaTime);
        
        // Check if rotation is complete (within 1 degree)
        float angleDifference = Quaternion.Angle(transform.rotation, workRotation);
        return angleDifference <= 1f;
    }

    /// <summary>
    /// Rotate towards a work point using a transform
    /// </summary>
    /// <param name="transform">The transform to rotate</param>
    /// <param name="workPoint">The work point transform</param>
    /// <param name="rotationSpeed">Rotation speed in degrees per second</param>
    /// <returns>True if rotation is complete</returns>
    public static bool RotateTowardsWorkPoint(Transform transform, Transform workPoint, float rotationSpeed)
    {
        if (workPoint == null) return false;
        return RotateTowardsWorkPoint(transform, workPoint.position, workPoint.rotation, rotationSpeed);
    }

    /// <summary>
    /// Rotate towards the player for conversations
    /// </summary>
    /// <param name="npcTransform">The NPC's transform</param>
    /// <param name="playerTransform">The player's transform</param>
    /// <param name="rotationSpeed">Rotation speed in degrees per second</param>
    /// <returns>True if rotation is complete</returns>
    public static bool RotateTowardsPlayerForConversation(Transform npcTransform, Transform playerTransform, float rotationSpeed)
    {
        if (npcTransform == null || playerTransform == null) return false;
        
        // For conversations, we want to face the player directly
        return RotateTowardsTarget(npcTransform, playerTransform, rotationSpeed, true);
    }

    /// <summary>
    /// Handle rotation during movement (for NavMeshAgent-based characters)
    /// </summary>
    /// <param name="transform">The transform to rotate</param>
    /// <param name="target">The target transform</param>
    /// <param name="agentVelocity">The NavMeshAgent's velocity</param>
    /// <param name="rotationSpeed">Rotation speed in degrees per second</param>
    /// <param name="velocityThreshold">Minimum velocity to trigger rotation</param>
    public static void HandleMovementRotation(Transform transform, Transform target, Vector3 agentVelocity, float rotationSpeed, float velocityThreshold = 0.1f)
    {
        if (transform == null || target == null) return;
        
        // Only rotate when moving
        if (agentVelocity.magnitude > velocityThreshold)
        {
            RotateTowardsTarget(transform, target, rotationSpeed, true);
        }
    }

    #endregion

    #region Enemy Spawning Utilities

    /// <summary>
    /// Find a random position on the NavMesh that is at least the specified distance away from the player's possessed NPC
    /// </summary>
    /// <param name="minDistanceFromPlayer">Minimum distance required from the player's possessed NPC</param>
    /// <param name="maxAttempts">Maximum number of attempts to find a valid position</param>
    /// <param name="sampleRadius">Radius to sample around random points</param>
    /// <returns>A valid NavMesh position, or Vector3.zero if none found</returns>
    public static Vector3 FindRandomSpawnPosition(float minDistanceFromPlayer = 10f, int maxAttempts = 50, float sampleRadius = 5f)
    {
        Debug.Log($"[NavigationUtils] FindRandomSpawnPosition called with minDistance={minDistanceFromPlayer}, maxAttempts={maxAttempts}, sampleRadius={sampleRadius}");
        
        // Get the player's possessed NPC position
        Vector3 playerPosition = GetPlayerPosition();
        Debug.Log($"[NavigationUtils] Player position: {playerPosition}");
        
        // Get current room bounds for validation
        Bounds? roomBounds = GetCurrentRoomBounds();
        if (roomBounds.HasValue)
        {
            Debug.Log($"[NavigationUtils] Current room bounds: Center={roomBounds.Value.center}, Size={roomBounds.Value.size}");
        }
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate a random position within a reasonable range
            Vector3 randomPosition = GenerateRandomPosition();
            Debug.Log($"[NavigationUtils] Attempt {attempt + 1}: Generated random position: {randomPosition}");
            
            // Sample the NavMesh at this position
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPosition, out hit, sampleRadius, NavMesh.AllAreas))
            {
                Debug.Log($"[NavigationUtils] Attempt {attempt + 1}: Found NavMesh position: {hit.position}");
                
                // Validate the position is within the current room bounds (for RogueLite mode)
                if (roomBounds.HasValue && !roomBounds.Value.Contains(hit.position))
                {
                    Debug.Log($"[NavigationUtils] Attempt {attempt + 1}: Position outside room bounds, skipping");
                    continue;
                }
                
                // Check if this position is far enough from the player
                float distanceFromPlayer = Vector3.Distance(hit.position, playerPosition);
                Debug.Log($"[NavigationUtils] Attempt {attempt + 1}: Distance from player: {distanceFromPlayer} (required: {minDistanceFromPlayer})");
                
                if (distanceFromPlayer >= minDistanceFromPlayer)
                {
                    Debug.Log($"[NavigationUtils] SUCCESS: Found valid spawn position at {hit.position} after {attempt + 1} attempts");
                    return hit.position;
                }
                else
                {
                    Debug.Log($"[NavigationUtils] Attempt {attempt + 1}: Position too close to player ({distanceFromPlayer} < {minDistanceFromPlayer})");
                }
            }
            else
            {
                Debug.Log($"[NavigationUtils] Attempt {attempt + 1}: Failed to sample NavMesh at position {randomPosition}");
            }
        }
        
        Debug.LogWarning($"[NavigationUtils] FAILED: Could not find valid spawn position after {maxAttempts} attempts");
        return Vector3.zero;
    }

    /// <summary>
    /// Get the bounds of the current room (for RogueLite mode) or null for other modes
    /// </summary>
    private static Bounds? GetCurrentRoomBounds()
    {
        if (GameManager.Instance.CurrentGameMode == GameMode.ROGUE_LITE)
        {
            if (RogueLiteManager.Instance != null && RogueLiteManager.Instance.BuildingManager != null)
            {
                var currentRoomParent = RogueLiteManager.Instance.BuildingManager.CurrentRoomParent;
                if (currentRoomParent != null)
                {
                    return GetRoomBounds(currentRoomParent);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Find multiple random spawn positions on the NavMesh, ensuring they're all at least the specified distance from the player
    /// </summary>
    /// <param name="count">Number of spawn positions to find</param>
    /// <param name="minDistanceFromPlayer">Minimum distance required from the player's possessed NPC</param>
    /// <param name="minDistanceBetweenSpawns">Minimum distance between spawn positions</param>
    /// <param name="maxAttemptsPerSpawn">Maximum attempts per spawn position</param>
    /// <param name="sampleRadius">Radius to sample around random points</param>
    /// <returns>Array of valid NavMesh positions</returns>
    public static Vector3[] FindMultipleSpawnPositions(int count, float minDistanceFromPlayer = 10f, float minDistanceBetweenSpawns = 3f, int maxAttemptsPerSpawn = 30, float sampleRadius = 5f)
    {
        Vector3[] spawnPositions = new Vector3[count];
        int foundCount = 0;
        
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = FindValidSpawnPosition(spawnPositions, foundCount, minDistanceFromPlayer, minDistanceBetweenSpawns, maxAttemptsPerSpawn, sampleRadius);
            
            if (spawnPosition != Vector3.zero)
            {
                spawnPositions[foundCount] = spawnPosition;
                foundCount++;
            }
            else
            {
                Debug.LogWarning($"[NavigationUtils] Could not find spawn position {i + 1} after {maxAttemptsPerSpawn} attempts");
                break;
            }
        }
        
        // Resize array to actual found count
        if (foundCount < count)
        {
            Vector3[] resizedArray = new Vector3[foundCount];
            System.Array.Copy(spawnPositions, resizedArray, foundCount);
            return resizedArray;
        }
        
        return spawnPositions;
    }

    /// <summary>
    /// Find a spawn position that's valid relative to existing spawn positions
    /// </summary>
    private static Vector3 FindValidSpawnPosition(Vector3[] existingPositions, int existingCount, float minDistanceFromPlayer, float minDistanceBetweenSpawns, int maxAttempts, float sampleRadius)
    {
        Vector3 playerPosition = GetPlayerPosition();
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 randomPosition = GenerateRandomPosition();
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPosition, out hit, sampleRadius, NavMesh.AllAreas))
            {
                // Check distance from player
                float distanceFromPlayer = Vector3.Distance(hit.position, playerPosition);
                if (distanceFromPlayer < minDistanceFromPlayer)
                {
                    continue;
                }
                
                // Check distance from existing spawn positions
                bool tooCloseToExisting = false;
                for (int i = 0; i < existingCount; i++)
                {
                    float distanceFromExisting = Vector3.Distance(hit.position, existingPositions[i]);
                    if (distanceFromExisting < minDistanceBetweenSpawns)
                    {
                        tooCloseToExisting = true;
                        break;
                    }
                }
                
                if (!tooCloseToExisting)
                {
                    return hit.position;
                }
            }
        }
        
        return Vector3.zero;
    }

    /// <summary>
    /// Generate a random position within reasonable bounds
    /// </summary>
    private static Vector3 GenerateRandomPosition()
    {
        // For RogueLite mode, try to get bounds from the current room
        if (GameManager.Instance.CurrentGameMode == GameMode.ROGUE_LITE)
        {
            if (RogueLiteManager.Instance != null && RogueLiteManager.Instance.BuildingManager != null)
            {
                var currentRoomParent = RogueLiteManager.Instance.BuildingManager.CurrentRoomParent;
                if (currentRoomParent != null)
                {
                    // Get the bounds of the current room
                    Bounds roomBounds = GetRoomBounds(currentRoomParent);
                    
                    Debug.Log($"[NavigationUtils] Using current room bounds - Center: {roomBounds.center}, Size: {roomBounds.size}");
                    
                    return new Vector3(
                        Random.Range(roomBounds.min.x, roomBounds.max.x),
                        roomBounds.center.y,
                        Random.Range(roomBounds.min.z, roomBounds.max.z)
                    );
                }
                else
                {
                    Debug.LogWarning("[NavigationUtils] No current room parent found in RogueLite mode");
                }
            }
            else
            {
                Debug.LogWarning("[NavigationUtils] RogueLiteManager or BuildingManager not available");
            }
        }
        
        // Try to get bounds from CampManager for camp modes
        if (Managers.CampManager.Instance != null)
        {
            Vector2 xBounds = Managers.CampManager.Instance.SharedXBounds;
            Vector2 zBounds = Managers.CampManager.Instance.SharedZBounds;
            
            Debug.Log($"[NavigationUtils] Using CampManager bounds - X: [{xBounds.x}, {xBounds.y}], Z: [{zBounds.x}, {zBounds.y}]");
            
            return new Vector3(
                Random.Range(xBounds.x, xBounds.y),
                0f,
                Random.Range(zBounds.x, zBounds.y)
            );
        }
        
        Debug.Log($"[NavigationUtils] No bounds available, using fallback bounds [-50, 50]");
        
        // Fallback to a reasonable default range
        return new Vector3(
            Random.Range(-50f, 50f),
            0f,
            Random.Range(-50f, 50f)
        );
    }

    /// <summary>
    /// Get the bounds of a room parent by combining all its child colliders
    /// </summary>
    private static Bounds GetRoomBounds(GameObject roomParent)
    {
        Collider[] colliders = roomParent.GetComponentsInChildren<Collider>();
        
        if (colliders.Length == 0)
        {
            // If no colliders, use a default area around the room center
            Vector3 center = roomParent.transform.position;
            return new Bounds(center, new Vector3(100f, 10f, 100f));
        }
        
        Bounds bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        {
            bounds.Encapsulate(colliders[i].bounds);
        }
        
        // Add some padding to ensure we don't spawn too close to walls
        bounds.Expand(5f);
        
        return bounds;
    }

    /// <summary>
    /// Get the current position of the player's possessed NPC
    /// </summary>
    public static Vector3 GetPlayerPosition()
    {
        Debug.Log("[NavigationUtils] GetPlayerPosition called");
        
        if (PlayerController.Instance != null)
        {
            Debug.Log("[NavigationUtils] PlayerController.Instance found");
            
            if (PlayerController.Instance._possessedNPC != null)
            {
                Vector3 playerPos = PlayerController.Instance._possessedNPC.GetTransform().position;
                Debug.Log($"[NavigationUtils] Player possessed NPC position: {playerPos}");
                return playerPos;
            }
            else
            {
                Debug.LogWarning("[NavigationUtils] PlayerController.Instance._possessedNPC is null");
            }
        }
        else
        {
            Debug.LogWarning("[NavigationUtils] PlayerController.Instance is null");
        }
        
        // Fallback to finding any player in the scene
        Debug.Log("[NavigationUtils] Attempting fallback: searching for PlayerController in scene");
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            Debug.Log("[NavigationUtils] Found PlayerController via FindFirstObjectByType");
            
            if (player._possessedNPC != null)
            {
                Vector3 playerPos = player._possessedNPC.GetTransform().position;
                Debug.Log($"[NavigationUtils] Fallback player possessed NPC position: {playerPos}");
                return playerPos;
            }
            else
            {
                Debug.LogWarning("[NavigationUtils] Fallback PlayerController._possessedNPC is null");
            }
        }
        else
        {
            Debug.LogWarning("[NavigationUtils] No PlayerController found in scene via FindFirstObjectByType");
        }
        
        // Ultimate fallback
        Debug.LogWarning("[NavigationUtils] Could not find player position, using origin");
        return Vector3.zero;
    }

    /// <summary>
    /// Check if a position is valid for enemy spawning (on NavMesh and far enough from player)
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="minDistanceFromPlayer">Minimum distance required from player</param>
    /// <param name="sampleRadius">Radius to sample for NavMesh validation</param>
    /// <returns>True if the position is valid for spawning</returns>
    public static bool IsValidSpawnPosition(Vector3 position, float minDistanceFromPlayer = 10f, float sampleRadius = 1f)
    {
        // Check if position is on NavMesh
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(position, out hit, sampleRadius, NavMesh.AllAreas))
        {
            return false;
        }
        
        // Check distance from player
        Vector3 playerPosition = GetPlayerPosition();
        float distanceFromPlayer = Vector3.Distance(hit.position, playerPosition);
        
        return distanceFromPlayer >= minDistanceFromPlayer;
    }

    /// <summary>
    /// Check if the NavMesh is ready and has been baked in the current area
    /// </summary>
    /// <param name="centerPosition">Center position to check around</param>
    /// <param name="checkRadius">Radius to check for NavMesh availability</param>
    /// <returns>True if NavMesh appears to be ready</returns>
    public static bool IsNavMeshReady(Vector3 centerPosition, float checkRadius = 10f)
    {
        Debug.Log($"[NavigationUtils] Checking if NavMesh is ready around position: {centerPosition}");
        
        // Check multiple points around the center to ensure NavMesh coverage
        Vector3[] testPositions = {
            centerPosition,
            centerPosition + Vector3.forward * checkRadius * 0.5f,
            centerPosition + Vector3.back * checkRadius * 0.5f,
            centerPosition + Vector3.left * checkRadius * 0.5f,
            centerPosition + Vector3.right * checkRadius * 0.5f
        };
        
        int validPositions = 0;
        foreach (Vector3 testPos in testPositions)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(testPos, out hit, checkRadius, NavMesh.AllAreas))
            {
                validPositions++;
                Debug.Log($"[NavigationUtils] NavMesh found at test position: {testPos} -> {hit.position}");
            }
            else
            {
                Debug.Log($"[NavigationUtils] No NavMesh found at test position: {testPos}");
            }
        }
        
        // Consider NavMesh ready if at least 60% of test positions are valid
        bool isReady = validPositions >= (testPositions.Length * 0.6f);
        Debug.Log($"[NavigationUtils] NavMesh readiness check: {validPositions}/{testPositions.Length} positions valid. Ready: {isReady}");
        
        return isReady;
    }

    #endregion
} 