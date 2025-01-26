using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform target;          // The player or target to follow
    public Vector3 offset = new Vector3(0, 10, -10);  // Default offset position
    public float followSpeed = 5f;    // Speed at which the camera follows the target
    public float rotationSpeed = 5f;  // Speed at which the camera rotates to match the target

    void LateUpdate()
    {
        if (target == null) return;

        // Smoothly interpolate the camera's position to the target position + offset
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // Smoothly rotate the camera to look at the target
        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
