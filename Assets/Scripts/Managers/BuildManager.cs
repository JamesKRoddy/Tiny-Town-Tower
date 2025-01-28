using UnityEngine;

public class BuildManager : MonoBehaviour
{
    // Singleton instance
    private static BuildManager _instance;

    // Singleton property to get the instance
    public static BuildManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find the GameManager instance if it hasn't been assigned
                _instance = FindFirstObjectByType<BuildManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("BuildManager instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            _instance = this; // Set the instance
        }
    }

    [Header("Build Grid")]
    [SerializeField] private Vector2 xBounds = new Vector2(-25f, 25f); // X-axis bounds for turret grid
    [SerializeField] private Vector2 zBounds = new Vector2(-25f, 25f); // Z-axis bounds for turret grid
    [SerializeField] bool showGridBounds;

    public Vector2 GetXBounds() => xBounds;
    public Vector2 GetZBounds() => zBounds;


    private void OnDrawGizmos()
    {
        if (showGridBounds)
        {
            Gizmos.color = Color.green;

            // Draw a rectangular outline to visualize the panning bounds
            Vector3 bottomLeft = new Vector3(TurretManager.Instance.GetXBounds().x, 0, TurretManager.Instance.GetZBounds().x);
            Vector3 bottomRight = new Vector3(TurretManager.Instance.GetXBounds().y, 0, TurretManager.Instance.GetZBounds().x);
            Vector3 topLeft = new Vector3(TurretManager.Instance.GetXBounds().x, 0, TurretManager.Instance.GetZBounds().y);
            Vector3 topRight = new Vector3(TurretManager.Instance.GetXBounds().y, 0, TurretManager.Instance.GetZBounds().y);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}
