using UnityEngine;
using Enemies;

/// <summary>
/// Utility class for handling root motion collision detection and adjustment.
/// Prevents characters from moving through walls, pushing enemies, or getting too close to targets during root motion.
/// Consolidates logic used by both HumanCharacterController and EnemyBase.
/// </summary>
public static class RootMotionUtils
{
    /// <summary>
    /// Checks if root motion movement would cause collisions and returns adjusted movement.
    /// Based on the existing logic in HumanCharacterController with enhancements for distance-based collision.
    /// </summary>
    /// <param name="character">The character performing root motion</param>
    /// <param name="rootMotion">The root motion delta to check</param>
    /// <param name="collisionLayers">LayerMask for collision detection</param>
    /// <param name="collisionDetected">Output parameter indicating if a collision was detected</param>
    /// <param name="target">Optional target to maintain distance from</param>
    /// <param name="minDistance">Minimum distance from target (if provided)</param>
    /// <param name="enableDebug">Whether to enable debug logging</param>
    /// <returns>Adjusted root motion vector that prevents overlapping</returns>
    public static Vector3 CheckRootMotionCollision(
        Transform character, 
        Vector3 rootMotion, 
        LayerMask collisionLayers,
        out bool collisionDetected,
        Transform target = null,
        float minDistance = 0.2f,
        bool enableDebug = false)
    {
        collisionDetected = false;
        
        // Get the character's collider for collision detection
        Collider characterCollider = character.GetComponent<Collider>();
        if (characterCollider == null)
        {
            return rootMotion; // No collider, assume safe
        }

        // Calculate proposed position
        Vector3 proposedPosition = character.position + rootMotion;
        
        // Check if moving to proposed position would get too close to target (enhancement for distance-based collision)
        if (target != null && minDistance > 0)
        {
            float distanceToTarget = Vector3.Distance(proposedPosition, target.position);
            float currentDistance = Vector3.Distance(character.position, target.position);
            
            // Always log distance check (temporarily always on for debugging)
            Debug.Log($"[{character.name}] CheckRootMotionCollision - currentDistance: {currentDistance:F2} | proposedDistance: {distanceToTarget:F2} | minDistance: {minDistance:F2}");
            
            if (distanceToTarget < minDistance)
            {
                collisionDetected = true;
                
                Debug.Log($"[{character.name}] Root motion would get too close to target. Distance: {distanceToTarget:F2}, Min: {minDistance:F2}");
                
                // Calculate maximum allowed movement that maintains minimum distance
                Vector3 directionToTarget = (target.position - character.position).normalized;
                float dotProduct = Vector3.Dot(rootMotion.normalized, directionToTarget);
                
                Debug.Log($"[{character.name}] CheckRootMotionCollision - directionToTarget: {directionToTarget} | rootMotion.normalized: {rootMotion.normalized} | dotProduct: {dotProduct:F3}");
                
                // If we're moving towards the target, limit the movement
                if (dotProduct > 0)
                {
                    // Calculate how far we can move towards the target
                    float maxMoveDistance = currentDistance - minDistance;
                    
                    Debug.Log($"[{character.name}] CheckRootMotionCollision - maxMoveDistance: {maxMoveDistance:F2}");
                    
                    if (maxMoveDistance > 0)
                    {
                        float limitedMovement = Mathf.Min(maxMoveDistance, rootMotion.magnitude);
                        Vector3 result = rootMotion.normalized * limitedMovement;
                        Debug.Log($"[{character.name}] Limited movement towards target: {limitedMovement:F2} units | result: {result.magnitude:F3}");
                        return result;
                    }
                    else
                    {
                        Debug.Log($"[{character.name}] Already too close to target, blocking movement");
                        return Vector3.zero;
                    }
                }
                else
                {
                    Debug.Log($"[{character.name}] Not moving towards target (dotProduct <= 0), allowing movement");
                }
            }
            else
            {
                Debug.Log($"[{character.name}] Distance check passed - not too close to target");
            }
        }
        else
        {
            Debug.Log($"[{character.name}] CheckRootMotionCollision - no target or minDistance is 0");
        }

        // Use capsule cast to check for collisions in the root motion direction (from HumanCharacterController logic)
        Vector3 capsuleBottom = character.position + Vector3.up * 0.3f;
        Vector3 capsuleTop = character.position + Vector3.up * characterCollider.bounds.size.y;
        float capsuleRadius = characterCollider.bounds.extents.x; // Use X extent as radius

        if (Physics.CapsuleCast(capsuleBottom, capsuleTop, capsuleRadius * 0.8f, 
            rootMotion.normalized, out RaycastHit hitInfo, rootMotion.magnitude, collisionLayers))
        {
            collisionDetected = true;
            
            // Debug visualization
            if (enableDebug)
            {
                Debug.DrawLine(character.position, hitInfo.point, Color.red, 0.1f);
                Debug.Log($"[{character.name}] Root motion collision detected with {hitInfo.collider.name}");
            }
            
            // Check if we hit the player specifically
            if (hitInfo.collider.CompareTag("Player") || hitInfo.collider.GetComponent<PlayerController>() != null)
            {
                // Allow partial movement towards player but prevent complete overlap
                float playerSafeDistance = hitInfo.distance - minDistance;
                if (playerSafeDistance > 0)
                {
                    if (enableDebug)
                        Debug.Log($"[{character.name}] Partial movement towards player: {playerSafeDistance:F2} units");
                    return rootMotion.normalized * playerSafeDistance;
                }
                
                if (enableDebug)
                    Debug.Log($"[{character.name}] Blocked by player collision");
                return Vector3.zero;
            }

            // Check if we hit an NPC (HumanCharacterController)
            if (hitInfo.collider.GetComponent<HumanCharacterController>() != null)
            {
                // Allow partial movement towards NPCs but prevent complete overlap
                float npcSafeDistance = hitInfo.distance - minDistance;
                if (npcSafeDistance > 0)
                {
                    if (enableDebug)
                        Debug.Log($"[{character.name}] Partial movement towards NPC: {npcSafeDistance:F2} units");
                    return rootMotion.normalized * npcSafeDistance;
                }
                
                if (enableDebug)
                    Debug.Log($"[{character.name}] Blocked by NPC collision");
                return Vector3.zero;
            }

            // Check if we hit an enemy (from HumanCharacterController logic)
            if (hitInfo.collider.CompareTag("Enemy") || hitInfo.collider.GetComponent<EnemyBase>() != null)
            {
                // Allow partial movement towards enemies but prevent complete overlap
                float enemySafeDistance = hitInfo.distance - minDistance;
                if (enemySafeDistance > 0)
                {
                    if (enableDebug)
                        Debug.Log($"[{character.name}] Partial movement towards enemy: {enemySafeDistance:F2} units");
                    return rootMotion.normalized * enemySafeDistance;
                }
                
                if (enableDebug)
                    Debug.Log($"[{character.name}] Blocked by enemy collision");
                return Vector3.zero;
            }

            // For other obstacles (walls, etc.), allow partial movement up to the collision point
            float safeDistance = hitInfo.distance - (minDistance * 0.5f); // Smaller buffer for walls
            if (safeDistance > 0)
            {
                if (enableDebug)
                    Debug.Log($"[{character.name}] Partial movement allowed: {safeDistance:F2} units");
                return rootMotion.normalized * safeDistance;
            }
            
            return Vector3.zero; // No safe movement possible
        }

        return rootMotion; // No collision detected, return original root motion
    }

