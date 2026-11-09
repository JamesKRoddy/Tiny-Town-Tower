using UnityEngine;
using Managers;

/// <summary>
/// Handles all camera functionality including following targets, panning, work detection, and screen shake effects.
/// Camera shake automatically triggers on damage events through IDamageable interface integration.
/// </summary>
public class PlayerCamera : MonoBehaviour, IControllerInput
{
    public static PlayerCamera Instance { get; private set; }

    #region Follow Camera Settings

    [Header("Follow Camera")]
    [SerializeField, ReadOnly] private Transform target; // The player or target to follow
    [SerializeField] private Vector3 offset = new Vector3(-8, 12, -8); // Side-angled offset position
    [SerializeField] private float followSpeed = 5f; // Speed at which the camera follows the target
    [SerializeField] private float rotationSpeed = 5f; // Speed at which the camera rotates to match the target
    [SerializeField] private float cameraYAngle = 30f; // Y-axis rotation angle of the camera in degrees

    #endregion

    #region Panning Camera Settings

    [Header("Panning Camera")]
    [SerializeField] private Transform defaultTarget; // The default target to follow for camera panning
    [SerializeField] private float panSpeed = 20f; // Speed at which the camera pans

    #endregion

    #region Work Assignment Settings

    [Header("Work Assignment")]
    [SerializeField] private GameObject workDetectionPoint; // Point used to detect work buildings
    [SerializeField] private float workDetectionDistance = 5f; // Distance to check for work buildings

    #endregion

    #region Camera Shake Settings

    [Header("Camera Shake - Default Settings")]
    [Tooltip("How long the default shake lasts in seconds")]
    [SerializeField] private float defaultShakeDuration = 0.2f;
    [Tooltip("How strong the default shake is (higher = more intense)")]
    [SerializeField] private float defaultShakeIntensity = 0.3f;
    [Tooltip("How fast the shake oscillates (higher = faster/jittery)")]
    [SerializeField] private float defaultShakeFrequency = 25f;

    [Header("Camera Shake - Damage Scaling")]
    [Tooltip("Minimum damage required to trigger a shake")]
    [SerializeField] private float minDamageForShake = 5f;
    [Tooltip("Damage amount that results in maximum shake intensity")]
    [SerializeField] private float maxDamageForFullShake = 50f;
    [Tooltip("Maximum shake intensity from taking damage")]
    [SerializeField] private float maxDamageShakeIntensity = 1.0f;

    [Header("Camera Shake - Hit Enemy")]
    [Tooltip("Shake intensity when player hits an enemy")]
    [SerializeField] private float hitEnemyIntensity = 0.15f;
    [Tooltip("Shake duration when player hits an enemy")]
    [SerializeField] private float hitEnemyDuration = 0.1f;

    [Header("Camera Shake - Death")]
    [Tooltip("Shake intensity when player dies")]
    [SerializeField] private float deathShakeIntensity = 0.8f;
    [Tooltip("Shake duration when player dies")]
    [SerializeField] private float deathShakeDuration = 0.5f;

    #endregion

    #region Private Variables

    private Vector2 joystickInput; // Stores the current joystick input
    
    // Camera shake state
    private Vector3 originalLocalPosition;
    private Coroutine currentShakeCoroutine;
    private bool isShaking;

    #endregion

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Store original local position for shake calculations
        originalLocalPosition = transform.localPosition;

