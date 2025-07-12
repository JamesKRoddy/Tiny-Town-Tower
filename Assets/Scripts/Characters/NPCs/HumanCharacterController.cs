using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Managers;
using Enemies;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HumanCharacterController : MonoBehaviour, IPossessable, IDamageable
{
    [Header("Character Type")]
    [SerializeField] protected CharacterType characterType = CharacterType.HUMAN_MALE_1;

    [Header("Movement Parameters")]
    public float moveMaxSpeed = 10f; // Speed at which the player moves normally
    public float rotationSpeed = 720f; // Speed at which the player rotates
    public float attackRotationSpeed = 360f; // Speed at which the player rotates while attacking
    public float dashSpeed = 20f; // Speed during a dash
    public float dashDuration = 0.2f; // How long a dash lasts
    public float dashCooldown = 1.0f; // Cooldown time between dashes
    public float vaultDuration = 0.4f; // How long a vault lasts
    public float vaultCooldown = 0.3f; // Short cooldown between vaults to prevent rapid firing

    protected bool isAttacking; // Whether the player is currently attacking

    protected Animator animator;
    public Animator Animator => animator;
    protected CharacterCombat characterCombat;
    protected NavMeshAgent agent; // Reference to NavMeshAgent
    protected CharacterInventory characterInventory;

    [Header("Vault Parameters")]
    public LayerMask[] obstacleLayers; // Array of layers for obstacles
    public float capsuleCastRadius = 0.5f; // Radius of the capsule for collision detection
    public float vaultHeight = 1.0f; // Height of the raycast to detect obstacles
    public float vaultOffset = 1.0f; // Distance to move beyond the obstacle after vaulting
    private Collider humanCollider;

    [Header("Enhanced Obstacle Navigation")]
    public float maxVaultHeight = 1.2f; // Maximum height the player can vault over
    public float minVaultHeight = 0.3f; // Minimum height to consider vaulting (below this, just walk over)
    public float obstacleAnalysisRange = 1.5f; // Range for analyzing obstacles ahead
    public int heightCheckRayCount = 5; // Number of raycasts for height analysis
    public bool autoNavigateObstacles = true; // Enable/disable automatic obstacle navigation

    // Automatic obstacle navigation: analyzes height to determine WalkOver, Vault, or TooHigh
    // RollUnder and Block types only come from ObstacleVaultBehavior components

    public enum ObstacleType
    {
        None,
        WalkOver,    // Too low, just walk over
        Vault,       // Perfect height for vaulting
        RollUnder,   // Medium height, could roll under
        TooHigh,     // Too high to navigate
        Block,       // Cannot be vaulted (from component override)
        Pushable     // Can be pushed to move it
    }

    [Header("Input and Movement State")]
    protected Vector3 movementInput; // Stores the current movement input
    private bool isDashing = false; // Whether the player is currently dashing
    private bool isVaulting = false; // Whether the player is currently vaulting
    private bool isPushing = false; // Whether the player is currently pushing an object

    [Header("Dash State")]
    private float dashTime = 0f; // Timer for the current dash
    private float dashCooldownTime = 0f; // Timer for dash cooldown
    private float vaultCooldownTime = 0f; // Timer for vault cooldown
    private Vector3 currentDirection; // Current direction the player is moving in
    private float dashTurnSpeed = 5f; // Speed at which the player can turn while dashing

    [Header("Vault State")]
    private float vaultTime = 0f; // Timer for the current vault
    private Vector3 vaultStartPosition; // Starting position of the vault
    private Vector3 vaultTargetPosition; // Target position for vaulting
    private Vector3 currentVaultDirection; // Current direction during vault (can be adjusted)
    private float vaultTurnSpeed = 1f; // Speed at which the player can turn while vaulting
    private ObstacleType currentVaultType = ObstacleType.None; // Type of obstacle being vaulted
    private ObstacleVaultBehavior currentObstacleComponent = null; // Component on the obstacle being vaulted
    private Collider currentObstacleCollider = null; // Reference to the collider being vaulted (for safety checks)
    private bool isVaultTargetSafe = false; // Whether the calculated vault target is safe

    [Header("Push State")]
    private PushableObject currentPushTarget = null; // The object currently being pushed
    private float pushHoldTime = 0f; // How long the player has been trying to push
    private float pushActivationDelay = 0.5f; // Time to hold before push activates
    private Vector3 lastPushDirection = Vector3.zero; // Last direction the player tried to push

    [Header("Health")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;

    public event Action<float, float> OnDamageTaken;
    public event Action<float, float> OnHeal;
    public event Action OnDeath;
    public CharacterType CharacterType => characterType;
    
    public Allegiance GetAllegiance() => Allegiance.FRIENDLY;

    protected virtual void Awake()
    {
        // Store the reference to NavMeshAgent once
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        humanCollider = GetComponent<Collider>();
        characterCombat = GetComponent<CharacterCombat>();
        characterInventory = GetComponent<CharacterInventory>();
    }

    protected virtual void Start()
    {
        GetComponent<CharacterAnimationEvents>().Setup(characterCombat, this, characterInventory);
        
        // Register with CampManager for target tracking
        if (Managers.CampManager.Instance != null)
        {
            Managers.CampManager.Instance.RegisterTarget(this);
        }
        

    }

    public virtual void PossessedUpdate()
    {
        HandleDash();
        MoveCharacter();
        UpdateAnimations();
    }

    #region IPossessable Interface

    public void OnPossess()
    {
        SetAIControl(false);
        transform.parent = PlayerController.Instance.transform;
    }

    public void OnUnpossess()
    {
        SetAIControl(true);
        transform.SetParent(null, true);
        SceneTransitionManager.Instance.MoveGameObjectBackToCurrent(gameObject);
    }

    /// <summary>
    /// Enables or disables AI components.
    /// </summary>
    /// <param name="isAIControlled">True if the NPC should act autonomously, False if player-controlled.</param>
    private void SetAIControl(bool isAIControlled)
    {
        var navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null) navMeshAgent.enabled = isAIControlled;

        var narrativeInteractive = GetComponent<NarrativeInteractive>();
        if (narrativeInteractive != null) narrativeInteractive.enabled = isAIControlled;

        var settlerNPC = GetComponent<SettlerNPC>();

        foreach (var task in GetComponents<_TaskState>())
        {
            task.enabled = isAIControlled;
        }

        if (isAIControlled)
        {
            settlerNPC?.ChangeTask(TaskType.WANDER);
        }
        else
        {
            settlerNPC?.ChangeState(null);            
        }
    }

    public void Movement(Vector3 movement)
    {
        movementInput = new Vector3(movement.x, 0, movement.z);
    }

    public void Attack()
    {
        if (!isDashing && !isVaulting && !isPushing && characterInventory.equippedWeaponScriptObj != null)
        {
            isAttacking = true;
            animator.SetBool("LightAttack", true);
        }
    }

    public void Dash()
    {
        if (!isDashing && !isVaulting && !isPushing && Time.time > dashCooldownTime && Time.time > vaultCooldownTime && movementInput.magnitude > 0.1f)
        {
            StopAttacking(); // Ensure player stops attacking when dashing

            if (CanVault(out RaycastHit hitInfo, out ObstacleType obstacleType))
            {
                currentVaultType = obstacleType; // Store the vault type for animation
                CalculateVaultTarget(hitInfo);
                
                // Only start vault if a safe target was found
                if (isVaultTargetSafe)
                {
                    StartVault();
                }
                else
                {
                    Debug.Log("Dash: Vault target unsafe, falling back to dash");
                    StartDash();
                }
            }
            else
            {
                StartDash();
            }
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public Vector3 GetMovementInput()
    {
        return movementInput;
    }

    #endregion

    #region Weapons

    public WeaponScriptableObj GetEquipped()
    {
        return characterInventory.equippedWeaponScriptObj;
    }

    public void EquipWeapon(WeaponScriptableObj weapon)
    {
        characterInventory.EquipWeapon(weapon);
    }
    public void EquipMeleeWeapon(int equipped)
    {
        animator.SetInteger("Equipped", equipped);
        UpdateAnimationSpeed();
    }

    private void UpdateAnimationSpeed()
    {
        if (characterInventory.equippedWeaponBase != null)
        {
            // Update the speed of all attack animations in the Attacking Layer
            animator.SetFloat("AttackSpeed", characterInventory.equippedWeaponBase.GetCurrentAttackSpeed());
        }
    }

    //Called from animator state class CombatAnimationState
    public void StopAttacking()
    {
        isAttacking = false;
        if(characterCombat != null)
            characterCombat.StopAttacking();
    }

    #endregion

    #region Dash

    private void HandleDash()
    {
        if (isDashing && Time.time >= dashTime)
        {
            // End dashing when the dash duration has elapsed
            isDashing = false;
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTime = Time.time + dashDuration;
        dashCooldownTime = Time.time + dashCooldown;
        currentDirection = movementInput.normalized; // Initialize dash direction based on input
        animator.SetTrigger("IsDashing");

        characterCombat.DashVFX();
    }

    #endregion

    #region Vaulting

    private bool CanVault(out RaycastHit hitInfo, out ObstacleType obstacleType)
    {
        if (movementInput.magnitude > 0.1f)
        {
            // Use movement direction instead of transform.forward for more accurate detection
            Vector3 movementDirection = movementInput.normalized;
            obstacleType = AnalyzeObstacle(movementDirection, out hitInfo, enableLogs: false);
            
            if (obstacleType == ObstacleType.Vault || obstacleType == ObstacleType.RollUnder)
            {
                // Additional safety check: Ensure we can find a safe vault path
                Vector3 proposedTarget = hitInfo.point + movementDirection.normalized * vaultOffset;
                Vector3 targetPosition = new Vector3(proposedTarget.x, transform.position.y, proposedTarget.z);
                
                // Check if any safe vault position exists (original, extended, or shorter)
                bool canVaultSafely = IsVaultPathSafe(transform.position, targetPosition) ||
                                     IsVaultPathSafe(transform.position, new Vector3(proposedTarget.x + movementDirection.x * vaultOffset, transform.position.y, proposedTarget.z + movementDirection.z * vaultOffset)) ||
                                     IsVaultPathSafe(transform.position, new Vector3(proposedTarget.x - movementDirection.x * vaultOffset * 0.5f, transform.position.y, proposedTarget.z - movementDirection.z * vaultOffset * 0.5f));
                
                return canVaultSafely;
            }
        }
        
        hitInfo = default;
        obstacleType = ObstacleType.None;
        return false;
    }

    private void CalculateVaultTarget(RaycastHit hitInfo)
    {
        Vector3 direction = (hitInfo.point - transform.position).normalized;
        direction.y = 0; // Keep direction horizontal
        CalculateVaultTargetEnhanced(hitInfo, direction);
    }

    private void StartVault()
    {
        isVaulting = true;
        isDashing = false; // Ensure dashing is stopped when starting a vault
        dashTime = 0f; // Reset dash timer
        dashCooldownTime = 0f; // Reset dash cooldown to allow immediate subsequent vaults
        
        // Use custom duration if obstacle component specifies it
        float duration = currentObstacleComponent != null ? 
            currentObstacleComponent.GetVaultDuration(vaultDuration) : vaultDuration;
        vaultTime = Time.time + duration;
        
        vaultStartPosition = transform.position; // Store starting position for lerp
        currentVaultDirection = (vaultTargetPosition - transform.position).normalized; // Initialize vault direction
        
        // Trigger different animations based on obstacle type
        switch (currentVaultType)
        {
            case ObstacleType.Vault:
                animator.SetTrigger("IsVaulting"); // High vault animation
                break;
            case ObstacleType.RollUnder:
                animator.SetTrigger("IsRolling"); // Low vault/roll animation
                break;
            default:
                animator.SetTrigger("IsVaulting"); // Default vault animation
                break;
        }
        
        // Call obstacle component callback if present
        currentObstacleComponent?.OnVaultStart(this);
        
        humanCollider.enabled = false; // Disable the player's collider to avoid collision during vaulting
    }

    private void FinishVault()
    {
        isVaulting = false;
        vaultCooldownTime = Time.time + vaultCooldown; // Set vault cooldown
        humanCollider.enabled = true; // Re-enable the player's collider
        
        // Call obstacle component callback if present
        currentObstacleComponent?.OnVaultComplete(this);
        currentObstacleComponent = null; // Clear reference
    }

    #endregion

    #region Pushing

    /// <summary>
    /// Handles pushing logic when encountering a pushable object
    /// </summary>
    /// <param name="obstacleInfo">Information about the pushable obstacle</param>
    /// <param name="movementDirection">Direction the player is trying to move</param>
    /// <param name="targetMovement">Reference to the movement vector to modify</param>
    private void HandlePushing(RaycastHit obstacleInfo, Vector3 movementDirection, ref Vector3 targetMovement)
    {
        PushableObject pushableObject = obstacleInfo.collider.GetComponent<PushableObject>();
        if (pushableObject == null)
        {
            // No pushable component, treat as regular obstacle
            targetMovement = CalculateWallSlide(targetMovement, obstacleInfo.normal);
            return;
        }

        // Check if we're trying to push in the same direction as before
        bool samePushDirection = currentPushTarget == pushableObject && 
                                Vector3.Dot(movementDirection, lastPushDirection) > 0.8f;

        if (samePushDirection)
        {
            // Continue building up push time
            pushHoldTime += Time.deltaTime;
        }
        else
        {
            // New push attempt, reset timer
            pushHoldTime = 0f;
            currentPushTarget = pushableObject;
            lastPushDirection = movementDirection;
        }

        // Check if we've held long enough to start pushing
        if (pushHoldTime >= pushActivationDelay)
        {
            // Attempt to start pushing
            if (pushableObject.TryStartPush(movementDirection, this))
            {
                isPushing = true;
                // Stop player movement while pushing
                targetMovement = Vector3.zero;
                
                // Trigger push animation if you have one
                animator.SetBool("IsPushing", true);
                
                // Reset push state
                pushHoldTime = 0f;
            }
            else
            {
                // Push failed (blocked or invalid direction), slide along obstacle
                targetMovement = CalculateWallSlide(targetMovement, obstacleInfo.normal);
                pushHoldTime = 0f; // Reset timer since push failed
            }
        }
        else
        {
            // Still building up push time, prevent movement but allow rotation
            targetMovement = Vector3.zero;
            
            // Rotate player towards push direction
            if (movementDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 
                    rotationSpeed * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Called when a push operation completes
    /// </summary>
    public void OnPushComplete()
    {
        isPushing = false;
        currentPushTarget = null;
        animator.SetBool("IsPushing", false);
    }

    /// <summary>
    /// Resets push state when player stops trying to push
    /// </summary>
    private void ResetPushState()
    {
        if (currentPushTarget != null && !isPushing)
        {
            // Check if player is still trying to push the same object
            if (movementInput.magnitude < 0.1f || 
                Vector3.Dot(movementInput.normalized, lastPushDirection) < 0.5f)
            {
                pushHoldTime = 0f;
                currentPushTarget = null;
                lastPushDirection = Vector3.zero;
            }
        }
    }

    #endregion

    #region Enhanced Obstacle Navigation

    /// <summary>
    /// Helper method to combine all obstacle layers into a single LayerMask
    /// </summary>
    private LayerMask GetCombinedObstacleLayers()
    {
        LayerMask combined = 0;
        foreach (LayerMask layer in obstacleLayers)
        {
            combined |= layer;
        }
        return combined;
    }

    /// <summary>
    /// Analyzes an obstacle ahead of the player to determine how to navigate it
    /// </summary>
    /// <param name="direction">Direction the player is moving</param>
    /// <param name="obstacleInfo">Information about the detected obstacle</param>
    /// <param name="enableLogs">Whether to enable debug logging for this call</param>
    /// <returns>Type of obstacle and recommended navigation method</returns>
    private ObstacleType AnalyzeObstacle(Vector3 direction, out RaycastHit obstacleInfo, bool enableLogs = true)
    {
        obstacleInfo = default;
        currentObstacleComponent = null;
        currentObstacleCollider = null; // Clear previous obstacle reference
        
        if (!autoNavigateObstacles || direction.magnitude < 0.1f)
        {
            return ObstacleType.None;
        }

        // Check for obstacles in the movement direction using capsule cast (more reliable than single raycast)
        Vector3 capsuleBottom = transform.position + Vector3.up * 0.3f;
        Vector3 capsuleTop = transform.position + Vector3.up * humanCollider.bounds.size.y;
        
        // First try capsule cast for better detection
        bool foundObstacle = false;
        foreach (LayerMask layer in obstacleLayers)
        {
            if (Physics.CapsuleCast(capsuleBottom, capsuleTop, capsuleCastRadius, direction.normalized, out obstacleInfo, obstacleAnalysisRange, layer))
            {
                foundObstacle = true;
                break;
            }
        }
        
        // Fallback to raycast at chest height if capsule cast fails
        if (!foundObstacle)
        {
            Vector3 chestRayOrigin = transform.position + Vector3.up * vaultHeight;
            if (!Physics.Raycast(chestRayOrigin, direction.normalized, out obstacleInfo, obstacleAnalysisRange, GetCombinedObstacleLayers()))
            {
                return ObstacleType.None;
            }
        }

        // Store reference to the obstacle collider for safety checks
        currentObstacleCollider = obstacleInfo.collider;

        // First priority: Check if the obstacle is pushable and should be pushed from this direction
        PushableObject pushableComponent = obstacleInfo.collider.GetComponent<PushableObject>();
        if (pushableComponent != null)
        {
            if (pushableComponent.IsBeingPushed)
            {
                // Object is currently being pushed, treat as blocking obstacle (can't vault over moving objects)
                return ObstacleType.Block;
            }
            else if (pushableComponent.ShouldBePushed(direction))
            {
                return ObstacleType.Pushable;
            }
            // If object can't be pushed further (at max distance), continue with normal obstacle analysis
            // to allow vaulting if height is appropriate - don't return here, let it fall through
        }

        // Second priority: Check if the obstacle has a component that overrides behavior
        currentObstacleComponent = obstacleInfo.collider.GetComponent<ObstacleVaultBehavior>();
        if (currentObstacleComponent != null)
        {
            var effectiveVaultType = currentObstacleComponent.GetEffectiveVaultType();
            
            // Map component vault type to our ObstacleType enum
            switch (effectiveVaultType)
            {
                case ObstacleVaultBehavior.VaultAnimationType.Vault:
                    return ObstacleType.Vault;
                case ObstacleVaultBehavior.VaultAnimationType.Roll:
                    return ObstacleType.RollUnder;
                case ObstacleVaultBehavior.VaultAnimationType.WalkOver:
                    return ObstacleType.WalkOver;
                case ObstacleVaultBehavior.VaultAnimationType.Block:
                    return ObstacleType.Block;
                default:
                    return ObstacleType.Vault; // Default to vault
            }
        }

        // Fallback: Use height-based analysis if no component is found
        float obstacleHeight = AnalyzeObstacleHeight(direction, obstacleInfo.point, enableLogs);
        
        // Determine obstacle type based on height (roll vs vault only determined by component)
        if (obstacleHeight <= minVaultHeight)
        {
            return ObstacleType.WalkOver;
        }
        else if (obstacleHeight <= maxVaultHeight)
        {
            return ObstacleType.Vault; // Default to vault for all vaultable heights
        }
        else
        {
            return ObstacleType.TooHigh;
        }
    }

    /// <summary>
    /// Analyzes the height of an obstacle using multiple raycasts
    /// </summary>
    /// <param name="direction">Direction toward the obstacle</param>
    /// <param name="obstaclePoint">Point where the obstacle was detected</param>
    /// <param name="enableLogs">Whether to enable debug logging for this call</param>
    /// <returns>Height of the obstacle</returns>
    private float AnalyzeObstacleHeight(Vector3 direction, Vector3 obstaclePoint, bool enableLogs = true)
    {
        float playerGroundLevel = transform.position.y;
        float highestHitPoint = playerGroundLevel;
        
        // Cast rays at different heights to find the top of the obstacle
        for (int i = 0; i < heightCheckRayCount; i++)
        {
            float checkHeight = playerGroundLevel + (maxVaultHeight + 1f) * ((float)i / (heightCheckRayCount - 1));
            Vector3 rayOrigin = new Vector3(transform.position.x, checkHeight, transform.position.z);
            
            if (Physics.Raycast(rayOrigin, direction.normalized, out RaycastHit hit, obstacleAnalysisRange, GetCombinedObstacleLayers()))
            {
                // If we hit something at this height, the obstacle extends at least this high
                highestHitPoint = Mathf.Max(highestHitPoint, checkHeight);
            }
            else if (highestHitPoint > playerGroundLevel)
            {
                // We didn't hit anything at this height, so we've found the top
                break;
            }
        }
        
        return highestHitPoint - playerGroundLevel;
    }



    /// <summary>
    /// Calculates vault target position for the enhanced system with collision safety checks
    /// </summary>
    /// <param name="hitInfo">Obstacle hit information</param>
    /// <param name="direction">Movement direction</param>
    private void CalculateVaultTargetEnhanced(RaycastHit hitInfo, Vector3 direction)
    {
        // SIMPLE APPROACH: Just vault to the same height level as the player
        // Calculate the horizontal vault target position by moving past the hit point
        Vector3 horizontalTarget = hitInfo.point + direction.normalized * vaultOffset;
        
        // Keep the player at the same Y level but add a small buffer to avoid ground detection issues
        float targetY = Mathf.Max(transform.position.y, 0.3f); // Ensure minimum height of 0.3f above ground
        Vector3 primaryTarget = new Vector3(horizontalTarget.x, targetY, horizontalTarget.z);
        
        // Safety check: Ensure target position and path are clear
        if (IsVaultPathSafe(transform.position, primaryTarget))
        {
            vaultTargetPosition = primaryTarget;
            isVaultTargetSafe = true;
            return;
        }
        
        // Try extending further if initial target is unsafe
        Vector3 extendedTarget = hitInfo.point + direction.normalized * (vaultOffset * 2f);
        Vector3 extendedPosition = new Vector3(extendedTarget.x, targetY, extendedTarget.z);
        
        if (IsVaultPathSafe(transform.position, extendedPosition))
        {
            vaultTargetPosition = extendedPosition;
            isVaultTargetSafe = true;
            return;
        }
        
        // If even extended position is unsafe, try shorter vault
        Vector3 shorterTarget = hitInfo.point + direction.normalized * (vaultOffset * 0.5f);
        Vector3 shorterPosition = new Vector3(shorterTarget.x, targetY, shorterTarget.z);
        
        if (IsVaultPathSafe(transform.position, shorterPosition))
        {
            vaultTargetPosition = shorterPosition;
            isVaultTargetSafe = true;
            return;
        }
        
        // NO SAFE POSITION FOUND - vault should be aborted
        isVaultTargetSafe = false;
    }

    /// <summary>
    /// Checks if a vault path from start to target is safe (won't pass through walls/obstacles)
    /// </summary>
    /// <param name="startPos">Starting position</param>
    /// <param name="targetPos">Target landing position</param>
    /// <returns>True if path is safe, false if it would pass through obstacles</returns>
    private bool IsVaultPathSafe(Vector3 startPos, Vector3 targetPos)
    {
        // Check 1: Target position isn't inside an obstacle (but be more lenient with ground-level checks)
        Vector3 checkPos = targetPos;
        if (targetPos.y < 0.5f) // If target is close to ground, check slightly above
        {
            checkPos = new Vector3(targetPos.x, 0.5f, targetPos.z);
        }
        
        if (Physics.CheckSphere(checkPos, capsuleCastRadius * 0.8f, GetCombinedObstacleLayers())) // Use smaller radius for more lenient checking
        {
            return false;
        }
        
        // Check 2: Path from start to target doesn't pass through obstacles
        Vector3 pathDirection = (targetPos - startPos).normalized;
        float pathDistance = Vector3.Distance(startPos, targetPos);
        
        // Use capsule cast to check the entire vault path
        Vector3 capsuleBottom = startPos + Vector3.up * 0.5f; // Start check higher to avoid ground issues
        Vector3 capsuleTop = startPos + Vector3.up * (humanCollider.bounds.size.y + 0.2f); // Add buffer
        
        // Store reference to the collider we're vaulting over for comparison
        Collider originalObstacleCollider = currentObstacleCollider; // Use the stored collider reference
        
        // Check against each obstacle layer
        foreach (LayerMask layer in obstacleLayers)
        {
            if (Physics.CapsuleCast(capsuleBottom, capsuleTop, capsuleCastRadius * 0.8f, pathDirection, out RaycastHit pathHit, pathDistance, layer))
            {
                // Only allow passing through the exact obstacle we're vaulting over
                if (originalObstacleCollider != null && pathHit.collider == originalObstacleCollider)
                {
                    continue; // This is the original obstacle, allow it
                }
                
                // Also allow if hit point is very close to start (likely the original obstacle)
                if (Vector3.Distance(pathHit.point, startPos) < vaultOffset * 0.3f)
                {
                    continue;
                }
                
                // Ignore ground-level obstacles if they're below the vault height
                if (pathHit.point.y < startPos.y + 0.2f)
                {
                    continue;
                }
                
                // Block all other obstacles (including walls)
                return false;
            }
        }
        
        return true; // Path is clear
    }
    
    #endregion

    #region Movement

    /// <summary>
    /// Calculates sliding movement along a wall surface when direct movement is blocked
    /// </summary>
    /// <param name="originalMovement">The intended movement vector</param>
    /// <param name="wallNormal">The normal vector of the wall surface</param>
    /// <returns>Modified movement vector that slides along the wall</returns>
    private Vector3 CalculateWallSlide(Vector3 originalMovement, Vector3 wallNormal)
    {
        // Get the movement direction from input (not the calculated movement with deltaTime)
        Vector3 inputDirection = movementInput.normalized;
        
        // Calculate how much the player is trying to move parallel to the wall vs into it
        Vector3 parallelComponent = inputDirection - Vector3.Project(inputDirection, wallNormal);
        Vector3 perpendicularComponent = Vector3.Project(inputDirection, wallNormal);
        
        // Calculate sliding factor based on how much parallel movement the player intends
        float parallelIntensity = parallelComponent.magnitude;
        float perpendicularIntensity = Mathf.Abs(Vector3.Dot(inputDirection, wallNormal));
        
        // Reduce sliding based on how much the player is pushing into the wall
        float slidingFactor = parallelIntensity * (1f - perpendicularIntensity * 0.5f);
        
        // Project the original movement onto the wall surface
        Vector3 slidingMovement = originalMovement - Vector3.Project(originalMovement, wallNormal);
        
        // Apply the sliding factor to make it feel more responsive to input
        if (slidingMovement.magnitude > 0.01f)
        {
            slidingMovement = slidingMovement.normalized * (originalMovement.magnitude * slidingFactor);
        }
        else
        {
            // If there's no sliding component (moving directly into wall), stop movement
            slidingMovement = Vector3.zero;
        }
        
        return slidingMovement;
    }

    protected void MoveCharacter()
    {
        if (isVaulting)
        {
            // Allow slight directional control during vault, similar to dash
            if (movementInput.magnitude > 0.1f)
            {
                currentVaultDirection = Vector3.Lerp(currentVaultDirection, movementInput.normalized, vaultTurnSpeed * Time.deltaTime);
                currentVaultDirection.y = 0; // Keep vault direction horizontal
                currentVaultDirection = currentVaultDirection.normalized;
            }

            // Time-based vault movement with directional control
            float timeElapsed = vaultDuration - (vaultTime - Time.time);
            float vaultProgress = timeElapsed / vaultDuration;
            vaultProgress = Mathf.Clamp01(vaultProgress); // Ensure progress stays between 0 and 1
            
            // Calculate movement based on adjusted direction and progress
            float vaultDistance = Vector3.Distance(vaultStartPosition, vaultTargetPosition);
            Vector3 adjustedTarget = vaultStartPosition + currentVaultDirection * vaultDistance;
            adjustedTarget.y = vaultStartPosition.y; // Keep at same Y level
            
            // Lerp position from start to adjusted target based on progress
            transform.position = Vector3.Lerp(vaultStartPosition, adjustedTarget, vaultProgress);

            // Rotate player towards the current vault direction
            if (currentVaultDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(currentVaultDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // End vault when duration is complete
            if (Time.time >= vaultTime)
            {
                transform.position = adjustedTarget; // Snap to final adjusted position
                FinishVault();
            }
        }
        else
        {
            float inputMagnitude = movementInput.magnitude;
            float speed = isDashing ? dashSpeed : moveMaxSpeed;
            float currentRotationSpeed = isAttacking ? attackRotationSpeed : rotationSpeed;

            // If dashing, smoothly change direction
            if (isDashing)
            {
                // Interpolate the current direction with the new input direction while dashing
                if (movementInput.magnitude > 0.1f)
                {
                    currentDirection = Vector3.Lerp(currentDirection, movementInput.normalized, dashTurnSpeed * Time.deltaTime);
                }

                // Apply the current direction to the dash movement
                Vector3 targetMovement = currentDirection * speed * Time.deltaTime;

                // Check if the player is hitting a vaultable object while dashing
                if (IsObstacleInPath(targetMovement, out RaycastHit hitInfo))
                {
                    // Check if it's vaultable using height analysis instead of layer
                    ObstacleType obstacleType = AnalyzeObstacle(currentDirection, out RaycastHit obstacleInfo, enableLogs: false);
                    if (obstacleType == ObstacleType.Vault || obstacleType == ObstacleType.RollUnder)
                    {
                        // Store the vault type and component for proper animation selection
                        currentVaultType = obstacleType;
                        
                        // Calculate vault target and check if it's safe
                        CalculateVaultTargetEnhanced(hitInfo, currentDirection);
                        
                        if (isVaultTargetSafe)
                        {
                            // If safe target found, start vaulting
                            StartVault();
                        }
                        else
                        {
                            // If no safe target, stop dashing to prevent wall clipping
                            isDashing = false;
                        }
                    }
                    else
                    {
                        // If not vaultable, stop dashing
                        isDashing = false;
                    }
                }
                else
                {
                    transform.position += targetMovement;

                    // Rotate player towards the current direction
                    Quaternion targetRotation = Quaternion.LookRotation(currentDirection);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, currentRotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // Regular movement behavior when not dashing
                Vector3 targetMovement = movementInput.normalized * speed * inputMagnitude * Time.deltaTime;

                // Enhanced obstacle detection for automatic navigation
                if (autoNavigateObstacles && inputMagnitude > 0.1f)
                {
                    ObstacleType obstacleType = AnalyzeObstacle(movementInput.normalized, out RaycastHit obstacleInfo);
                    
                    switch (obstacleType)
                    {
                        case ObstacleType.WalkOver:
                            // Low obstacle, just continue normal movement
                            break;
                            
                        case ObstacleType.Vault:
                            // Perfect height for vaulting, automatically start vault - but check cooldown first
                            if (Time.time > vaultCooldownTime)
                            {
                                currentVaultType = obstacleType; // Store vault type for animation
                                CalculateVaultTargetEnhanced(obstacleInfo, movementInput.normalized);
                                
                                if (isVaultTargetSafe)
                                {
                                    StartVault();
                                    return; // Exit early since we're now vaulting
                                }
                                else
                                {
                                    // Calculate sliding movement along the obstacle
                                    targetMovement = CalculateWallSlide(targetMovement, obstacleInfo.normal);
                                }
                            }
                            else
                            {
                                // Calculate sliding movement while waiting for cooldown
                                targetMovement = CalculateWallSlide(targetMovement, obstacleInfo.normal);
                            }
                            break;
                            
                        case ObstacleType.RollUnder:
                            // Implement rolling under animation for lower obstacles
                            if (Time.time > vaultCooldownTime)
                            {
                                if (AnalyzeObstacleHeight(movementInput.normalized, obstacleInfo.point, enableLogs: false) <= maxVaultHeight)
                                {
                                    currentVaultType = obstacleType; // Store vault type for animation
                                    CalculateVaultTargetEnhanced(obstacleInfo, movementInput.normalized);
                                    
                                    if (isVaultTargetSafe)
                                    {
                                        StartVault();
                                        return;
                                    }
                                    else
                                    {
                                        // Calculate sliding movement along the obstacle
                                        targetMovement = CalculateWallSlide(targetMovement, obstacleInfo.normal);
                                    }
                                }
                            }
                            else
                            {
                                // Calculate sliding movement while waiting for cooldown
                                targetMovement = CalculateWallSlide(targetMovement, obstacleInfo.normal);
                            }
                            break;
                            
                        case ObstacleType.TooHigh:
                            // Obstacle too high, slide along the wall instead of stopping
                            targetMovement = CalculateWallSlide(targetMovement, obstacleInfo.normal);
                            break;
                            
                        case ObstacleType.Block:
                            // Obstacle marked as non-vaultable by component, slide along the wall
                            targetMovement = CalculateWallSlide(targetMovement, obstacleInfo.normal);
                            break;
                            
                        case ObstacleType.Pushable:
                            // Handle pushing logic
                            HandlePushing(obstacleInfo, movementInput.normalized, ref targetMovement);
                            break;
                            
                        case ObstacleType.None:
                        default:
                            // No obstacle, continue normal movement
                            break;
                    }
                }

                // Check for traditional obstacles (non-vaultable) if we haven't handled it above
                if (!IsObstacleInPath(targetMovement, out RaycastHit hitInfo))
                {
                    if (!isAttacking) // Only move position if not attacking
                    {
                        transform.position += targetMovement;
                    }

                    // Rotate player towards the input direction
                    if (movementInput != Vector3.zero && !isVaulting)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(movementInput);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, currentRotationSpeed * Time.deltaTime);
                    }
                }
                else
                {
                    // Stop dashing if hitting any obstacle
                    isDashing = false;
                }
            }
        }
        
        // Reset push state if player stops trying to push
        ResetPushState();
    }

    private bool IsObstacleInPath(Vector3 direction, out RaycastHit hitInfo)
    {
        Vector3 capsuleBottom = transform.position + Vector3.up * 0.3f; // Slightly above ground to avoid terrain issues
        Vector3 capsuleTop = transform.position + Vector3.up * humanCollider.bounds.size.y;

        // Check against each obstacle layer
        foreach (LayerMask layer in obstacleLayers)
        {
            // Perform a capsule cast to detect obstacles in the path using the current obstacle layer
            if (Physics.CapsuleCast(capsuleBottom, capsuleTop, capsuleCastRadius, direction.normalized, out hitInfo, capsuleCastRadius, layer))
            {
                return true;
            }
        }

        hitInfo = default;
        return false;
    }

    #endregion

    #region Animation

    protected void UpdateAnimations()
    {
        float maxSpeed = isDashing ? dashSpeed : moveMaxSpeed;
        float currentSpeedNormalized = movementInput.magnitude * moveMaxSpeed / maxSpeed;

        animator.SetFloat("Speed", currentSpeedNormalized);
    }

    public virtual void PlayWorkAnimation(string animationName)
    {
        
    }

    #endregion

    private void OnDrawGizmos()
    {
        // Vault detection raycast visualization
        Gizmos.color = Color.cyan;
        Vector3 rayOrigin = transform.position + Vector3.up * vaultHeight;
        Gizmos.DrawLine(rayOrigin, rayOrigin + transform.forward * obstacleAnalysisRange);
        Gizmos.DrawWireSphere(rayOrigin + transform.forward * obstacleAnalysisRange, 0.1f);

        // Enhanced obstacle analysis visualization
        if (autoNavigateObstacles && movementInput.magnitude > 0.1f)
        {
            Vector3 direction = movementInput.normalized;
            
            // Main analysis ray
            Gizmos.color = Color.yellow;
            Vector3 analysisOrigin = transform.position + Vector3.up * vaultHeight;
            Gizmos.DrawLine(analysisOrigin, analysisOrigin + direction * obstacleAnalysisRange);
            
            // Height analysis rays
            for (int i = 0; i < heightCheckRayCount; i++)
            {
                float checkHeight = transform.position.y + (maxVaultHeight + 1f) * ((float)i / (heightCheckRayCount - 1));
                Vector3 heightRayOrigin = new Vector3(transform.position.x, checkHeight, transform.position.z);
                
                // Color code the rays based on height thresholds
                if (checkHeight - transform.position.y <= minVaultHeight)
                    Gizmos.color = Color.green; // Walk over range
                else if (checkHeight - transform.position.y <= maxVaultHeight)
                    Gizmos.color = new Color(1f, 0.5f, 0f); // Vault range (orange) - includes both vault and roll
                else
                    Gizmos.color = Color.red; // Too high range
                
                Gizmos.DrawLine(heightRayOrigin, heightRayOrigin + direction * obstacleAnalysisRange);
                Gizmos.DrawWireSphere(heightRayOrigin, 0.05f);
            }
            
            // Show obstacle type if detected
            ObstacleType obstacleType = AnalyzeObstacle(direction, out RaycastHit obstacleInfo, enableLogs: false);
            if (obstacleType != ObstacleType.None)
            {
                // Color code based on obstacle type
                switch (obstacleType)
                {
                    case ObstacleType.WalkOver:
                        Gizmos.color = Color.green;
                        break;
                    case ObstacleType.Vault:
                        Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
                        break;
                    case ObstacleType.RollUnder:
                        Gizmos.color = Color.blue; // Only from component override
                        break;
                    case ObstacleType.Block:
                        Gizmos.color = Color.gray; // Component-defined block
                        break;
                    case ObstacleType.TooHigh:
                        Gizmos.color = Color.red;
                        break;
                    case ObstacleType.Pushable:
                        Gizmos.color = Color.magenta;
                        break;
                }
                
                Gizmos.DrawWireCube(obstacleInfo.point, Vector3.one * 0.3f);
                
                // Show calculated vault target if applicable
                if (obstacleType == ObstacleType.Vault || obstacleType == ObstacleType.RollUnder)
                {
                    Vector3 targetPos = obstacleInfo.point + direction * vaultOffset;
                    targetPos.y = transform.position.y;
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(targetPos, 0.2f);
                }
            }
        }

        // Vault target position visualization
        if (isVaulting)
        {
            Gizmos.color = Color.green;
            
            // Show original target
            Gizmos.DrawWireSphere(vaultTargetPosition, 0.15f);
            
            // Show adjusted target with directional control
            float vaultDistance = Vector3.Distance(vaultStartPosition, vaultTargetPosition);
            Vector3 adjustedTarget = vaultStartPosition + currentVaultDirection * vaultDistance;
            adjustedTarget.y = vaultStartPosition.y;
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(adjustedTarget, 0.2f);
            
            // Show vault path (adjusted)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, adjustedTarget);
            
            // Show current vault direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + currentVaultDirection * 2f);
            Gizmos.DrawWireSphere(transform.position + currentVaultDirection * 2f, 0.1f);
        }

        // Capsule cast for collision detection visualization
        Gizmos.color = Color.red;

        if (humanCollider != null)
        {
            Vector3 capsuleBottom = transform.position + Vector3.up * 0.1f; // Slightly above ground
            Vector3 capsuleTop = transform.position + Vector3.up * humanCollider.bounds.size.y;

            Gizmos.DrawWireSphere(capsuleBottom, capsuleCastRadius);
            Gizmos.DrawWireSphere(capsuleTop, capsuleCastRadius);
            Gizmos.DrawLine(capsuleBottom, capsuleTop);
        }
        
        // Show vault target calculation when analyzing obstacles
        if (autoNavigateObstacles && movementInput.magnitude > 0.1f)
        {
            Vector3 direction = movementInput.normalized;
            ObstacleType obstacleType = AnalyzeObstacle(direction, out RaycastHit obstacleInfo, enableLogs: false);
            
            if (obstacleType == ObstacleType.Vault || obstacleType == ObstacleType.RollUnder)
            {
                // Show the horizontal target calculation (simple version)
                Vector3 horizontalTarget = obstacleInfo.point + direction * vaultOffset;
                Vector3 simpleTarget = new Vector3(horizontalTarget.x, transform.position.y, horizontalTarget.z);
                
                // Check if this path would be safe
                bool pathSafe = IsVaultPathSafe(transform.position, simpleTarget);
                Gizmos.color = pathSafe ? Color.yellow : Color.red;
                Gizmos.DrawWireSphere(simpleTarget, 0.15f);
                Gizmos.DrawLine(obstacleInfo.point, simpleTarget);
                
                // Show the vault path trajectory
                Gizmos.color = pathSafe ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, simpleTarget);
            }
        }
        
        // Height threshold indicators
        if (Application.isPlaying)
        {
            // Min vault height
            Gizmos.color = Color.green;
            Vector3 minHeightPos = transform.position + Vector3.up * minVaultHeight;
            Gizmos.DrawWireCube(minHeightPos + transform.forward * 0.5f, new Vector3(1f, 0.05f, 0.1f));
            
            // Max vault height (vault and roll both use same height range)
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Vector3 maxHeightPos = transform.position + Vector3.up * maxVaultHeight;
            Gizmos.DrawWireCube(maxHeightPos + transform.forward * 0.5f, new Vector3(1f, 0.05f, 0.1f));
        }

        // Show current vault type if vaulting
        if (isVaulting && Application.isPlaying)
        {
#if UNITY_EDITOR
            // Display vault type text above the player
            Vector3 textPos = transform.position + Vector3.up * 3f;
            string vaultTypeText = $"Vault Type: {currentVaultType}";
            
            // Draw the vault type as a debug label (this will show in Scene view)
            var style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            
            // Note: This text will only be visible in Scene view, not Game view
            Handles.Label(textPos, vaultTypeText, style);
#endif
        }
        
        // Show push state visualization
        if (currentPushTarget != null && Application.isPlaying)
        {
            // Draw line to push target
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentPushTarget.transform.position);
            
            // Draw push direction arrow
            Vector3 pushArrowStart = transform.position + Vector3.up * 1.5f;
            Vector3 pushArrowEnd = pushArrowStart + lastPushDirection * 1.2f;
            Gizmos.DrawLine(pushArrowStart, pushArrowEnd);
            Gizmos.DrawWireSphere(pushArrowEnd, 0.1f);
            
            // Draw push progress circle
            float pushProgress = pushHoldTime / pushActivationDelay;
            Gizmos.color = Color.Lerp(Color.yellow, Color.green, pushProgress);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f * pushProgress);
            
#if UNITY_EDITOR
            // Display push status text
            Vector3 pushTextPos = transform.position + Vector3.up * 2.5f;
            string pushText = isPushing ? "PUSHING" : $"Push Hold: {pushProgress:F1}";
            
            var pushStyle = new GUIStyle();
            pushStyle.normal.textColor = isPushing ? Color.green : Color.yellow;
            pushStyle.fontSize = 10;
            
            Handles.Label(pushTextPos, pushText, pushStyle);
#endif
        }
    }

#region IDamageable Interface

    public void TakeDamage(float amount, Transform damageSource = null)
    {
        float previousHealth = health;
        health = Mathf.Max(0, health - amount);
        OnDamageTaken?.Invoke(amount, health);

        // Play hit VFX
        Vector3 hitPoint = transform.position + Vector3.up * 1.5f; // Adjust height as needed
        Vector3 hitNormal = damageSource != null 
            ? (transform.position - damageSource.position).normalized 
            : Vector3.up; // Use upward direction as fallback
        EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, this);

        if (health <= 0) Die();
    }

    public void Heal(float amount)
    {
        health = Mathf.Min(maxHealth, health + amount);
        OnHeal?.Invoke(amount, health);
    }

    public void Die()
    {
        Debug.Log($"{gameObject.name} has died!");
        OnDeath?.Invoke();

        // Notify all enemies that this NPC was destroyed
        EnemyBase.NotifyTargetDestroyed(transform);

        characterInventory.ClearInventory();

        animator.SetBool("Dead", true);

        // Play death VFX
        Vector3 deathPoint = transform.position + Vector3.up * 1.5f;
        Vector3 deathNormal = Vector3.up; // Default upward direction for death effects
        EffectManager.Instance.PlayDeathEffect(deathPoint, deathNormal, this);
    }

    public virtual void StartWork(WorkTask newTask)
    {
        
    }

    #endregion

    public float Health { get => health; set => health = value; }
    public float MaxHealth { get => maxHealth; set => maxHealth = value; }

    protected virtual void OnDestroy()
    {
        // Unregister from CampManager target tracking
        if (Managers.CampManager.Instance != null)
        {
            Managers.CampManager.Instance.UnregisterTarget(this);
        }
    }
}
