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
    protected bool isDamaged; // Whether the player is currently playing damage animation

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

    [Header("Climbing Parameters")]
    public float maxClimbHeight = 1.8f; // Maximum height the player can climb onto
    public float climbDuration = 1.0f; // How long a climb takes
    public float climbCooldown = 0.5f; // Cooldown time between climbs
    public float climbCheckDistance = 0.5f; // Distance to check for clear landing area when climbing

    [Header("Gravity System")]
    public bool enableGravity = true; // Enable/disable gravity system
    public float gravity = 9.81f; // Gravity strength
    public float groundCheckDistance = 0.3f; // Distance to check for ground below (increased for platform detection)
    public LayerMask groundLayers = 1; // Layers to consider as ground (default to default layer)
    public float terminalVelocity = 20f; // Maximum falling speed
    public float maxFallDistance = 100f; // Maximum distance to fall before stopping (prevents endless drops)
    private Vector3 gravityVelocity = Vector3.zero; // Current gravity velocity
    private bool isGrounded = true; // Whether character is currently grounded

    // Automatic obstacle navigation: analyzes height to determine WalkOver, Vault, or TooHigh
    // RollUnder and Block types only come from ObstacleVaultBehavior components

    public enum ObstacleType
    {
        None,
        WalkOver,    // Too low, just walk over
        Vault,       // Perfect height for vaulting
        RollUnder,   // Medium height, could roll under
        Climb,       // Wall that can be climbed up onto
        TooHigh,     // Too high to navigate
        Block,       // Cannot be vaulted (from component override)
        Pushable     // Can be pushed to move it
    }

    [Header("Input and Movement State")]
    protected Vector3 movementInput; // Stores the current movement input
    private bool isDashing = false; // Whether the player is currently dashing
    private bool isVaulting = false; // Whether the player is currently vaulting
    private bool isPushing = false; // Whether the player is currently pushing an object
    
    [Header("Movement Tracking")]
    private Vector3 lastPosition; // Position from previous frame for speed calculation
    private float actualMovementSpeed; // Actual speed the player is moving (not input speed)

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

    [Header("Climb State")]
    private bool isClimbing = false; // Whether the player is currently climbing
    private float climbTime = 0f; // Timer for the current climb
    private Vector3 climbStartPosition; // Starting position of the climb
    private Vector3 climbTargetPosition; // Target position for climbing (top of obstacle)
    private Vector3 climbExactFinalPosition; // Exact final position on the platform surface
    private float climbCooldownTime = 0f; // Timer for climb cooldown

    [Header("Push State")]
    private PushableObject currentPushTarget = null; // The object currently being pushed
    private float pushHoldTime = 0f; // How long the player has been trying to push
    private float pushActivationDelay = 0.5f; // Time to hold before push activates
    private Vector3 lastPushDirection = Vector3.zero; // Last direction the player tried to push
    private Vector3 pushOffsetFromObject = Vector3.zero; // Player's offset from the pushed object
    private Vector3 lastPushObjectPosition = Vector3.zero; // Last position of the pushed object

    [Header("Health")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damageCooldown = 0.5f; // Time before TakeDamage can be called again
    private bool isDead = false;
    private float lastDamageTime = 0f; // Track when damage was last taken

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
        
        // Initialize movement tracking
        lastPosition = transform.position;
        actualMovementSpeed = 0f;
        
        // Ensure root motion is disabled by default
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        
        // Log gravity system configuration
        Debug.Log($"[Gravity] {gameObject.name}: Gravity system initialized - enableGravity: {enableGravity}");
        Debug.Log($"[Gravity] {gameObject.name}: Ground layers: {groundLayers.value}");
        Debug.Log($"[Gravity] {gameObject.name}: Obstacle layers count: {obstacleLayers?.Length ?? 0}");
        if (obstacleLayers != null)
        {
            for (int i = 0; i < obstacleLayers.Length; i++)
            {
                Debug.Log($"[Gravity] {gameObject.name}: Obstacle layer {i}: {obstacleLayers[i].value}");
            }
        }
        Debug.Log($"[Gravity] {gameObject.name}: Ground check distance: {groundCheckDistance}");
    }

    public virtual void PossessedUpdate()
    {
        // Don't do anything if dead
        if (health <= 0 || isDead) 
        {
            return;
        }
        
        HandleDash();
        ApplyGravity(); // Apply gravity before movement
        MoveCharacter();
        UpdateActualMovementSpeed();
        UpdateAnimations();
    }

    #region IPossessable Interface

    public void OnPossess()
    {
        Debug.Log($"[Gravity] {gameObject.name}: OnPossess called");
        SetAIControl(false);
        transform.parent = PlayerController.Instance.transform;
        
        // Initialize gravity system for player control
        ResetGravityVelocity();
        isGrounded = CheckGrounded(); // Check initial ground state
        Debug.Log($"[Gravity] {gameObject.name}: OnPossess complete - isGrounded: {isGrounded}, agent.enabled: {(agent != null ? agent.enabled : false)}");
    }

    public void OnUnpossess()
    {
        Debug.Log($"[Gravity] {gameObject.name}: OnUnpossess called");
        SetAIControl(true);
        transform.SetParent(null, true);
        SceneTransitionManager.Instance.MoveGameObjectBackToCurrent(gameObject);
        
        // Reset gravity system when returning to AI control
        ResetGravityVelocity();
        isGrounded = true; // Assume grounded when NavMeshAgent takes over
        Debug.Log($"[Gravity] {gameObject.name}: OnUnpossess complete - isGrounded: {isGrounded}, agent.enabled: {(agent != null ? agent.enabled : false)}");
    }

    /// <summary>
    /// Enables or disables AI components.
    /// </summary>
    /// <param name="isAIControlled">True if the NPC should act autonomously, False if player-controlled.</param>
    private void SetAIControl(bool isAIControlled)
    {
        var navMeshAgent = GetComponent<NavMeshAgent>();
        Debug.Log($"[Gravity] {gameObject.name}: SetAIControl({isAIControlled}) - NavMeshAgent was: {(navMeshAgent != null ? navMeshAgent.enabled : false)}");
        if (navMeshAgent != null) navMeshAgent.enabled = isAIControlled;
        Debug.Log($"[Gravity] {gameObject.name}: NavMeshAgent now: {(navMeshAgent != null ? navMeshAgent.enabled : false)}");

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
        if (!isDashing && !isVaulting && !isPushing && !isClimbing && characterInventory.equippedWeaponScriptObj != null)
        {
            isAttacking = true;
            animator.SetBool("LightAttack", true);
        }
    }

    public void Dash()
    {
        if (!isDashing && !isVaulting && !isPushing && !isClimbing && Time.time > dashCooldownTime && Time.time > vaultCooldownTime && movementInput.magnitude > 0.1f)
        {
            StopAttacking(); // Ensure player stops attacking when dashing

            if (CanVault(out RaycastHit hitInfo, out ObstacleType obstacleType))
            {
                if (obstacleType == ObstacleType.Climb)
                {
                    // Handle climbing
                    Debug.Log($"[Climb] {gameObject.name}: Dash detected climbable obstacle, starting climb");
                    float obstacleHeight = AnalyzeObstacleHeight((hitInfo.point - transform.position).normalized, hitInfo.point, false);
                    CalculateClimbTarget(hitInfo, obstacleHeight);
                    StartClimb();
                }
                else if (obstacleType == ObstacleType.Vault || obstacleType == ObstacleType.RollUnder)
                {
                    // Handle vaulting
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
        // Prevent dashing while damaged
        if (isDamaged) return;
        
        isDashing = true;
        dashTime = Time.time + dashDuration;
        dashCooldownTime = Time.time + dashCooldown;
        currentDirection = movementInput.normalized; // Initialize dash direction based on input
        animator.SetTrigger("IsDashing");

        // Disable root motion during dash
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

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
            
            Debug.Log($"[Climb] {gameObject.name}: CanVault detected obstacle type: {obstacleType}");
            
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
            else if (obstacleType == ObstacleType.Climb)
            {
                // For climbing, we just need to check if we can climb (already validated in AnalyzeObstacle)
                Debug.Log($"[Climb] {gameObject.name}: CanVault detected climbable obstacle");
                return true;
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

    private void CalculateClimbTarget(RaycastHit hitInfo, float obstacleHeight)
    {
        // Calculate the top of the obstacle as the climb target using the provided height
        // Add extra clearance to ensure we land well above the platform surface
        Vector3 climbTopPosition = new Vector3(hitInfo.point.x, transform.position.y + obstacleHeight + 0.3f, hitInfo.point.z);
        
        // Move slightly forward from the wall to land on top
        Vector3 direction = (hitInfo.point - transform.position).normalized;
        direction.y = 0; // Keep direction horizontal
        climbTargetPosition = climbTopPosition + direction * climbCheckDistance;
        
        Debug.Log($"[Climb] {gameObject.name}: Calculated climb target - obstacle height: {obstacleHeight:F2}, top position: {climbTopPosition}, final target: {climbTargetPosition}");
    }

    /// <summary>
    /// Calculate the exact final position for climbing by raycasting down from the target position to find the platform surface
    /// </summary>
    /// <returns>Exact position on the platform surface</returns>
    private Vector3 CalculateExactClimbFinalPosition()
    {
        // Start from our calculated climb target position
        Vector3 startPos = climbTargetPosition;
        
        // Cast a ray downward to find the exact platform surface
        Vector3 rayStart = startPos + Vector3.up * 0.5f; // Start slightly above to ensure we hit the platform
        Vector3 rayDirection = Vector3.down;
        
        // Combine all layers we want to check (obstacle layers + ground layers)
        LayerMask allGroundLayers = groundLayers;
        foreach (LayerMask layer in obstacleLayers)
        {
            allGroundLayers |= layer;
        }
        
        // Cast ray downward to find the platform surface
        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, 2.0f, allGroundLayers, QueryTriggerInteraction.Ignore))
        {
            // Position the character on top of the platform with a small offset to ensure they're above the surface
            Vector3 exactPosition = hit.point + Vector3.up * 0.1f; // Small offset to ensure we're above the surface
            Debug.Log($"[Climb] {gameObject.name}: Found platform surface at {hit.point}, final position: {exactPosition}");
            return exactPosition;
        }
        
        // Fallback to the original calculated position if we can't find the platform
        Debug.LogWarning($"[Climb] {gameObject.name}: Could not find platform surface, using fallback position: {climbTargetPosition}");
        return climbTargetPosition;
    }

    private void StartVault()
    {
        // Prevent vaulting while damaged
        if (isDamaged) return;
        
        isVaulting = true;
        isDashing = false; // Ensure dashing is stopped when starting a vault
        dashTime = 0f; // Reset dash timer
        dashCooldownTime = 0f; // Reset dash cooldown to allow immediate subsequent vaults
        
        // Reset gravity velocity when starting vault
        ResetGravityVelocity();
        
        // Use custom duration if obstacle component specifies it
        float duration = currentObstacleComponent != null ? 
            currentObstacleComponent.GetVaultDuration(vaultDuration) : vaultDuration;
        vaultTime = Time.time + duration;
        
        vaultStartPosition = transform.position; // Store starting position for lerp
        currentVaultDirection = (vaultTargetPosition - transform.position).normalized; // Initialize vault direction
        
        // Disable root motion during vault
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        
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
        
        // Reset gravity velocity when finishing vault to prevent immediate falling
        ResetGravityVelocity();
        
        // Call obstacle component callback if present
        currentObstacleComponent?.OnVaultComplete(this);
        currentObstacleComponent = null; // Clear reference
    }

    private void StartClimb()
    {
        // Prevent climbing while damaged
        if (isDamaged) return;
        
        Debug.Log($"[Climb] {gameObject.name}: STARTING CLIMB - from {transform.position} to {climbTargetPosition}");
        
        isClimbing = true;
        isDashing = false; // Ensure dashing is stopped when starting a climb
        isVaulting = false; // Ensure vaulting is stopped when starting a climb
        dashTime = 0f; // Reset dash timer
        dashCooldownTime = 0f; // Reset dash cooldown
        
        // Reset gravity velocity when starting climb
        ResetGravityVelocity();
        
        climbTime = Time.time + climbDuration;
        climbStartPosition = transform.position; // Store starting position for lerp
        
        // Calculate the exact final position we want to end up at
        // This ensures we land perfectly on the platform surface
        climbExactFinalPosition = CalculateExactClimbFinalPosition();
        Debug.Log($"[Climb] {gameObject.name}: Calculated exact final position: {climbExactFinalPosition}");
        
        // Disable root motion during climb
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        
        // Trigger climb animation (use vault animation as fallback since climb animation might not exist)
        animator.SetTrigger("IsVaulting");
        
        humanCollider.enabled = false; // Disable the player's collider to avoid collision during climbing
    }

    private void FinishClimb()
    {
        Debug.Log($"[Climb] {gameObject.name}: FINISHING CLIMB - teleporting from {transform.position} to {climbExactFinalPosition}");
        
        // Teleport to the exact final position on the platform
        transform.position = climbExactFinalPosition;
        
        isClimbing = false;
        climbCooldownTime = Time.time + climbCooldown; // Set climb cooldown
        humanCollider.enabled = true; // Re-enable the player's collider
        
        // Reset gravity velocity when finishing climb to prevent immediate falling
        ResetGravityVelocity();
        
        // Force ground check to update immediately after climbing
        isGrounded = CheckGrounded();
        
        // Re-enable root motion
        if (animator != null)
        {
            animator.applyRootMotion = true;
        }
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
                
                // Disable root motion during push
                if (animator != null)
                {
                    animator.applyRootMotion = false;
                }
                
                // Store the offset from the object to the player
                pushOffsetFromObject = transform.position - pushableObject.transform.position;
                lastPushObjectPosition = pushableObject.transform.position;
                
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
        
        // Ensure root motion is disabled after push
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        
        // Clear push tracking variables
        pushOffsetFromObject = Vector3.zero;
        lastPushObjectPosition = Vector3.zero;
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
                pushOffsetFromObject = Vector3.zero;
                lastPushObjectPosition = Vector3.zero;
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
            if (enableLogs) Debug.Log($"[Climb] {gameObject.name}: AnalyzeObstacle - autoNavigate disabled or no movement");
            return ObstacleType.None;
        }
        
        if (enableLogs) Debug.Log($"[Climb] {gameObject.name}: AnalyzeObstacle - checking direction: {direction}");

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
                if (enableLogs) Debug.Log($"[Climb] {gameObject.name}: Found obstacle with capsule cast: {obstacleInfo.collider.name} at distance {obstacleInfo.distance:F2}");
                break;
            }
        }
        
        // Fallback to raycast at chest height if capsule cast fails
        if (!foundObstacle)
        {
            Vector3 chestRayOrigin = transform.position + Vector3.up * vaultHeight;
            if (!Physics.Raycast(chestRayOrigin, direction.normalized, out obstacleInfo, obstacleAnalysisRange, GetCombinedObstacleLayers()))
            {
                if (enableLogs) Debug.Log($"[Climb] {gameObject.name}: No obstacle found with raycast either");
                return ObstacleType.None;
            }
            else
            {
                if (enableLogs) Debug.Log($"[Climb] {gameObject.name}: Found obstacle with raycast: {obstacleInfo.collider.name} at distance {obstacleInfo.distance:F2}");
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
        
        if (enableLogs) Debug.Log($"[Climb] {gameObject.name}: Obstacle height analysis: {obstacleHeight:F2} (minVault: {minVaultHeight:F2}, maxVault: {maxVaultHeight:F2}, maxClimb: {maxClimbHeight:F2})");
        
        // Determine obstacle type based on height (roll vs vault only determined by component)
        if (obstacleHeight <= minVaultHeight)
        {
            return ObstacleType.WalkOver;
        }
        else if (obstacleHeight <= maxVaultHeight)
        {
            // Check if vault would be unsafe - if so, try climbing instead
            Vector3 proposedVaultTarget = obstacleInfo.point + direction.normalized * vaultOffset;
            Vector3 vaultTargetPosition = new Vector3(proposedVaultTarget.x, transform.position.y, proposedVaultTarget.z);
            
            // If vault path is unsafe and obstacle is within climbing range, suggest climbing
            if (!IsVaultPathSafe(transform.position, vaultTargetPosition) && obstacleHeight <= maxClimbHeight)
            {
                // Additional check: ensure there's a clear area on top to climb onto
                Vector3 climbTopPosition = new Vector3(obstacleInfo.point.x, transform.position.y + obstacleHeight + 0.1f, obstacleInfo.point.z);
                if (IsVaultPathSafe(transform.position, climbTopPosition))
                {
                    Debug.Log($"[Climb] {gameObject.name}: Vault unsafe, suggesting climb - height: {obstacleHeight:F2}, top position: {climbTopPosition}");
                    return ObstacleType.Climb;
                }
            }
            
            return ObstacleType.Vault; // Default to vault for all vaultable heights
        }
        else if (obstacleHeight <= maxClimbHeight)
        {
            // Too high to vault but within climbing range
            // Check if there's a clear area on top to climb onto
            Vector3 climbTopPosition = new Vector3(obstacleInfo.point.x, transform.position.y + obstacleHeight + 0.1f, obstacleInfo.point.z);
            if (IsVaultPathSafe(transform.position, climbTopPosition))
            {
                Debug.Log($"[Climb] {gameObject.name}: Too high to vault, suggesting climb - height: {obstacleHeight:F2}, top position: {climbTopPosition}");
                return ObstacleType.Climb;
            }
            else
            {
                Debug.Log($"[Climb] {gameObject.name}: No clear area on top for climb - height: {obstacleHeight:F2}, marking as TooHigh");
                return ObstacleType.TooHigh; // No clear area on top
            }
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

    /// <summary>
    /// Checks if a climb path to the target position is safe (specifically for climbing)
    /// </summary>
    /// <param name="startPos">Starting position (ground level)</param>
    /// <param name="targetPos">Target position (top of obstacle)</param>
    /// <returns>True if climb path is safe, false if blocked</returns>
    private bool IsClimbPathSafe(Vector3 startPos, Vector3 targetPos)
    {
        // For climbing, we only need to check if there's a clear area on top of the obstacle
        // We don't need to check the path since we're going straight up
        
        // Check if target position has enough clearance for the character
        Vector3 checkPos = targetPos;
        
        // Use a smaller radius for climbing since we're landing on a surface
        float climbCheckRadius = capsuleCastRadius * 0.6f;
        
        // Check if there's enough space at the target position, but exclude the obstacle we're climbing
        Collider[] collidersAtTarget = Physics.OverlapSphere(checkPos, climbCheckRadius, GetCombinedObstacleLayers());
        
        foreach (Collider col in collidersAtTarget)
        {
            // Skip the obstacle we're climbing (currentObstacleCollider)
            if (currentObstacleCollider != null && col == currentObstacleCollider)
            {
                continue; // This is the obstacle we're climbing, ignore it
            }
            
            Debug.Log($"[Climb] {gameObject.name}: IsClimbPathSafe - target position blocked by {col.name}");
            return false;
        }
        
        // Additional check: ensure there's space above the target for the character's full height
        Vector3 aboveCheckPos = checkPos + Vector3.up * (humanCollider.bounds.size.y * 0.5f);
        Collider[] collidersAbove = Physics.OverlapSphere(aboveCheckPos, climbCheckRadius, GetCombinedObstacleLayers());
        
        foreach (Collider col in collidersAbove)
        {
            // Skip the obstacle we're climbing (currentObstacleCollider)
            if (currentObstacleCollider != null && col == currentObstacleCollider)
            {
                continue; // This is the obstacle we're climbing, ignore it
            }
            
            Debug.Log($"[Climb] {gameObject.name}: IsClimbPathSafe - not enough clearance above target, blocked by {col.name}");
            return false;
        }
        
        Debug.Log($"[Climb] {gameObject.name}: IsClimbPathSafe - path is clear for climbing");
        return true;
    }
    
    #endregion

    #region Root Motion Handling

    /// <summary>
    /// Handles root motion from animations, particularly during attack animations
    /// This prevents the character from moving through walls during root motion
    /// </summary>
    private void OnAnimatorMove()
    {
        // Only handle root motion if the animator is applying root motion
        if (animator == null || !animator.applyRootMotion)
        {
            return;
        }

        // Get the root motion delta from the animator
        Vector3 rootMotionDelta = animator.deltaPosition;
        
        // If there's no movement from root motion, don't do anything
        if (rootMotionDelta.magnitude < 0.001f)
        {
            return;
        }

        // Check if the root motion movement would cause a collision
        Vector3 proposedPosition = transform.position + rootMotionDelta;
        
        // Use capsule cast to check for collisions in the root motion direction
        Vector3 capsuleBottom = transform.position + Vector3.up * 0.3f;
        Vector3 capsuleTop = transform.position + Vector3.up * humanCollider.bounds.size.y;
        
        bool collisionDetected = false;
        foreach (LayerMask layer in obstacleLayers)
        {
            if (Physics.CapsuleCast(capsuleBottom, capsuleTop, capsuleCastRadius * 0.8f, 
                rootMotionDelta.normalized, out RaycastHit hitInfo, rootMotionDelta.magnitude, layer))
            {
                collisionDetected = true;
                break;
            }
        }

        // If no collision detected, apply the root motion movement
        if (!collisionDetected)
        {
            transform.position = proposedPosition;
        }
        // If collision detected, don't apply the movement (character stays in place)
        // This prevents the character from moving through walls during attack animations
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
        if (isPushing)
        {
            // When pushing, follow the object's movement and maintain offset
            if (currentPushTarget != null)
            {
                Vector3 currentObjectPosition = currentPushTarget.transform.position;
                Vector3 objectMovement = currentObjectPosition - lastPushObjectPosition;
                
                // Move player with the object, maintaining the offset
                transform.position = currentObjectPosition + pushOffsetFromObject;
                
                // Update last object position for next frame
                lastPushObjectPosition = currentObjectPosition;
                
                // Rotate to face the push direction
                if (lastPushDirection.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lastPushDirection);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 
                        rotationSpeed * Time.deltaTime);
                }
            }
        }
        else if (isVaulting)
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
        else if (isClimbing)
        {
            // Time-based climb movement - move upward to the exact final position on the platform
            float timeElapsed = climbDuration - (climbTime - Time.time);
            float climbProgress = timeElapsed / climbDuration;
            climbProgress = Mathf.Clamp01(climbProgress); // Ensure progress stays between 0 and 1
            
            // Lerp position from start to the exact final position (upward movement)
            transform.position = Vector3.Lerp(climbStartPosition, climbExactFinalPosition, climbProgress);
            
            // Keep facing the same direction during climb
            // (No rotation changes during climb to maintain stability)
            
            // End climb when duration is complete
            if (Time.time >= climbTime)
            {
                transform.position = climbExactFinalPosition; // Snap to exact final position
                FinishClimb();
            }
        }
        else
        {
            float inputMagnitude = movementInput.magnitude;
            float speed = isDashing ? dashSpeed : moveMaxSpeed;
            float currentRotationSpeed = (isAttacking || isDamaged) ? attackRotationSpeed : rotationSpeed;

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
                    
                    Debug.Log($"[Climb] {gameObject.name}: Auto-navigation detected obstacle type: {obstacleType}");
                    
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
                                
                                Debug.Log($"[Climb] {gameObject.name}: Vault target safe: {isVaultTargetSafe}");
                                
                                if (isVaultTargetSafe)
                                {
                                    StartVault();
                                    return; // Exit early since we're now vaulting
                                }
                                else
                                {
                                    Debug.Log($"[Climb] {gameObject.name}: Vault unsafe, checking if we can climb instead");
                                    // Vault is unsafe, check if we can climb instead
                                    float obstacleHeight = AnalyzeObstacleHeight(movementInput.normalized, obstacleInfo.point, false);
                                    Debug.Log($"[Climb] {gameObject.name}: Climb check - obstacle height: {obstacleHeight:F2}, maxClimbHeight: {maxClimbHeight:F2}");
                                    
                                    if (obstacleHeight <= maxClimbHeight)
                                    {
                                        Vector3 climbTopPosition = new Vector3(obstacleInfo.point.x, transform.position.y + obstacleHeight + 0.1f, obstacleInfo.point.z);
                                        Debug.Log($"[Climb] {gameObject.name}: Climb top position: {climbTopPosition}");
                                        
                                        bool climbPathSafe = IsClimbPathSafe(transform.position, climbTopPosition);
                                        Debug.Log($"[Climb] {gameObject.name}: Climb path safe: {climbPathSafe}");
                                        
                                        if (climbPathSafe)
                                        {
                                            Debug.Log($"[Climb] {gameObject.name}: Can climb instead of vault, starting climb");
                                            CalculateClimbTarget(obstacleInfo, obstacleHeight);
                                            StartClimb();
                                            return; // Exit early since we're now climbing
                                        }
                                        else
                                        {
                                            Debug.Log($"[Climb] {gameObject.name}: Climb path unsafe - no clear area on top");
                                        }
                                    }
                                    else
                                    {
                                        Debug.Log($"[Climb] {gameObject.name}: Obstacle too high for climbing: {obstacleHeight:F2} > {maxClimbHeight:F2}");
                                    }
                                    
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
                            
                        case ObstacleType.Climb:
                            // Wall that can be climbed - automatically start climbing
                            if (Time.time > climbCooldownTime)
                            {
                                Debug.Log($"[Climb] {gameObject.name}: Auto-navigation detected climbable obstacle, starting climb");
                                float obstacleHeight = AnalyzeObstacleHeight(movementInput.normalized, obstacleInfo.point, false);
                                CalculateClimbTarget(obstacleInfo, obstacleHeight);
                                StartClimb();
                                return; // Exit early since we're now climbing
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
                    if (!isAttacking && !isDamaged) // Only move position if not attacking or damaged
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

    #region Gravity System

    /// <summary>
    /// Check if the character is currently grounded
    /// </summary>
    /// <returns>True if grounded, false if in air</returns>
    private bool CheckGrounded()
    {
        // Don't check for ground during vaulting or when collider is disabled
        if (isVaulting || (humanCollider != null && !humanCollider.enabled))
        {
            return false;
        }

        // Use raycast to check for ground directly below character feet
        Vector3 rayStart = transform.position;
        Vector3 rayDirection = Vector3.down;
        
        // Combine all layers we want to check (obstacle layers + ground layers)
        LayerMask allGroundLayers = groundLayers;
        foreach (LayerMask layer in obstacleLayers)
        {
            allGroundLayers |= layer;
        }
        
        // Cast ray downward to check for ground within a small distance
        // Use QueryTriggerInteraction.Ignore to skip trigger colliders
        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, groundCheckDistance, allGroundLayers, QueryTriggerInteraction.Ignore))
        {
            // Check if the hit point is close enough to the character's feet
            float distanceToGround = hit.distance;
            bool isCloseEnough = distanceToGround <= groundCheckDistance;
            return isCloseEnough;
        }
        
        return false;
    }

    /// <summary>
    /// Check if there's ground within the maximum fall distance below the character
    /// </summary>
    /// <returns>True if there's ground within maxFallDistance, false if it's an endless drop</returns>
    private bool HasGroundWithinMaxFallDistance()
    {
        // Cast a ray downward to check for ground within maxFallDistance
        Vector3 rayStart = transform.position;
        Vector3 rayDirection = Vector3.down;
        
        // Combine all layers we want to check (obstacle layers + ground layers)
        LayerMask allGroundLayers = groundLayers;
        foreach (LayerMask layer in obstacleLayers)
        {
            allGroundLayers |= layer;
        }
        
        // Cast ray to check for ground within max fall distance
        // Use QueryTriggerInteraction.Ignore to skip trigger colliders
        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, maxFallDistance, allGroundLayers, QueryTriggerInteraction.Ignore))
        {
            return true; // Found ground within max fall distance
        }
        
        return false; // No ground found within max fall distance (endless drop)
    }

    /// <summary>
    /// Apply gravity to the character when not grounded
    /// </summary>
    protected virtual void ApplyGravity()
    {
        // Don't apply gravity if disabled or during certain states
        if (!enableGravity || isVaulting || isDashing || isPushing || isClimbing || isDead)
        {
            return;
        }

        // Only apply gravity when NavMeshAgent is disabled (player-controlled)
        // When NavMeshAgent is enabled, it handles ground detection and positioning
        if (agent != null && agent.enabled)
        {
            // Reset gravity velocity when NavMeshAgent is handling positioning
            gravityVelocity = Vector3.zero;
            isGrounded = true; // Assume grounded when NavMeshAgent is active
            return;
        }

        // Check if grounded
        bool wasGrounded = isGrounded;
        isGrounded = CheckGrounded();

        // If we just landed, reset gravity velocity
        if (isGrounded && !wasGrounded)
        {
            gravityVelocity = Vector3.zero;
            return;
        }

        // Apply gravity when not grounded
        if (!isGrounded)
        {
            // Safety check: don't fall into endless drops
            if (!HasGroundWithinMaxFallDistance())
            {
                gravityVelocity = Vector3.zero;
                return;
            }
            
            // Apply gravity acceleration
            gravityVelocity.y -= gravity * Time.deltaTime;
            
            // Clamp to terminal velocity
            gravityVelocity.y = Mathf.Max(gravityVelocity.y, -terminalVelocity);
            
            // Apply gravity movement
            Vector3 gravityMovement = gravityVelocity * Time.deltaTime;
            transform.position += gravityMovement;
            
        }
        else
        {
            // When grounded, reset gravity velocity to prevent accumulation
            gravityVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Get the current gravity velocity (for external systems that need to know falling state)
    /// </summary>
    /// <returns>Current gravity velocity vector</returns>
    public Vector3 GetGravityVelocity()
    {
        return gravityVelocity;
    }

    /// <summary>
    /// Check if the character is currently falling
    /// </summary>
    /// <returns>True if falling (not grounded and has downward velocity)</returns>
    public bool IsFalling()
    {
        return !isGrounded && gravityVelocity.y < 0;
    }

    /// <summary>
    /// Check if the character is currently grounded
    /// </summary>
    /// <returns>True if grounded</returns>
    public bool IsGrounded()
    {
        return isGrounded;
    }

    /// <summary>
    /// Reset gravity velocity (useful for teleporting or special movement)
    /// </summary>
    public void ResetGravityVelocity()
    {
        gravityVelocity = Vector3.zero;
    }

    #endregion

    #region Animation

    private void UpdateActualMovementSpeed()
    {
        // Calculate actual movement speed based on position change
        Vector3 currentPosition = transform.position;
        float distanceMoved = Vector3.Distance(currentPosition, lastPosition);
        actualMovementSpeed = distanceMoved / Time.deltaTime;
        
        // Update last position for next frame
        lastPosition = currentPosition;
    }

    protected void UpdateAnimations()
    {
        float maxSpeed = isDashing ? dashSpeed : moveMaxSpeed;
        float currentSpeedNormalized = actualMovementSpeed / maxSpeed;

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
                    case ObstacleType.Climb:
                        Gizmos.color = Color.cyan; // Light blue for climbing
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
                // Show calculated climb target if applicable
                else if (obstacleType == ObstacleType.Climb)
                {
                    float obstacleHeight = AnalyzeObstacleHeight(direction, obstacleInfo.point, false);
                    Vector3 climbTopPosition = new Vector3(obstacleInfo.point.x, transform.position.y + obstacleHeight + 0.1f, obstacleInfo.point.z);
                    Vector3 climbTarget = climbTopPosition + direction * climbCheckDistance;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(climbTarget, 0.2f);
                    Gizmos.DrawLine(obstacleInfo.point, climbTarget);
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

        // Climb target position visualization
        if (isClimbing)
        {
            Gizmos.color = Color.cyan;
            
            // Show climb target
            Gizmos.DrawWireSphere(climbTargetPosition, 0.15f);
            
            // Show climb path
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, climbTargetPosition);
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
        
        // Gravity system visualization
        if (Application.isPlaying)
        {
            // Ground check visualization
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
            
            // Safety fall distance raycast visualization
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Vector3.down * maxFallDistance);
            
            // Gravity velocity visualization
            if (gravityVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Vector3 gravityArrowStart = transform.position + Vector3.up * 1.5f;
                Vector3 gravityArrowEnd = gravityArrowStart + gravityVelocity * 0.5f;
                Gizmos.DrawLine(gravityArrowStart, gravityArrowEnd);
                Gizmos.DrawWireSphere(gravityArrowEnd, 0.1f);
                
#if UNITY_EDITOR
                // Display gravity velocity text
                Vector3 gravityTextPos = transform.position + Vector3.up * 2.5f;
                string gravityText = $"Gravity: {gravityVelocity.y:F1} m/s";
                var gravityStyle = new GUIStyle();
                gravityStyle.normal.textColor = Color.yellow;
                gravityStyle.fontSize = 10;
                Handles.Label(gravityTextPos, gravityText, gravityStyle);
#endif
            }
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
        // Prevent taking damage if cooldown is active
        if (Time.time - lastDamageTime < damageCooldown)
        {
            Debug.Log($"{gameObject.name} damage cooldown active. Damage not applied.");
            return;
        }

        float previousHealth = health;
        health = Mathf.Max(0, health - amount);
        OnDamageTaken?.Invoke(amount, health);

        // Check if already damaged to prevent unnecessary animation calls
        bool wasAlreadyDamaged = isDamaged;
        
        // Set damaged state to prevent movement
        isDamaged = true;
        lastDamageTime = Time.time; // Update last damage time

        // Only trigger damaged animation if not already damaged to prevent unnecessary animation calls
        if (!wasAlreadyDamaged)
        {
            // Calculate hit direction and trigger damaged animation
            DamageUtils.TriggerDamagedAnimation(animator, DamageUtils.CalculateHitDirection(transform, damageSource));
        }

        // Play hit VFX
        var (hitPoint, hitNormal) = DamageUtils.CalculateHitPointAndNormal(transform, damageSource);
        EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, this);

        if (health <= 0 && !isDead) Die();
    }

    /// <summary>
    /// Called when damage animation ends to re-enable movement
    /// </summary>
    public void StopDamage()
    {
        isDamaged = false;
    }

    public void Heal(float amount)
    {
        health = Mathf.Min(maxHealth, health + amount);
        OnHeal?.Invoke(amount, health);
    }

    public void Die()
    {
        // Prevent multiple calls to Die()
        if (isDead) return;
        isDead = true;
        
        Debug.Log($"{gameObject.name} has died!");
        OnDeath?.Invoke();

        // Notify all enemies that this NPC was destroyed
        EnemyBase.NotifyTargetDestroyed(transform);

        characterInventory.ClearInventory();

        animator.SetBool("Dead", true);

        // Disable movement and AI components to prevent dead NPCs from moving
        isAttacking = false;
        isDamaged = false;
        isDashing = false;
        isVaulting = false;
        isPushing = false;
        
        // Disable NavMeshAgent
        if (agent != null)
        {
            agent.enabled = false;
        }
        
        // If this is a SettlerNPC, clear all states and tasks
        if (this is SettlerNPC settler)
        {
            settler.ChangeState(null);
            if (settler.HasAssignedWork())
            {
                settler.StopWork();
            }
        }
        
        // Disable all task states
        foreach (var taskState in GetComponents<_TaskState>())
        {
            if (taskState != null)
            {
                taskState.enabled = false;
            }
        }
        
        // Disable interaction collider but keep main collider for physics
        var narrativeInteractive = GetComponent<NarrativeInteractive>();
        if (narrativeInteractive != null)
        {
            narrativeInteractive.enabled = false;
        }

        // Play death VFX
        Vector3 deathPoint = transform.position + Vector3.up * 1.5f;
        Vector3 deathNormal = Vector3.up; // Default upward direction for death effects
        EffectManager.Instance.PlayDeathEffect(deathPoint, deathNormal, this);
    }

    // Simple work tracking - WorkState handles the actual work execution
    private WorkTask currentWorkTask;
    
    public virtual void StartWork(WorkTask newTask)
    {
        currentWorkTask = newTask;
    }
    
    public virtual void StopWork()
    {
        currentWorkTask = null;
    }
    
    public WorkTask GetCurrentWorkTask()
    {
        return currentWorkTask;
    }

    /// <summary>
    /// Checks if the character can take damage (not on cooldown and not dead)
    /// </summary>
    /// <returns>True if damage can be applied, false otherwise</returns>
    public bool CanTakeDamage()
    {
        return !isDead && (Time.time - lastDamageTime >= damageCooldown);
    }

    #endregion

    public float Health { get => health; set => health = value; }
    public float MaxHealth { get => maxHealth; set => maxHealth = value; }
    public float DamageCooldown { get => damageCooldown; set => damageCooldown = value; }

    protected virtual void OnDestroy()
    {
        // Unregister from CampManager target tracking
        if (Managers.CampManager.Instance != null)
        {
            Managers.CampManager.Instance.UnregisterTarget(this);
        }
    }
}
