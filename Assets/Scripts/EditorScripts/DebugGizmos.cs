using UnityEngine;

[ExecuteAlways]
public class DebugGizmos : MonoBehaviour
{
    public bool showForwardArrow = true;
    public Color arrowColor = Color.green;
    public float arrowLength = 2f;

    public bool showColliderBounds = true;
    public Color boundsColor = Color.cyan;

    public bool showUpArrow = false;
    public Color upArrowColor = Color.yellow;

    private void OnDrawGizmos()
    {
        // Draw Forward Arrow
        if (showForwardArrow)
        {
            Gizmos.color = arrowColor;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.forward * arrowLength;
            DrawArrow(startPos, endPos);
        }

        // Draw Up Arrow
        if (showUpArrow)
        {
            Gizmos.color = upArrowColor;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.up * arrowLength;
            DrawArrow(startPos, endPos);
        }

        // Draw Collider Bounds
        if (showColliderBounds)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = boundsColor;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }

    private void DrawArrow(Vector3 from, Vector3 to)
    {
        Gizmos.DrawLine(from, to);
        Vector3 direction = (to - from).normalized;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
        float headLength = arrowLength * 0.2f;

        Gizmos.DrawLine(to, to + right * headLength);
        Gizmos.DrawLine(to, to + left * headLength);
    }
}
