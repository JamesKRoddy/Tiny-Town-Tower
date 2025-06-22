using UnityEngine;

/// <summary>
/// This class is used to store the references to the overworld.
/// </summary>
public class OverworldReferences : MonoBehaviour
{
    private static OverworldReferences _instance;

    // Singleton property to get the instance
    public static OverworldReferences Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find the GameManager instance if it hasn't been assigned
                _instance = FindFirstObjectByType<OverworldReferences>();
                if (_instance == null)
                {
                    Debug.LogWarning("OverworldReferences instance not found in the scene!");
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
    }

    [SerializeField] private Transform overWorldSpawnPoint;
    public Transform OverWorldSpawnPoint => overWorldSpawnPoint;
}
