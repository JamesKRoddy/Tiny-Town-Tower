using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Windows;
using Managers;
using Enemies;

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

    protected bool isAttacking; // Whether the player is currently attacking

    protected Animator animator;
    public Animator Animator => animator;
    protected CharacterCombat characterCombat;
    protected NavMeshAgent agent; // Reference to NavMeshAgent
    protected CharacterInventory characterInventory;

    [Header("Vault Parameters")]
    public float vaultSpeed = 5f; // Slower speed for vaulting
    public float vaultDetectionRange = 1.0f; // Range to detect vaultable obstacles
    public LayerMask[] obstacleLayers; // Array of layers for obstacles (both vaultable and non-vaultable)
    public float capsuleCastRadius = 0.5f; // Radius of the capsule for collision detection
    public float vaultHeight = 1.0f; // Height of the raycast to detect obstacles
    public float vaultOffset = 1.0f; // Distance to move beyond the obstacle after vaulting
    private Collider humanCollider;

    [Header("Enhanced Obstacle Navigation")]
    public float maxVaultHeight = 1.2f; // Maximum height the player can vault over
    public float minVaultHeight = 0.3f; // Minimum height to consider vaulting (below this, just walk over)
    public float rollUnderHeight = 0.8f; // Height threshold for rolling under (future feature)
    public float obstacleAnalysisRange = 1.5f; // Range for analyzing obstacles ahead
    public int heightCheckRayCount = 5; // Number of raycasts for height analysis
    public bool autoNavigateObstacles = true; // Enable/disable automatic obstacle navigation
    public bool enableObstacleDebugLogs = true; // Enable/disable debug logging

    // SYSTEM OVERVIEW: All objects in 'obstacleLayers' are automatically analyzed for height.
    // The system determines action based on measured height: WalkOver, Vault, RollUnder, or TooHigh.
    // No need for separate "vaultable" layers - height analysis handles everything intelligently!

    public enum ObstacleType
    {
        None,
        WalkOver,    // Too low, just walk over
        Vault,       // Perfect height for vaulting
        RollUnder,   // Medium height, could roll under (future)
        TooHigh      // Too high to navigate
    }

    [Header("Input and Movement State")]
    protected Vector3 movementInput; // Stores the current movement input
    private bool isDashing = false; // Whether the player is currently dashing
    private bool isVaulting = false; // Whether the player is currently vaulting

    [Header("Dash State")]
    private float dashTime = 0f; // Timer for the current dash
    private float dashCooldownTime = 0f; // Timer for dash cooldown
    private Vector3 currentDirection; // Current direction the player is moving in
    private float dashTurnSpeed = 5f; // Speed at which the player can turn while dashing

    [Header("Vault State")]
    private Vector3 vaultTargetPosition; // Target position for vaulting

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
        
        // Debug vault layer information
        if (enableObstacleDebugLogs)
        {
            LayerMask combinedLayers = GetCombinedObstacleLayers();
            Debug.Log($"[VaultSetup] {gameObject.name} obstacle navigation system initialized. ObstacleLayers: {combinedLayers.value}");
            Debug.Log($"[VaultSetup] All obstacles will be analyzed for height to determine vaultability automatically");
            Debug.Log($"[VaultSetup] Height ranges: WalkOver(<{minVaultHeight}m), Vault({minVaultHeight}-{maxVaultHeight}m), TooHigh(>{maxVaultHeight}m)");
        }
    }

    public virtual void PossessedUpdate()
    {
        // Debug log every 120 frames to avoid spam
        if (enableObstacleDebugLogs && Time.frameCount % 120 == 0 && movementInput.magnitude > 0.1f)
        {
            Debug.Log($"[PossessedUpdate] Running for {gameObject.name}. Movement input: {movementInput}, AutoNav: {autoNavigateObstacles}");
        }
        
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
        if (!isVaulting)
        {
            movementInput = new Vector3(movement.x, 0, movement.z);
        }
    }

    public void Attack()
    {
        if (!isDashing && !isVaulting && characterInventory.equippedWeaponScriptObj != null)
        {
            isAttacking = true;
            animator.SetBool("LightAttack", true);
        }
    }

    public void Dash()
    {
        if (!isDashing && !isVaulting && Time.time > dashCooldownTime && movementInput.magnitude > 0.1f)
        {
            StopAttacking(); // Ensure player stops attacking when dashing

            if (CanVault(out RaycastHit hitInfo))
            {
                CalculateVaultTarget(hitInfo);
                StartVault();
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

    private bool CanVault(out RaycastHit hitInfo)
    {
        // Legacy method - now uses the enhanced height-based system
        if (movementInput.magnitude > 0.1f)
        {
            ObstacleType obstacleType = AnalyzeObstacle(transform.forward, out hitInfo, enableLogs: false);
            return obstacleType == ObstacleType.Vault || obstacleType == ObstacleType.RollUnder;
        }
        
        hitInfo = default;
        return false;
    }

    private void CalculateVaultTarget(RaycastHit hitInfo)
    {
        // Calculate the vault target position by moving past the hit point in the player's forward direction
        vaultTargetPosition = hitInfo.point + transform.forward * vaultOffset;
        vaultTargetPosition.y = transform.position.y; // Keep the target position at player's current y level
    }

    private void StartVault()
    {
        Debug.Log($"[Vault] Starting vault for {gameObject.name}. Target position: {vaultTargetPosition}");
        isVaulting = true;
        isDashing = false; // Ensure dashing is stopped when starting a vault
        dashTime = 0f; // Reset dash timer
        animator.SetTrigger("IsVaulting"); // Trigger vault animation
        humanCollider.enabled = false; // Disable the player's collider to avoid collision during vaulting
        Debug.Log($"[Vault] Vault initiated - isVaulting: {isVaulting}, collider disabled for {gameObject.name}");
    }

    private void FinishVault()
    {
        Debug.Log($"[Vault] Finishing vault for {gameObject.name}");
        isVaulting = false;
        humanCollider.enabled = true; // Re-enable the player's collider
        Debug.Log($"[Vault] Vault finished - isVaulting: {isVaulting}, collider re-enabled for {gameObject.name}");
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
        
        if (!autoNavigateObstacles || direction.magnitude < 0.1f)
        {
            // Only log these issues once every 120 frames to avoid spam
            if (enableObstacleDebugLogs && enableLogs && Time.frameCount % 120 == 0)
            {
                if (!autoNavigateObstacles)
                    Debug.Log($"[ObstacleNav] Auto navigation disabled for {gameObject.name}");
                else if (direction.magnitude < 0.1f)
                    Debug.Log($"[ObstacleNav] Direction magnitude too low: {direction.magnitude} for {gameObject.name}");
            }
            return ObstacleType.None;
        }

        // Check for obstacles in the movement direction
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f; // Start just above ground
        float maxCheckHeight = transform.position.y + maxVaultHeight + 0.5f;
        
        // First, check if there's any obstacle at all at chest height
        Vector3 chestRayOrigin = transform.position + Vector3.up * vaultHeight;
        if (!Physics.Raycast(chestRayOrigin, direction.normalized, out obstacleInfo, obstacleAnalysisRange, GetCombinedObstacleLayers()))
        {
            // Enhanced debugging - check if there's ANY object in that direction (regardless of layer)
            if (enableObstacleDebugLogs && enableLogs && Physics.Raycast(chestRayOrigin, direction.normalized, out RaycastHit anyHit, obstacleAnalysisRange))
            {
                Debug.Log($"[ObstacleNav] FOUND object '{anyHit.collider.name}' on layer {anyHit.collider.gameObject.layer} ({LayerMask.LayerToName(anyHit.collider.gameObject.layer)}) at distance {anyHit.distance}, but it's not on obstacleLayer {GetCombinedObstacleLayers().value}");
                Debug.Log($"[ObstacleNav] Ray from {chestRayOrigin} toward {direction.normalized} with range {obstacleAnalysisRange}");
            }
            else if (enableObstacleDebugLogs && enableLogs && Time.frameCount % 300 == 0) // Only log "no objects" every 300 frames (5 seconds at 60fps)
            {
                Debug.Log($"[ObstacleNav] NO objects found in any layer. Ray from {chestRayOrigin} toward {direction.normalized} with range {obstacleAnalysisRange}");
            }
            
            // Only log no obstacle detected occasionally
            if (enableObstacleDebugLogs && enableLogs && Time.frameCount % 60 == 0)
                Debug.Log($"[ObstacleNav] No obstacle detected at chest height for {gameObject.name}. ObstacleLayers: {GetCombinedObstacleLayers().value}, Range: {obstacleAnalysisRange}");
            return ObstacleType.None;
        }

        // Always log when we detect an obstacle (this is important) - but only once per detection
        if (enableObstacleDebugLogs && enableLogs)
            Debug.Log($"[ObstacleNav] Obstacle detected for {gameObject.name} at {obstacleInfo.point}, distance: {obstacleInfo.distance}");

        // Analyze the height of the obstacle using multiple raycasts
        float obstacleHeight = AnalyzeObstacleHeight(direction, obstacleInfo.point, enableLogs);
        
        // Always log classification results (important for debugging)
        if (enableObstacleDebugLogs && enableLogs)
            Debug.Log($"[ObstacleNav] Obstacle height analyzed: {obstacleHeight}m for {gameObject.name}");
        
        // Determine obstacle type based on height and always log the classification
        if (obstacleHeight <= minVaultHeight)
        {
            if (enableObstacleDebugLogs && enableLogs)
                Debug.Log($"[ObstacleNav] Obstacle classified as WalkOver (height: {obstacleHeight} <= {minVaultHeight}) for {gameObject.name}");
            return ObstacleType.WalkOver;
        }
        else if (obstacleHeight <= rollUnderHeight)
        {
            if (enableObstacleDebugLogs && enableLogs)
                Debug.Log($"[ObstacleNav] Obstacle classified as RollUnder (height: {obstacleHeight} <= {rollUnderHeight}) for {gameObject.name}");
            return ObstacleType.RollUnder; // Future feature
        }
        else if (obstacleHeight <= maxVaultHeight)
        {
            // ALWAYS log vault classification - this is critical (but only when enableLogs is true)
            if (enableLogs)
                Debug.Log($"[ObstacleNav] Obstacle classified as Vault (height: {obstacleHeight} <= {maxVaultHeight}) for {gameObject.name}");
            return ObstacleType.Vault;
        }
        else
        {
            // ALWAYS log too high classification - this is important (but only when enableLogs is true)
            if (enableLogs)
                Debug.Log($"[ObstacleNav] Obstacle classified as TooHigh (height: {obstacleHeight} > {maxVaultHeight}) for {gameObject.name}");
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
        
        // Only log start of analysis when we actually detect an obstacle
        if (enableObstacleDebugLogs && enableLogs)
            Debug.Log($"[ObstacleHeight] Starting height analysis for {gameObject.name}. Player ground level: {playerGroundLevel}");
        
        int hitCount = 0;
        
        // Cast rays at different heights to find the top of the obstacle
        for (int i = 0; i < heightCheckRayCount; i++)
        {
            float checkHeight = playerGroundLevel + (maxVaultHeight + 1f) * ((float)i / (heightCheckRayCount - 1));
            Vector3 rayOrigin = new Vector3(transform.position.x, checkHeight, transform.position.z);
            
            if (Physics.Raycast(rayOrigin, direction.normalized, out RaycastHit hit, obstacleAnalysisRange, GetCombinedObstacleLayers()))
            {
                // If we hit something at this height, the obstacle extends at least this high
                highestHitPoint = Mathf.Max(highestHitPoint, checkHeight);
                hitCount++;
                // Only log first and last hits to reduce spam
                if (enableObstacleDebugLogs && enableLogs && (i == 0 || i == heightCheckRayCount - 1))
                    Debug.Log($"[ObstacleHeight] Hit at height {checkHeight}m (ray {i}/{heightCheckRayCount}) for {gameObject.name}");
            }
            else if (highestHitPoint > playerGroundLevel)
            {
                // We didn't hit anything at this height, so we've found the top
                if (enableObstacleDebugLogs && enableLogs)
                    Debug.Log($"[ObstacleHeight] No hit at height {checkHeight}m, found top at {highestHitPoint}m for {gameObject.name}");
                break;
            }
        }
        
        float finalHeight = highestHitPoint - playerGroundLevel;
        // ALWAYS log final result - this is critical for debugging
        if (enableLogs)
            Debug.Log($"[ObstacleHeight] Final calculated height: {finalHeight}m (hit {hitCount}/{heightCheckRayCount} rays) for {gameObject.name}");
        
        return finalHeight;
    }

    /// <summary>
    /// Enhanced vault detection that works with the new obstacle analysis system
    /// </summary>
    /// <param name="direction">Direction of movement</param>
    /// <param name="hitInfo">Hit information for the obstacle</param>
    /// <returns>True if vaulting is possible and recommended</returns>
    private bool CanVaultEnhanced(Vector3 direction, out RaycastHit hitInfo)
    {
        ObstacleType obstacleType = AnalyzeObstacle(direction, out hitInfo);
        return obstacleType == ObstacleType.Vault;
    }

    /// <summary>
    /// Calculates vault target position for the enhanced system
    /// </summary>
    /// <param name="hitInfo">Obstacle hit information</param>
    /// <param name="direction">Movement direction</param>
    private void CalculateVaultTargetEnhanced(RaycastHit hitInfo, Vector3 direction)
    {
        // Calculate the vault target position by moving past the hit point in the movement direction
        vaultTargetPosition = hitInfo.point + direction.normalized * vaultOffset;
        vaultTargetPosition.y = transform.position.y; // Keep the target position at player's current y level
        
        if (enableObstacleDebugLogs)
            Debug.Log($"[VaultTarget] Initial target calculated for {gameObject.name}: {vaultTargetPosition} (hit point: {hitInfo.point}, direction: {direction.normalized}, offset: {vaultOffset})");
        
        // Ensure the target position is valid (not inside another obstacle)
        if (Physics.CheckSphere(vaultTargetPosition, capsuleCastRadius, GetCombinedObstacleLayers()))
        {
            // If the calculated position is blocked, try extending further
            vaultTargetPosition = hitInfo.point + direction.normalized * (vaultOffset * 1.5f);
            if (enableObstacleDebugLogs)
                Debug.Log($"[VaultTarget] Target position was blocked, extended to: {vaultTargetPosition} for {gameObject.name}");
        }
        
        // ALWAYS log final target - this is critical
        Debug.Log($"[VaultTarget] Final vault target for {gameObject.name}: {vaultTargetPosition}");
    }

    /// <summary>
    /// Public method to check what type of obstacle is in a given direction
    /// Useful for AI systems or external scripts
    /// </summary>
    /// <param name="direction">Direction to check</param>
    /// <param name="obstacleInfo">Information about the detected obstacle</param>
    /// <returns>Type of obstacle detected</returns>
    public ObstacleType CheckObstacleInDirection(Vector3 direction, out RaycastHit obstacleInfo)
    {
        return AnalyzeObstacle(direction.normalized, out obstacleInfo, enableLogs: true);
    }

    /// <summary>
    /// Public method to get the current movement input (useful for AI systems)
    /// </summary>
    /// <returns>Current movement input vector</returns>
    public Vector3 GetCurrentMovementInput()
    {
        return movementInput;
    }

    /// <summary>
    /// Public method to check if automatic obstacle navigation is enabled
    /// </summary>
    /// <returns>True if auto navigation is enabled</returns>
    public bool IsAutoNavigationEnabled()
    {
        return autoNavigateObstacles;
    }

    /// <summary>
    /// Public method to toggle automatic obstacle navigation
    /// </summary>
    /// <param name="enabled">Whether to enable auto navigation</param>
    public void SetAutoNavigation(bool enabled)
    {
        autoNavigateObstacles = enabled;
    }

    /// <summary>
    /// Debug method to check what layers objects around the player are on
    /// Call this in the inspector or console to help identify layer issues
    /// </summary>
    [ContextMenu("Debug Nearby Object Layers")]
    public void DebugNearbyObjectLayers()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * vaultHeight;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, transform.forward, obstacleAnalysisRange * 2f);
        
        Debug.Log($"[LayerDebug] Checking objects in front of {gameObject.name}:");
        Debug.Log($"[LayerDebug] Ray from {rayOrigin} forward {obstacleAnalysisRange * 2f} units");
        Debug.Log($"[LayerDebug] Current obstacleLayers setting: {GetCombinedObstacleLayers().value}");
        
        if (hits.Length == 0)
        {
            Debug.Log($"[LayerDebug] No objects found in front of player");
        }
        else
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
                bool isOnObstacleLayer = ((1 << hit.collider.gameObject.layer) & GetCombinedObstacleLayers()) != 0;
                
                // If it's on an obstacle layer, analyze its height to determine action
                string actionText = "✗ NOT on obstacle layer";
                if (isOnObstacleLayer)
                {
                    // Simulate height analysis for this object
                    float estimatedHeight = hit.collider.bounds.size.y;
                    if (estimatedHeight <= minVaultHeight)
                        actionText = "✓ OBSTACLE - will WALK OVER (low height)";
                    else if (estimatedHeight <= maxVaultHeight)
                        actionText = "✓ OBSTACLE - will VAULT OVER (good height)";
                    else
                        actionText = "✓ OBSTACLE - TOO HIGH to vault";
                }
                
                Debug.Log($"[LayerDebug] {i+1}. '{hit.collider.name}' at distance {hit.distance:F2} on layer {hit.collider.gameObject.layer} ('{layerName}') - {actionText}");
            }
        }
    }

    /// <summary>
    /// Quick toggle to disable all obstacle navigation debug logging
    /// </summary>
    [ContextMenu("Toggle Debug Logs")]
    public void ToggleDebugLogs()
    {
        enableObstacleDebugLogs = !enableObstacleDebugLogs;
        Debug.Log($"[ObstacleNav] Debug logging {(enableObstacleDebugLogs ? "ENABLED" : "DISABLED")} for {gameObject.name}");
    }

    #endregion

    #region Movement

    protected void MoveCharacter()
    {
        if (isVaulting)
        {
            // Move player towards the vault target position during vaulting
            float step = vaultSpeed * Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, vaultTargetPosition, step);

            // Snap player to the target vault position and end vault if close enough
            if (Vector3.Distance(transform.position, vaultTargetPosition) < 0.1f)
            {
                transform.position = vaultTargetPosition; // Snap to position
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
                        // If it's vaultable, start vaulting
                        CalculateVaultTarget(hitInfo);
                        StartVault();
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
                    // Only log every 30 frames to reduce spam
                    bool shouldLog = enableObstacleDebugLogs && Time.frameCount % 30 == 0;
                    if (shouldLog)
                        Debug.Log($"[Movement] Enhanced obstacle detection active for {gameObject.name}. Input magnitude: {inputMagnitude}");
                    
                    ObstacleType obstacleType = AnalyzeObstacle(movementInput.normalized, out RaycastHit obstacleInfo);
                    
                    // Only log when obstacle type changes or when we detect something important
                    if (enableObstacleDebugLogs && (obstacleType != ObstacleType.None || shouldLog))
                        Debug.Log($"[Movement] Obstacle analysis result: {obstacleType} for {gameObject.name}");
                    
                    switch (obstacleType)
                    {
                        case ObstacleType.WalkOver:
                            // Low obstacle, just continue normal movement
                            if (shouldLog)
                                Debug.Log($"[Movement] WalkOver - continuing normal movement for {gameObject.name}");
                            break;
                            
                        case ObstacleType.Vault:
                            // Perfect height for vaulting, automatically start vault - ALWAYS LOG THIS
                            Debug.Log($"[Movement] Vault detected - starting automatic vault for {gameObject.name}");
                            CalculateVaultTargetEnhanced(obstacleInfo, movementInput.normalized);
                            StartVault();
                            return; // Exit early since we're now vaulting
                            
                        case ObstacleType.RollUnder:
                            // Future: implement rolling under - ALWAYS LOG THIS
                            Debug.Log($"[Movement] RollUnder detected - checking if vaultable for {gameObject.name}");
                            if (AnalyzeObstacleHeight(movementInput.normalized, obstacleInfo.point, enableLogs: true) <= maxVaultHeight)
                            {
                                Debug.Log($"[Movement] RollUnder treated as vault for {gameObject.name}");
                                CalculateVaultTargetEnhanced(obstacleInfo, movementInput.normalized);
                                StartVault();
                                return;
                            }
                            break;
                            
                        case ObstacleType.TooHigh:
                            // Obstacle too high, stop movement in that direction - ALWAYS LOG THIS
                            Debug.Log($"[Movement] TooHigh obstacle - stopping movement for {gameObject.name}");
                            targetMovement = Vector3.zero;
                            break;
                            
                        case ObstacleType.None:
                        default:
                            // No obstacle, continue normal movement
                            if (shouldLog)
                                Debug.Log($"[Movement] No obstacle detected - continuing normal movement for {gameObject.name}");
                            break;
                    }
                }
                else
                {
                    // Only log state changes, not every frame
                    if (enableObstacleDebugLogs && Time.frameCount % 60 == 0)
                    {
                        if (!autoNavigateObstacles)
                            Debug.Log($"[Movement] Auto navigate disabled for {gameObject.name}");
                        else if (inputMagnitude <= 0.1f)
                            Debug.Log($"[Movement] Input magnitude too low: {inputMagnitude} for {gameObject.name}");
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
        Gizmos.DrawLine(rayOrigin, rayOrigin + transform.forward * vaultDetectionRange);
        Gizmos.DrawWireSphere(rayOrigin + transform.forward * vaultDetectionRange, 0.1f);

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
                else if (checkHeight - transform.position.y <= rollUnderHeight)
                    Gizmos.color = Color.blue; // Roll under range
                else if (checkHeight - transform.position.y <= maxVaultHeight)
                    Gizmos.color = new Color(1f, 0.5f, 0f); // Vault range (orange)
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
                        Gizmos.color = Color.blue;
                        break;
                    case ObstacleType.TooHigh:
                        Gizmos.color = Color.red;
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
            Gizmos.DrawWireSphere(vaultTargetPosition, 0.2f);
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
        
        // Height threshold indicators
        if (Application.isPlaying)
        {
            // Min vault height
            Gizmos.color = Color.green;
            Vector3 minHeightPos = transform.position + Vector3.up * minVaultHeight;
            Gizmos.DrawWireCube(minHeightPos + transform.forward * 0.5f, new Vector3(1f, 0.05f, 0.1f));
            
            // Roll under height
            Gizmos.color = Color.blue;
            Vector3 rollHeightPos = transform.position + Vector3.up * rollUnderHeight;
            Gizmos.DrawWireCube(rollHeightPos + transform.forward * 0.5f, new Vector3(1f, 0.05f, 0.1f));
            
            // Max vault height
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Vector3 maxHeightPos = transform.position + Vector3.up * maxVaultHeight;
            Gizmos.DrawWireCube(maxHeightPos + transform.forward * 0.5f, new Vector3(1f, 0.05f, 0.1f));
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
