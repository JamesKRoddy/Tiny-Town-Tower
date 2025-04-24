using UnityEngine;
using Managers;

public class PlayerCamera : MonoBehaviour, IControllerInput
{
    [Header("Follow Camera")]
    [SerializeField, ReadOnly] private Transform target; // The player or target to follow
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -10); // Default offset position
    [SerializeField] private float followSpeed = 5f; // Speed at which the camera follows the target
    [SerializeField] private float rotationSpeed = 5f; // Speed at which the camera rotates to match the target

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
            case PlayerControlType.TURRET_PLACEMENT:
                break;
            case PlayerControlType.TURRET_CAMERA_MOVEMENT:
                target = defaultTarget; // Detach from target to allow free camera movement
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystickInput; // Subscribe to joystick input
                break;
            case PlayerControlType.CAMP_CAMERA_MOVEMENT:
                target = defaultTarget; // Detach from target to allow free camera movement
                PlayerInput.Instance.OnLeftJoystick += HandleLeftJoystickInput; // Subscribe to joystick input
                break;
            case PlayerControlType.CAMP_WORK_ASSIGNMENT:
                target = defaultTarget; // Detach from target to allow free camera movement
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

        // Smoothly interpolate the camera's position to the target position + offset
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // Smoothly rotate the camera to look at the target
        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void HandleCameraPanning()
    {
        Vector3 panMovement = new Vector3(joystickInput.x, 0, joystickInput.y) * panSpeed * Time.deltaTime;

        // Clamp the camera's new position within the X and Z bounds
        Vector3 newPosition = defaultTarget.position + panMovement;
        // Clamp the position to the grid bounds based on game mode
        switch (GameManager.Instance.CurrentGameMode)
        {
            case GameMode.TURRET:
                newPosition.x = Mathf.Clamp(newPosition.x, TurretPlacer.Instance.GetXBounds().x, TurretPlacer.Instance.GetXBounds().y);
                newPosition.z = Mathf.Clamp(newPosition.z, TurretPlacer.Instance.GetZBounds().x, TurretPlacer.Instance.GetZBounds().y);
                break;
            case GameMode.CAMP:
                newPosition.x = Mathf.Clamp(newPosition.x, BuildingPlacer.Instance.GetXBounds().x, BuildingPlacer.Instance.GetXBounds().y);
                newPosition.z = Mathf.Clamp(newPosition.z, BuildingPlacer.Instance.GetZBounds().x, BuildingPlacer.Instance.GetZBounds().y);
                break;
            default:
                Debug.LogWarning($"Camera bounds not set for game mode: {GameManager.Instance.CurrentGameMode}");
                break;
        }

        defaultTarget.position = newPosition;
    }

    private void HandleLeftJoystickInput(Vector2 input)
    {
        joystickInput = input; // Update joystick input for panning
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
}
