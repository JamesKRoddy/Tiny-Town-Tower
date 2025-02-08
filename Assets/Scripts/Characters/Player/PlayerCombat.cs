using UnityEngine;

public class PlayerCombat : CharacterCombat
{
    // Static instance of the PlayerUIManager class
    private static PlayerCombat _instance;

    // Public property to access the instance
    public static PlayerCombat Instance
    {
        get
        {
            // Check if the instance is null
            if (_instance == null)
            {
                // Try to find the PlayerCombat in the scene
                _instance = FindFirstObjectByType<PlayerCombat>();

                // If not found, log a warning
                if (_instance == null)
                {
                    Debug.LogWarning("PlayerCombat instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    protected override void Awake()
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

        base.Awake();
    }
}
