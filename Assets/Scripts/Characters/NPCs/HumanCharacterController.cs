using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Windows;
using Managers;

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
    protected CharacterCombat characterCombat;
    protected NavMeshAgent agent; // Reference to NavMeshAgent
    protected CharacterInventory characterInventory;

    [Header("Vault Parameters")]
    public float vaultSpeed = 5f; // Slower speed for vaulting
    public float vaultDetectionRange = 1.0f; // Range to detect vaultable obstacles
    public LayerMask[] obstacleLayers; // Array of layers for non-vaultable obstacles
    public LayerMask vaultLayer; // Layer specifically for vaultable obstacles
    public float capsuleCastRadius = 0.5f; // Radius of the capsule for collision detection
    public float vaultHeight = 1.0f; // Height of the raycast to detect vaultable obstacles
    public float vaultOffset = 1.0f; // Distance to move beyond the obstacle after vaulting
    private Collider humanCollider;

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

    public float Health { get => health; set => health = value; }
    public float MaxHealth { get => maxHealth; set => maxHealth = value; }
    public CharacterType CharacterType => characterType;

    protected virtual void Awake()
    {
        // Store the reference to NavMeshAgent once
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        humanCollider = GetComponent<Collider>();
        characterCombat = GetComponent<CharacterCombat>();
        characterInventory = GetComponent<CharacterInventory>();
    }

    public void PossessedUpdate()
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
        PlayerController.Instance.playerCamera.UpdateTarget(transform);
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
            GetComponent<CharacterAnimationEvents>().Setup();
        }
        else
        {
            settlerNPC?.ChangeState(null);
            GetComponent<CharacterAnimationEvents>().Setup(characterCombat, this, characterInventory);
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
        if (characterInventory.equippedWeaponScriptObj != null)
        {
            // Update the speed of all attack animations in the Attacking Layer
            animator.SetFloat("AttackSpeed", characterInventory.equippedWeaponScriptObj.attackSpeed);
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
        // Cast a ray from the player's chest height to detect obstacles suitable for vaulting
        Vector3 rayOrigin = transform.position + Vector3.up * vaultHeight;

        // Cast a ray forward to see if there's an object to vault over
        if (Physics.Raycast(rayOrigin, transform.forward, out hitInfo, vaultDetectionRange, vaultLayer))
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
        vaultTargetPosition = hitInfo.point + transform.forward * vaultOffset;
        vaultTargetPosition.y = transform.position.y; // Keep the target position at player's current y level
    }

    private void StartVault()
    {
        isVaulting = true;
        isDashing = false; // Ensure dashing is stopped when starting a vault
        dashTime = 0f; // Reset dash timer
        animator.SetTrigger("IsVaulting"); // Trigger vault animation
        humanCollider.enabled = false; // Disable the player's collider to avoid collision during vaulting
    }

    private void FinishVault()
    {
        isVaulting = false;
        humanCollider.enabled = true; // Re-enable the player's collider
    }

    #endregion

    #region Movement

    private void MoveCharacter()
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
        Vector3 capsuleBottom = transform.position + Vector3.up * 0.1f; // Slightly above ground to avoid terrain issues
        Vector3 capsuleTop = transform.position + Vector3.up * humanCollider.bounds.size.y;

        // Check against each obstacle layer
        foreach (LayerMask layer in obstacleLayers)
        {
            // Perform a capsule cast to detect obstacles in the path using the current obstacle layer
            if (Physics.CapsuleCast(capsuleBottom, capsuleTop, capsuleCastRadius, direction.normalized, out hitInfo, capsuleCastRadius, layer | vaultLayer))
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

    #endregion

    private void OnDrawGizmos()
    {
        // Vault detection raycast visualization
        Gizmos.color = Color.cyan;
        Vector3 rayOrigin = transform.position + Vector3.up * vaultHeight;
        Gizmos.DrawLine(rayOrigin, rayOrigin + transform.forward * vaultDetectionRange);
        Gizmos.DrawWireSphere(rayOrigin + transform.forward * vaultDetectionRange, 0.1f);

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
    }

    public Allegiance GetAllegiance() => Allegiance.FRIENDLY;

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
        // TODO: Implement death behavior
        Debug.Log($"{gameObject.name} has died!");
        OnDeath?.Invoke();

        // Play death VFX
        Vector3 deathPoint = transform.position + Vector3.up * 1.5f;
        Vector3 deathNormal = Vector3.up; // Default upward direction for death effects
        EffectManager.Instance.PlayDeathEffect(deathPoint, deathNormal, this);
    }
}
