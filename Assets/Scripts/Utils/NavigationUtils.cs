using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

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
        if (agent == null || target == null) return false;

        // Check if the agent has reached its destination using NavMeshAgent's built-in logic
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            return true;
        }

        // Also check using our sophisticated distance calculation
        float effectiveDistance = CalculateEffectiveReachDistance(agent.transform.position, target, baseStoppingDistance, obstacleBoundsOffset);
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
        if (target == null) return baseStoppingDistance;

        return CalculateEffectiveReachDistance(agent.transform.position, target, baseStoppingDistance, obstacleBoundsOffset);
    }
} 