    /// <summary>
    /// Applies root motion to a character with collision detection and NavMesh validation.
    /// Consolidates the logic from both HumanCharacterController and EnemyBase.
    /// </summary>
    /// <param name="character">The character to move</param>
    /// <param name="rootMotion">The root motion delta</param>
    /// <param name="agent">The NavMeshAgent (optional, for NavMesh validation)</param>
    /// <param name="collisionLayers">LayerMask for collision detection</param>
    /// <param name="target">Optional target to maintain distance from</param>
    /// <param name="minDistance">Minimum distance from target (if provided)</param>
    /// <param name="enableDebug">Whether to enable debug logging</param>
    /// <returns>True if movement was applied, false if blocked</returns>
    public static bool ApplyRootMotion(
        Transform character, 
        Vector3 rootMotion, 
        UnityEngine.AI.NavMeshAgent agent = null,
        LayerMask? collisionLayers = null,
        Transform target = null,
        float minDistance = 0.2f,
        bool enableDebug = false)
    {
        // Always log entry (temporarily always on for debugging)
        Debug.Log($"[{character.name}] RootMotionUtils.ApplyRootMotion called - rootMotion: {rootMotion.magnitude:F3} | target: {(target != null ? target.name : "null")} | minDistance: {minDistance:F2}");
        
        if (rootMotion.magnitude < 0.001f)
        {
            Debug.Log($"[{character.name}] RootMotionUtils.ApplyRootMotion - no movement (magnitude < 0.001)");
            return false; // No movement
        }

        // Use default collision layers if none provided
        LayerMask layers = collisionLayers ?? LayerMask.GetMask("Default", "ObstacleLayer");
        
        // Always log before collision check
        Debug.Log($"[{character.name}] RootMotionUtils.ApplyRootMotion - checking collision with layers: {layers.value}");
        
        // Check for collisions and get adjusted movement
        Vector3 adjustedRootMotion = CheckRootMotionCollision(
            character, rootMotion, layers, out bool collisionDetected, target, minDistance, enableDebug);

        // Always log collision check result
        Debug.Log($"[{character.name}] RootMotionUtils.ApplyRootMotion - collisionDetected: {collisionDetected} | adjustedRootMotion: {adjustedRootMotion.magnitude:F3}");

        // Apply the adjusted movement
        if (adjustedRootMotion.magnitude > 0.001f)
        {
            Vector3 newPosition = character.position + adjustedRootMotion;
            
            // Always log position calculation
            Debug.Log($"[{character.name}] RootMotionUtils.ApplyRootMotion - newPosition: {newPosition} | currentPosition: {character.position}");
            
            // If we have a NavMeshAgent, validate the position is on the NavMesh (EnemyBase logic)
            if (agent != null && agent.isOnNavMesh)
            {
                Debug.Log($"[{character.name}] RootMotionUtils.ApplyRootMotion - validating position on NavMesh");
                
                if (UnityEngine.AI.NavMesh.SamplePosition(newPosition, out UnityEngine.AI.NavMeshHit hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    Debug.Log($"[{character.name}] RootMotionUtils.ApplyRootMotion - position valid on NavMesh, applying movement");
                    character.position = hit.position;
                    agent.nextPosition = hit.position;
                    return true;
                }
                else
                {
                    Debug.Log($"[{character.name}] Root motion position not on NavMesh, blocking movement");
                    return false;
                }
            }
            else
            {
                // No NavMeshAgent, apply directly (HumanCharacterController logic)
                Debug.Log($"[{character.name}] RootMotionUtils.ApplyRootMotion - no NavMeshAgent, applying movement directly");
                character.position = newPosition;
                return true;
            }
        }

        Debug.Log($"[{character.name}] RootMotionUtils.ApplyRootMotion - movement blocked (adjustedRootMotion magnitude < 0.001)");
        return false; // Movement was blocked
    }
}