        // Subscribe to control type updates
        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // Unsubscribe from all input events to prevent memory leaks
        PlayerInput.Instance.OnUpdatePlayerControls -= SetPlayerControlType;
        PlayerInput.Instance.OnLeftJoystick -= HandleLeftJoystickInput;
    }

    public void SetPlayerControlType(PlayerControlType controlType)
    {
        workDetectionPoint.SetActive(false); // Disable work detection point for other modes
        // Handle new control type
        switch (controlType)
        {
            case PlayerControlType.CAMP_ATTACK_CAMERA_MOVEMENT:
                UpdateTarget(defaultTarget); // Detach from target to allow free camera movement
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystickInput; // Subscribe to joystick input
                break;
            case PlayerControlType.CAMP_CAMERA_MOVEMENT:
                UpdateTarget(defaultTarget); // Detach from target to allow free camera movement
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystickInput; // Subscribe to joystick input
                workDetectionPoint.SetActive(true); // Enable work detection point
                break;
            case PlayerControlType.CAMP_WORK_ASSIGNMENT:
                UpdateTarget(defaultTarget); // Detach from target to allow free camera movement
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystickInput; // Subscribe to joystick input
                workDetectionPoint.SetActive(true); // Enable work detection point
                break;
            default:
                workDetectionPoint.SetActive(false); // Disable work detection point for other modes
                break;
        }
    }

    private void LateUpdate()
    {
        if (target == defaultTarget)
        {
            HandleCameraPanning();
            ClampWorkDetectionPointToBounds(); // Clamp the work detection point to bounds
        }

        FollowTarget();
    }

    public void UpdateTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void FollowTarget()
    {
        if (target == null)
            return;

        // FIXED ISOMETRIC ROTATION - never changes, always consistent for input calculations
        // This ensures transform.eulerAngles.y is ALWAYS the same value (cameraYAngle)
        transform.rotation = Quaternion.Euler(45f, cameraYAngle, 0f);

        // Calculate the rotated offset based on the camera Y angle
        Quaternion yRotation = Quaternion.Euler(0, cameraYAngle, 0);
        Vector3 rotatedOffset = yRotation * offset;
        
        // OPTION 1: Instant follow (best for input consistency)
        Vector3 targetPosition = target.position + rotatedOffset;
        transform.position = targetPosition;
        
        // OPTION 2: Very fast lerp (smooth but still responsive) - UNCOMMENT if you want slight smoothing
        // Vector3 targetPosition = target.position + rotatedOffset;
        // transform.position = Vector3.Lerp(transform.position, targetPosition, 20f * Time.deltaTime);
    }

    private void HandleCameraPanning()
    {
        // Transform input to world coordinates accounting for camera angle
        Vector3 panMovement = TransformInputToWorldCoordinates(new Vector3(joystickInput.x, 0, joystickInput.y)) * panSpeed * Time.deltaTime;

        // Calculate new position for the default target
        Vector3 newPosition = defaultTarget.position + panMovement;
        
        // Clamp the default target's position within the X and Z bounds
        Vector2 xBounds = new Vector2(-25f, 25f);
        Vector2 zBounds = new Vector2(-25f, 25f);

        if (CampManager.Instance != null)
        {
            xBounds = CampManager.Instance.SharedXBounds;
            zBounds = CampManager.Instance.SharedZBounds;
        }

        newPosition.x = Mathf.Clamp(newPosition.x, xBounds.x, xBounds.y);
        newPosition.z = Mathf.Clamp(newPosition.z, zBounds.x, zBounds.y);

        defaultTarget.position = newPosition;
    }

    private void HandleLeftJoystickInput(Vector2 input)
    {
        joystickInput = input; // Update joystick input for panning
    }

    /// <summary>
    /// ===== REBUILT INPUT SYSTEM =====
    /// Convert 2D stick input to 3D world movement.
    /// 
    /// THE PROBLEM:
    /// Controllers give inconsistent magnitude (0.81-1.06) around the edge due to hardware variance.
    /// Diagonals (45°, 135°, etc.) often report lower magnitudes than cardinal directions.
    /// 
    /// THE FIX (Hades-style):
    /// - Dead zone handled by Input System StickDeadzone processor (0.125 min)
    /// - If stick magnitude > 0.7, treat it as FULL EXTENSION (1.0) in that direction.
    /// - Use smooth magnitude transition to avoid popping
    /// </summary>
    public Vector3 ConvertStickInputToWorldMovement(Vector2 stickInput)
    {
        float inputMagnitude = stickInput.magnitude;
        
        // Dead zone is now handled by Input System's StickDeadzone processor
        // But keep a small threshold for safety
        if (inputMagnitude < 0.05f)
        {
            return Vector3.zero; // No input
        }
        
        // STEP 1: Handle stick magnitude variance (Hades-style)
        // Lower threshold to 0.7 to catch more diagonal cases where controllers underreport
        // Use smooth transition instead of hard snap for values near the threshold
        float normalizedMagnitude;
        
        const float fullSpeedThreshold = 0.7f;
        const float minActiveThreshold = 0.1f;
        
        if (inputMagnitude >= fullSpeedThreshold)
        {
            // Full speed - snap to 1.0 for consistent movement
            normalizedMagnitude = 1.0f;
        }
        else if (inputMagnitude < minActiveThreshold)
        {
            return Vector3.zero; // Below active threshold
        }
        else
        {
            // Smooth remap from [minActiveThreshold, fullSpeedThreshold] → [0, 1.0]
            normalizedMagnitude = (inputMagnitude - minActiveThreshold) / (fullSpeedThreshold - minActiveThreshold);
            // Apply a slight curve for more natural acceleration feel
            normalizedMagnitude = normalizedMagnitude * normalizedMagnitude; // Quadratic ease-in
        }
        
        // STEP 2: Get the direction (already normalized by Input System's NormalizeVector2 processor)
        // But normalize again to be safe in case raw values slip through
        Vector2 stickDirection = inputMagnitude > 0.001f ? stickInput / inputMagnitude : Vector2.zero;
        Vector3 input3D = new Vector3(stickDirection.x, 0, stickDirection.y);
        
        // STEP 3: Rotate input by camera's Y-axis only (ignore camera pitch)
        float cameraYaw = transform.eulerAngles.y;
        Quaternion yawRotation = Quaternion.Euler(0, cameraYaw, 0);
        Vector3 worldDirection = yawRotation * input3D;
        
        // STEP 4: Apply the normalized magnitude
        return worldDirection * normalizedMagnitude;
    }
    
    /// <summary>
    /// Legacy method for camera panning
    /// </summary>
    public Vector3 TransformInputToWorldCoordinates(Vector3 inputVector)
    {
        return ConvertStickInputToWorldMovement(new Vector2(inputVector.x, inputVector.z));
    }

    // Method to get the work detection point's position
    public Vector3 GetWorkDetectionPoint()
    {
        return workDetectionPoint.transform.position;
    }

    // Method to check for work tasks at the detection point
    public WorkTask GetWorkTaskAtDetectionPoint()
    {
        if (!workDetectionPoint.activeSelf) return null;

        // Cast a sphere to detect work tasks
        Collider[] hitColliders = Physics.OverlapSphere(workDetectionPoint.transform.position, workDetectionDistance);
        foreach (var hitCollider in hitColliders)
        {
            WorkTask workTask = hitCollider.GetComponent<WorkTask>();
            if (workTask != null)
            {
                return workTask;
            }
        }
        return null;
    }

    /// <summary>
    /// Clamps the work detection point position to the camp's grid bounds
    /// </summary>
    private void ClampWorkDetectionPointToBounds()
    {
        if (workDetectionPoint == null) return;

        Vector3 currentPosition = workDetectionPoint.transform.position;

        // Get bounds from CampManager if available, otherwise use PlacementManager
        Vector2 xBounds = new Vector2(-25f, 25f);
        Vector2 zBounds = new Vector2(-25f, 25f);

        if (CampManager.Instance != null)
        {
            xBounds = CampManager.Instance.SharedXBounds;
            zBounds = CampManager.Instance.SharedZBounds;
        }
        else if (CampManager.Instance.PlacementManager != null)
        {
            xBounds = CampManager.Instance.PlacementManager.GetXBounds();
            zBounds = CampManager.Instance.PlacementManager.GetZBounds();
        }

        // Clamp the position to the bounds
        Vector3 clampedPosition = new Vector3(
            Mathf.Clamp(currentPosition.x, xBounds.x, xBounds.y),
            currentPosition.y,
            Mathf.Clamp(currentPosition.z, zBounds.x, zBounds.y)
        );

        // Only update if the position actually changed
        if (clampedPosition != currentPosition)
        {
            workDetectionPoint.transform.position = clampedPosition;
        }
    }

    #region Camera Shake System

    /// <summary>
    /// Triggers camera shake based on damage amount received.
    /// Called automatically through IDamageable.OnDamageTaken event.
    /// Shake intensity scales with damage amount up to maxDamageForFullShake.
    /// </summary>
    /// <param name="damageAmount">Amount of damage taken</param>
    /// <param name="remainingHealth">Remaining health after damage (unused but from event signature)</param>
    public void ShakeFromDamage(float damageAmount, float remainingHealth)
    {
        if (damageAmount < minDamageForShake) return;

        // Calculate intensity based on damage amount (normalized between min and max)
        float normalizedDamage = Mathf.Clamp01((damageAmount - minDamageForShake) / (maxDamageForFullShake - minDamageForShake));
        float intensity = Mathf.Lerp(defaultShakeIntensity, maxDamageShakeIntensity, normalizedDamage);
        
        // Duration scales slightly with damage for heavier hits
        float duration = Mathf.Lerp(defaultShakeDuration, defaultShakeDuration * 1.5f, normalizedDamage);
        
        TriggerShake(duration, intensity, defaultShakeFrequency);
    }

    /// <summary>
    /// Triggers a small shake when the player hits an enemy.
    /// Provides subtle haptic-like feedback for successful hits.
    /// </summary>
    public void ShakeFromHittingEnemy()
    {
        TriggerShake(hitEnemyDuration, hitEnemyIntensity, defaultShakeFrequency);
    }

    /// <summary>
    /// Triggers a strong shake when the player character dies.
    /// Called automatically through IDamageable.OnDeath event.
    /// </summary>
    public void ShakeFromDeath()
    {
        TriggerShake(deathShakeDuration, deathShakeIntensity, defaultShakeFrequency * 0.5f);
    }

    /// <summary>
    /// Triggers a camera shake with custom parameters.
    /// Can be called manually for custom shake effects (explosions, impacts, etc).
    /// </summary>
    /// <param name="duration">How long the shake lasts in seconds</param>
    /// <param name="intensity">How strong the shake is (position offset magnitude)</param>
    /// <param name="frequency">How fast the shake oscillates</param>
    public void TriggerShake(float duration, float intensity, float frequency)
    {
        // Stop any current shake before starting a new one
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
        }
        
        currentShakeCoroutine = StartCoroutine(ShakeCoroutine(duration, intensity, frequency));
    }

    /// <summary>
    /// Triggers a shake with default settings.
    /// </summary>
    public void TriggerShake()
    {
        TriggerShake(defaultShakeDuration, defaultShakeIntensity, defaultShakeFrequency);
    }

    /// <summary>
    /// Coroutine that performs the actual camera shake using Perlin noise for smooth, natural movement.
    /// The shake decays over time (starts strong, fades to zero) for a more polished feel.
    /// </summary>
    private System.Collections.IEnumerator ShakeCoroutine(float duration, float intensity, float frequency)
    {
        isShaking = true;
        float elapsed = 0f;
        
        // Store the position before shake (in case camera has moved during shake)
        Vector3 startPosition = transform.localPosition;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Calculate decay over time (starts at 1.0, ends at 0.0)
            float percentComplete = elapsed / duration;
            float damper = 1.0f - Mathf.Clamp01(percentComplete);
            
            // Generate smooth random shake offset using Perlin noise
            // Using Time.time with frequency creates continuous noise sampling
            float x = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f;
            float z = (Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) - 0.5f) * 2f;
            
            // Apply shake with decay (stronger at start, weaker at end)
            Vector3 shakeOffset = new Vector3(x, y, z) * intensity * damper;
            transform.localPosition = startPosition + shakeOffset;
            
            yield return null;
        }
        
        // Reset to original position when complete
        transform.localPosition = startPosition;
        isShaking = false;
        currentShakeCoroutine = null;
    }

    /// <summary>
    /// Stops any ongoing shake immediately and resets camera position.
    /// </summary>
    public void StopShake()
    {
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
            currentShakeCoroutine = null;
        }
        
        transform.localPosition = originalLocalPosition;
        isShaking = false;
    }

    /// <summary>
    /// Returns whether the camera is currently shaking.
    /// </summary>
    public bool IsShaking()
    {
        return isShaking;
    }

    #endregion
}
