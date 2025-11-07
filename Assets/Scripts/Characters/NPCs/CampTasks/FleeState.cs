using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Managers;
using Enemies;

public class FleeState : _TaskState
{
    [Header("Flee Settings")]
    [SerializeField] private float fleeSpeed = 8f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float bunkerSeekRange = 20f;
    [SerializeField] private float fleeDistance = 15f;
    [SerializeField] private LayerMask enemyLayer = 1 << 8; // Default enemy layer
    
    private Vector3 fleeTarget;
    private bool isFleeing = false;
    private bool isSeekingBunker = false;
    private BunkerBuilding targetBunker;
    private float lastThreatCheck = 0f;
    private float threatCheckInterval = 0.5f;
    private float lastThreatTime = 0f;
    private float threatCooldown = 3f; // Stay in flee state for at least 3 seconds after last threat
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    public override void OnEnterState()
    {
        if (agent == null)
        {
            agent = npc.GetAgent();
        }
        
        // Ensure NavMeshAgent is enabled (could be disabled from sleep state)
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            Debug.Log($"{npc.name} re-enabled NavMeshAgent for fleeing");
        }
        
        // CRITICAL: Ensure agent can move (agent.isStopped could be true from sleep state)
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            Debug.Log($"{npc.name} set agent.isStopped = false to enable movement");
        }
        
        // Note: Speed is set by SettlerNPC.UpdateMovementSpeed() based on MaxSpeed() and health modifiers
        
        isFleeing = false;
        isSeekingBunker = false;
        targetBunker = null;
        
        Debug.Log($"{npc.name} entering Flee state");
        
        // IMPORTANT: Immediately check for threats when entering flee state
        // This is crucial for NPCs woken by attacks - they need to flee right away!
        lastThreatCheck = Time.time;
        CheckForThreats();
    }
    
    public override void OnExitState()
    {
        // Unsubscribe from bunker events if we were sheltered
        if (targetBunker != null)
        {
            targetBunker.OnBunkerVacated -= OnBunkerVacated;
        }
        
        isFleeing = false;
        isSeekingBunker = false;
        targetBunker = null;
        
        Debug.Log($"{npc.name} exiting Flee state");
    }
    
    public override void UpdateState()
    {
        // Check if NPC is completely exhausted (no stamina)
        if (npc is SettlerNPC settler && settler.currentStamina <= 0)
        {
            // NPC is too exhausted to flee - collapse in place
            if (isFleeing || isSeekingBunker)
            {
                Debug.Log($"{npc.name} is completely exhausted (stamina: 0) - cannot flee anymore!");
                
                // Stop movement
                if (agent != null && agent.enabled)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                
                isFleeing = false;
                isSeekingBunker = false;
                
                // NPC stays in flee state but can't move (too exhausted)
                // They're still vulnerable to attacks but physically can't run
            }
            return;
        }
        
        if (Time.time - lastThreatCheck >= threatCheckInterval)
        {
            lastThreatCheck = Time.time;
            CheckForThreats();
        }
        
        if (isFleeing)
        {
            UpdateFleeBehavior();
        }
        else if (isSeekingBunker)
        {
            UpdateBunkerSeekBehavior();
        }
    }
    
    /// <summary>
    /// Override stamina update for flee-specific panic drain
    /// Fear and running drain stamina heavily, especially when actively fleeing
    /// </summary>
    public override void UpdateStamina()
    {
        if (isFleeing)
        {
            // Active fleeing drains stamina heavily (panic + running at full speed)
            float panicDrain = npc.GetBaseStaminaDrainRate() * 2.5f * Time.deltaTime;
            npc.ApplyStaminaChange(-panicDrain, "Panic fleeing");
        }
        else if (isSeekingBunker)
        {
            // Seeking bunker is still stressful but slightly less than fleeing
            float seekingDrain = npc.GetBaseStaminaDrainRate() * 1.8f * Time.deltaTime;
            npc.ApplyStaminaChange(-seekingDrain, "Seeking shelter");
        }
        else
        {
            // Just being in flee state (threat detection) has elevated drain
            float alertDrain = npc.GetBaseStaminaDrainRate() * 1.2f * Time.deltaTime;
            npc.ApplyStaminaChange(-alertDrain, "Alert/threatened");
        }
    }
    
    private void CheckForThreats()
    {
        // Check if NPC is too exhausted to flee
        if (npc is SettlerNPC settler && settler.currentStamina <= 0)
        {
            Debug.Log($"{npc.name} detected threat but is too exhausted to flee (stamina: 0)");
            
            // Stop any movement
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            
            isFleeing = false;
            isSeekingBunker = false;
            return; // Can't flee when exhausted
        }
        
        // Check for nearby enemies using FindObjectsByType instead of layer-based detection
        EnemyBase[] nearbyEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        
        // Filter enemies within detection range
        List<EnemyBase> threatsInRange = new List<EnemyBase>();
        foreach (var enemy in nearbyEnemies)
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(npc.transform.position, enemy.transform.position);
                if (distance <= detectionRange)
                {
                    threatsInRange.Add(enemy);
                }
            }
        }
        
        if (threatsInRange.Count > 0)
        {
            // Update last threat time
            lastThreatTime = Time.time;
            
            // Find the closest enemy
            Transform closestEnemy = null;
            float closestDistance = float.MaxValue;
            
            foreach (var enemy in threatsInRange)
            {
                float distance = Vector3.Distance(npc.transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy.transform;
                }
            }
            
            if (closestEnemy != null)
            {
                // Calculate flee direction (away from enemy)
                Vector3 fleeDirection = (npc.transform.position - closestEnemy.position).normalized;
                fleeDirection.y = 0;
                
                // Set flee target
                fleeTarget = npc.transform.position + fleeDirection * fleeDistance;
                
                Debug.Log($"{npc.name} detected threat at {closestDistance:F2}m away, calculating flee target: {fleeTarget}");
                
                // Try to find a bunker first
                BunkerBuilding nearestBunker = FindNearestBunker();
                if (nearestBunker != null && nearestBunker.HasSpace)
                {
                    isSeekingBunker = true;
                    targetBunker = nearestBunker;
                    
                    if (agent != null && agent.enabled)
                    {
                        agent.SetDestination(nearestBunker.transform.position);
                        Debug.Log($"{npc.name} seeking shelter in bunker at {nearestBunker.transform.position}, agent enabled: {agent.enabled}, agent on NavMesh: {agent.isOnNavMesh}");
                    }
                    else
                    {
                        Debug.LogWarning($"{npc.name} cannot seek bunker - agent null or disabled!");
                    }
                }
                else
                {
                    // No bunker available, flee to safe location
                    isFleeing = true;
                    
                    if (agent != null && agent.enabled)
                    {
                        agent.SetDestination(fleeTarget);
                        Debug.Log($"{npc.name} fleeing to {fleeTarget}, agent enabled: {agent.enabled}, agent on NavMesh: {agent.isOnNavMesh}");
                    }
                    else
                    {
                        Debug.LogWarning($"{npc.name} cannot flee - agent null or disabled!");
                    }
                }
            }
        }
        else
        {
            // No immediate threats detected
            // Check if camp is under attack - if so, stay in FLEE state until attack ends (morning or all waves cleared)
            if (CampManager.Instance != null && CampManager.Instance.IsCampUnderAttack)
            {
                // Camp is under attack, stay vigilant - don't return to normal activities
                if (!isFleeing && !isSeekingBunker)
                {
                    // Not actively fleeing, but stay alert during the attack
                    Debug.Log($"{npc.name} no immediate threats but camp is under attack, staying in FLEE state");
                }
                return;
            }
            
            // Camp is peaceful, check cooldown before returning to normal behavior
            if ((isFleeing || isSeekingBunker) && Time.time - lastThreatTime >= threatCooldown)
            {
                Debug.Log($"{npc.name} no longer threatened after {threatCooldown}s cooldown, returning to normal behavior");
                
                // Use shared method to assign work or wander
                TryAssignWorkOrWander();
            }
            else if (isFleeing || isSeekingBunker)
            {
                Debug.Log($"{npc.name} no immediate threats but staying in flee state for {threatCooldown - (Time.time - lastThreatTime):F1}s more");
            }
        }
    }
    
    private void UpdateFleeBehavior()
    {
        // Use base class helper for destination reached checking
        bool hasReachedFleeTarget = HasReachedDestination(null, 0.5f);
        
        if (hasReachedFleeTarget)
        {
            // We've reached a safe distance, but check if there are still threats nearby
            EnemyBase[] nearbyEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            bool stillThreatened = false;
            
            foreach (var enemy in nearbyEnemies)
            {
                if (enemy != null)
                {
                    float distance = Vector3.Distance(npc.transform.position, enemy.transform.position);
                    if (distance <= detectionRange * 1.5f) // Extended range for safety
                    {
                        stillThreatened = true;
                        break;
                    }
                }
            }
            
            if (stillThreatened)
            {
                // Still threatened, flee further
                Vector3 currentFleeDirection = (npc.transform.position - fleeTarget).normalized;
                fleeTarget = npc.transform.position + currentFleeDirection * fleeDistance;
                agent.SetDestination(fleeTarget);
            }
            else
            {
                // We've reached a safe distance, check if we can find a bunker
                BunkerBuilding nearestBunker = FindNearestBunker();
                if (nearestBunker != null && nearestBunker.HasSpace)
                {
                    isFleeing = false;
                    isSeekingBunker = true;
                    targetBunker = nearestBunker;
                    agent.SetDestination(nearestBunker.transform.position);
                }
            }
        }
    }
    
    private void UpdateBunkerSeekBehavior()
    {
        if (npc == null || agent == null || !npc.gameObject.activeInHierarchy || !agent.enabled)
            return;
        if (targetBunker == null)
        {
            isSeekingBunker = false;
            return;
        }
        
        // Use a more generous stopping distance for bunkers since they have large NavMeshObstacles
        float bunkerStoppingDistance = 1.0f; // Reduced from 0.5f for bunkers specifically
        bool hasReachedBunker = HasReachedDestination(targetBunker.transform, bunkerStoppingDistance);
        
        // Also check with a simple distance check as fallback
        float distanceToBunker = Vector3.Distance(npc.transform.position, targetBunker.transform.position);
        bool simpleDistanceCheck = distanceToBunker <= 2f;
        
        if (hasReachedBunker || simpleDistanceCheck)
        {
            // Try to enter the bunker
            if (targetBunker.HasSpace)
            {
                // Cast to HumanCharacterController since SettlerNPC inherits from it
                HumanCharacterController humanNPC = npc as HumanCharacterController;
                bool sheltered = targetBunker.ShelterNPC(humanNPC);
                if (sheltered)
                {
                    Debug.Log($"{npc.name} entered bunker for shelter");
                    
                    if(targetBunker != null)
                    {
                        // Subscribe to bunker events to know when we're evacuated
                        targetBunker.OnBunkerVacated += OnBunkerVacated;
                    }
                    
                    // Stay in flee state but don't move - the NPC is now hidden
                    isSeekingBunker = false;
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                        agent.isStopped = true;
                    
                    // The NPC GameObject is now disabled, so they're safe from enemies
                    // They will remain in this state until the bunker is evacuated or they're manually removed
                    return; // Exit immediately since state has changed to SHELTERED
                }
                
                // If we reach here, sheltering failed
                // Failed to shelter, find another bunker or flee
                BunkerBuilding alternativeBunker = FindNearestBunker();
                if (alternativeBunker != null && alternativeBunker.HasSpace)
                {
                    targetBunker = alternativeBunker;
                    agent.SetDestination(alternativeBunker.transform.position);
                }
                else
                {
                    // No bunkers available, flee to safe location
                    isSeekingBunker = false;
                    isFleeing = true;
                    Vector3 fleeDirection = (npc.transform.position - targetBunker.transform.position).normalized;
                    fleeTarget = npc.transform.position + fleeDirection * fleeDistance;
                    agent.SetDestination(fleeTarget);
                }
            }
            else
            {
                // Bunker is full, find another one or flee
                BunkerBuilding alternativeBunker = FindNearestBunker();
                if (alternativeBunker != null && alternativeBunker.HasSpace)
                {
                    targetBunker = alternativeBunker;
                    agent.SetDestination(alternativeBunker.transform.position);
                }
                else
                {
                    // No bunkers available, flee to safe location
                    isSeekingBunker = false;
                    isFleeing = true;
                    Vector3 fleeDirection = (npc.transform.position - targetBunker.transform.position).normalized;
                    fleeTarget = npc.transform.position + fleeDirection * fleeDistance;
                    agent.SetDestination(fleeTarget);
                }
            }
        }
    }
    
    private BunkerBuilding FindNearestBunker()
    {
        BunkerBuilding[] bunkers = FindObjectsByType<BunkerBuilding>(FindObjectsSortMode.None);
        BunkerBuilding nearestBunker = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var bunker in bunkers)
        {
            if (bunker.HasSpace)
            {
                float distance = Vector3.Distance(npc.transform.position, bunker.transform.position);
                if (distance <= bunkerSeekRange && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestBunker = bunker;
                }
            }
        }
        
        return nearestBunker;
    }
    
    public override float MaxSpeed()
    {
        return fleeSpeed; // Faster speed when fleeing
    }
    
    public override TaskType GetTaskType()
    {
        return TaskType.FLEE;
    }
    
    /// <summary>
    /// Called when the bunker is evacuated (either manually or when destroyed)
    /// </summary>
    /// <param name="bunker">The bunker that was evacuated</param>
    private void OnBunkerVacated(BunkerBuilding bunker)
    {
        if (bunker == targetBunker)
        {
            Debug.Log($"{npc.name} was evacuated from bunker, returning to normal behavior");
            
            // Unsubscribe from the event
            targetBunker.OnBunkerVacated -= OnBunkerVacated;
            
            // Reset state and return to normal behavior
            isSeekingBunker = false;
            isFleeing = false;
            targetBunker = null;
            
            // Check if there are still threats nearby
            EnemyBase[] nearbyEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            bool stillThreatened = false;
            
            foreach (var enemy in nearbyEnemies)
            {
                if (enemy != null)
                {
                    float distance = Vector3.Distance(npc.transform.position, enemy.transform.position);
                    if (distance <= detectionRange)
                    {
                        stillThreatened = true;
                        break;
                    }
                }
            }
            
            if (stillThreatened)
            {
                // Still threatened, stay in flee state
                Debug.Log($"{npc.name} still threatened after evacuation, staying in flee state");
                lastThreatTime = Time.time; // Reset threat timer
            }
            else
            {
                // No threats, check for available work before returning to normal behavior
                TryAssignWorkOrWander();
            }
        }
    }
} 