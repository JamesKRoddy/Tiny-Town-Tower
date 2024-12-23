using System;
using UnityEngine;

public class PlayerController : HumanCharacterController, IControllerInput
{
    // Static instance of the PlayerUIManager class
    private static PlayerController _instance;

    // Public property to access the instance
    public static PlayerController Instance
    {
        get
        {
            // Check if the instance is null
            if (_instance == null)
            {
                // Try to find the PlayerCombat in the scene
                _instance = FindFirstObjectByType<PlayerController>();

                // If not found, log a warning
                if (_instance == null)
                {
                    Debug.LogWarning("PlayerController instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    [Header("NPC Possesion")]
    public GameObject possesedNPC;

    [Header("Camera")]
    public CameraFollow playerCamera;

    [Header("Movement Parameters")]
    public float dashSpeed = 20f; // Speed during a dash
    public float dashDuration = 0.2f; // How long a dash lasts
    public float dashCooldown = 1.0f; // Cooldown time between dashes


    [Header("Vault Parameters")]
    public float vaultSpeed = 5f; // Slower speed for vaulting
    public float vaultDetectionRange = 1.0f; // Range to detect vaultable obstacles
    public LayerMask obstacleLayer; // Layer for non-vaultable obstacles
    public LayerMask vaultLayer; // Layer specifically for vaultable obstacles
    public float capsuleCastRadius = 0.5f; // Radius of the capsule for collision detection
    public float vaultHeight = 1.0f; // Height of the raycast to detect vaultable obstacles
    public float vaultOffset = 1.0f; // Distance to move beyond the obstacle after vaulting

    [Header("Input and Movement State")]
    private Vector3 movementInput; // Stores the current movement input
    private bool isDashing = false; // Whether the player is currently dashing
    private bool isVaulting = false; // Whether the player is currently vaulting

    [Header("Dash State")]
    private float dashTime = 0f; // Timer for the current dash
    private float dashCooldownTime = 0f; // Timer for dash cooldown
    private Vector3 currentDirection; // Current direction the player is moving in
    private float dashTurnSpeed = 5f; // Speed at which the player can turn while dashing

    [Header("Vault State")]
    private Vector3 vaultTargetPosition; // Target position for vaulting

    private Collider playerCollider;
    private PlayerCombat playerCombat;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            _instance = this; // Set the instance
            DontDestroyOnLoad(gameObject); // Optionally persist across scenes
        }

        playerCollider = possesedNPC.GetComponent<Collider>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    void Start()
    {
        if(possesedNPC != null)
            ToggleNPCComponents(false, possesedNPC);

        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    private void OnDestroy()
    {
        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
    }

    public void ToggleNPCComponents(bool isAIControlled)
    {
        base.ToggleNPCComponents(isAIControlled, possesedNPC);
    }

    private void Update()
    {
        HandleDash();
        MoveCharacter();
        UpdateAnimations();
    }

    /// <summary>
    /// Updates player controls based on the given PlayerControlType.
    /// </summary>
    /// <param name="controlType">The desired control type.</param>
    public void SetPlayerControlType(PlayerControlType controlType)
    {
        // Subscribe to events based on the new control type
        switch (controlType)
        {
            case PlayerControlType.NONE:
                // No controls are active
                break;
            case PlayerControlType.COMBAT_MOVEMENT:
                if (PlayerInput.Instance != null)
                {
                    PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                    PlayerInput.Instance.OnYPressed += HandleYInput;
                    PlayerInput.Instance.OnAPressed += HandleAInput;
                    PlayerInput.Instance.OnSelectPressed += OpenCombatUtilityMenu;
                }
                break;
            case PlayerControlType.CAMP_MOVEMENT:
                if (PlayerInput.Instance != null)
                {
                    PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystick;
                    PlayerInput.Instance.OnSelectPressed += OpenCampUtilityMenu;
                }
                break;
            default:
                break;
        }
    }

    private void OpenCampUtilityMenu()
    {
        UtilityMenu.Instance.OpenMenu(PlayerControlType.CAMP_MOVEMENT);
    }

    private void OpenCombatUtilityMenu()
    {
        UtilityMenu.Instance.OpenMenu(PlayerControlType.COMBAT_MOVEMENT);
    }

    private void HandleLeftJoystick(Vector2 input)
    {
        if (!isVaulting && !isAttacking && PlayerInput.Instance.currentControlType != PlayerControlType.IN_CONVERSATION)
        {
            movementInput = new Vector3(input.x, 0, input.y);
        }
    }

    private void HandleAInput()
    {
        if (!isDashing && !isVaulting && PlayerInventory.Instance.equippedWeapon != null && PlayerInput.Instance.currentControlType == PlayerControlType.COMBAT_MOVEMENT)
        {
            isAttacking = true;
            animator.SetBool("LightAttack", true);
        }
    }

    private void HandleYInput()
    {
        if (!isDashing && !isVaulting && Time.time > dashCooldownTime && movementInput.magnitude > 0.1f && PlayerInput.Instance.currentControlType == PlayerControlType.COMBAT_MOVEMENT)
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

    private void HandleDash()
    {
        if (isDashing && Time.time >= dashTime)
        {
            // End dashing when the dash duration has elapsed
            isDashing = false;
        }
    }

    private bool CanVault(out RaycastHit hitInfo)
    {
        // Cast a ray from the player's chest height to detect obstacles suitable for vaulting
        Vector3 rayOrigin = possesedNPC.transform.position + Vector3.up * vaultHeight;

        // Cast a ray forward to see if there's an object to vault over
        if (Physics.Raycast(rayOrigin, possesedNPC.transform.forward, out hitInfo, vaultDetectionRange, vaultLayer))
        {
            // Only consider vaulting if the player is actively moving towards the object
            if (movementInput.magnitude > 0.1f)
            {
                return true;
            }
        }

        hitInfo = default;
        return false;
    }

    private void CalculateVaultTarget(RaycastHit hitInfo)
    {
        // Calculate the vault target position by moving past the hit point in the player's forward direction
        vaultTargetPosition = hitInfo.point + possesedNPC.transform.forward * vaultOffset;
        vaultTargetPosition.y = possesedNPC.transform.position.y; // Keep the target position at player's current y level
    }

    private void StartDash()
    {
        isDashing = true;
        dashTime = Time.time + dashDuration;
        dashCooldownTime = Time.time + dashCooldown;
        currentDirection = movementInput.normalized; // Initialize dash direction based on input
        animator.SetTrigger("IsDashing");

        playerCombat.DashVFX();
    }

    private void StartVault()
    {
        isVaulting = true;
        isDashing = false; // Ensure dashing is stopped when starting a vault
        dashTime = 0f; // Reset dash timer
        animator.SetTrigger("IsVaulting"); // Trigger vault animation
        playerCollider.enabled = false; // Disable the player's collider to avoid collision during vaulting
    }

    private void FinishVault()
    {
        isVaulting = false;
        playerCollider.enabled = true; // Re-enable the player's collider
    }

    private void MoveCharacter()
    {
        if (isVaulting)
        {
            // Move player towards the vault target position during vaulting
            float step = vaultSpeed * Time.deltaTime;
            possesedNPC.transform.position = Vector3.Lerp(possesedNPC.transform.position, vaultTargetPosition, step);

            // Snap player to the target vault position and end vault if close enough
            if (Vector3.Distance(possesedNPC.transform.position, vaultTargetPosition) < 0.1f)
            {
                possesedNPC.transform.position = vaultTargetPosition; // Snap to position
                FinishVault();
            }
        }
        else if (!isAttacking)
        {
            float inputMagnitude = movementInput.magnitude;
            float speed = isDashing ? dashSpeed : moveMaxSpeed;

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
                    // Check if it's a vaultable object
                    if (((1 << hitInfo.collider.gameObject.layer) & vaultLayer) != 0)
                    {
                        // If it's vaultable, start vaulting
                        CalculateVaultTarget(hitInfo);
                        StartVault();
                    }
                    else
                    {
                        // If not, stop dashing
                        isDashing = false;
                    }
                }
                else
                {
                    possesedNPC.transform.position += targetMovement;

                    // Rotate player towards the current direction
                    Quaternion targetRotation = Quaternion.LookRotation(currentDirection);
                    possesedNPC.transform.rotation = Quaternion.RotateTowards(possesedNPC.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // Regular movement behavior when not dashing
                Vector3 targetMovement = movementInput.normalized * speed * inputMagnitude * Time.deltaTime;

                if (!IsObstacleInPath(targetMovement, out RaycastHit hitInfo))
                {
                    possesedNPC.transform.position += targetMovement;

                    // Rotate player towards the input direction
                    if (movementInput != Vector3.zero && !isVaulting)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(movementInput);
                        possesedNPC.transform.rotation = Quaternion.RotateTowards(possesedNPC.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
        Vector3 capsuleBottom = possesedNPC.transform.position + Vector3.up * 0.1f; // Slightly above ground to avoid terrain issues
        Vector3 capsuleTop = possesedNPC.transform.position + Vector3.up * playerCollider.bounds.size.y;

        // Perform a capsule cast to detect obstacles in the path using obstacleLayer
        if (Physics.CapsuleCast(capsuleBottom, capsuleTop, capsuleCastRadius, direction.normalized, out hitInfo, capsuleCastRadius, obstacleLayer | vaultLayer))
        {
            return true;
        }

        hitInfo = default;
        return false;
    }

    protected override void UpdateAnimations()
    {
        float maxSpeed = isDashing ? dashSpeed : moveMaxSpeed;
        float currentSpeedNormalized = movementInput.magnitude * moveMaxSpeed / maxSpeed;

        animator.SetFloat("Speed", currentSpeedNormalized);
    }

    private void OnDrawGizmos()
    {
        // Vault detection raycast visualization
        Gizmos.color = Color.cyan;
        Vector3 rayOrigin = possesedNPC.transform.position + Vector3.up * vaultHeight;
        Gizmos.DrawLine(rayOrigin, rayOrigin + possesedNPC.transform.forward * vaultDetectionRange);
        Gizmos.DrawWireSphere(rayOrigin + possesedNPC.transform.forward * vaultDetectionRange, 0.1f);

        // Vault target position visualization
        if (isVaulting)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(vaultTargetPosition, 0.2f);
        }

        // Capsule cast for collision detection visualization
        Gizmos.color = Color.red;

        if (playerCollider != null)
        {
            Vector3 capsuleBottom = possesedNPC.transform.position + Vector3.up * 0.1f; // Slightly above ground
            Vector3 capsuleTop = possesedNPC.transform.position + Vector3.up * playerCollider.bounds.size.y;

            Gizmos.DrawWireSphere(capsuleBottom, capsuleCastRadius);
            Gizmos.DrawWireSphere(capsuleTop, capsuleCastRadius);
            Gizmos.DrawLine(capsuleBottom, capsuleTop);
        }
    }
}