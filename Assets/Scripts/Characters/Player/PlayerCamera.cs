using UnityEngine;
using Managers;

public class PlayerCamera : MonoBehaviour, IControllerInput
{
    [Header("Follow Camera")]
    [SerializeField, ReadOnly] private Transform target; // The player or target to follow
    [SerializeField] private Vector3 offset = new Vector3(-8, 12, -8); // Side-angled offset position
    [SerializeField] private float followSpeed = 5f; // Speed at which the camera follows the target
    [SerializeField] private float rotationSpeed = 5f; // Speed at which the camera rotates to match the target
    [SerializeField] private float cameraYAngle = 30f; // Y-axis rotation angle of the camera in degrees

    [Header("Panning Camera")]
    [SerializeField] private Transform defaultTarget; // The default target to follow for camera panning
    [SerializeField] private float panSpeed = 20f; // Speed at which the camera pans

    [Header("Work Assignment")]
    [SerializeField] private GameObject workDetectionPoint; // Point used to detect work buildings
    [SerializeField] private float workDetectionDistance = 5f; // Distance to check for work buildings

    private Vector2 joystickInput; // Stores the current joystick input

    private void Awake()
    {
        // Subscribe to control type updates
        PlayerInput.Instance.OnUpdatePlayerControls += SetPlayerControlType;
    }

    private void OnDestroy()
    {
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

        // Calculate the rotated offset based on the camera Y angle
        Quaternion yRotation = Quaternion.Euler(0, cameraYAngle, 0);
        Vector3 rotatedOffset = yRotation * offset;
        
        // Smoothly interpolate the camera's position to the target position + rotated offset
        Vector3 targetPosition = target.position + rotatedOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // Make the camera look at the target
        Vector3 lookDirection = target.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
    /// Transforms input coordinates to world coordinates accounting for camera angle
    /// </summary>
    public Vector3 TransformInputToWorldCoordinates(Vector3 inputVector)
    {
        // Get the camera's forward and right directions relative to the ground plane
        Vector3 cameraForward = transform.forward;
        Vector3 cameraRight = transform.right;
        
        // Project these directions onto the ground plane (Y = 0)
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        // Transform the input: 
        // - inputVector.z (forward/back on stick) maps to camera forward direction
        // - inputVector.x (left/right on stick) maps to camera right direction
        Vector3 transformedInput = cameraRight * inputVector.x + cameraForward * inputVector.z;
        
        return transformedInput;
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

    private void ClampPositionToBounds()
    {
        Vector3 newPosition = transform.position;

        // Use PlacementManager bounds if available, otherwise use default bounds
        Vector2 xBounds = new Vector2(-25f, 25f);
        Vector2 zBounds = new Vector2(-25f, 25f);

        if (Managers.PlacementManager.Instance != null)
        {
            xBounds = Managers.PlacementManager.Instance.GetXBounds();
            zBounds = Managers.PlacementManager.Instance.GetZBounds();
        }

        newPosition.x = Mathf.Clamp(newPosition.x, xBounds.x, xBounds.y);
        newPosition.z = Mathf.Clamp(newPosition.z, zBounds.x, zBounds.y);

        transform.position = newPosition;
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
        else if (Managers.PlacementManager.Instance != null)
        {
            xBounds = Managers.PlacementManager.Instance.GetXBounds();
            zBounds = Managers.PlacementManager.Instance.GetZBounds();
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
}
