using UnityEngine;
using System;

public enum CurrentGameMode
{
    NONE,
    ROGUE_LITE,
    CAMP,
    TURRET
}

public class GameManager : MonoBehaviour
{
    // Singleton instance
    private static GameManager _instance;

    // Singleton property to get the instance
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find the GameManager instance if it hasn't been assigned
                _instance = FindFirstObjectByType<GameManager>();
                if (_instance == null)
                {
                    Debug.LogError("GameManager instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    // Public event to notify when the game mode changes
    public event Action<CurrentGameMode> OnGameModeChanged;

    // Private backing field for the current game mode
    private CurrentGameMode _currentGameMode = CurrentGameMode.NONE;

    // Property to get and set the current game mode
    public CurrentGameMode CurrentGameMode
    {
        get { return _currentGameMode; }
        set
        {
            // Only trigger event if the mode changes
            if (_currentGameMode != value)
            {
                _currentGameMode = value;
                // Invoke the event when game mode changes
                OnGameModeChanged?.Invoke(_currentGameMode);
            }
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
            DontDestroyOnLoad(gameObject); // Optionally persist across scenes
        }
    }

    // Start is called once before the first execution of Update
    void Start()
    {
        // Example usage: Setting the initial game mode
        //CurrentGameMode = CurrentGameMode.ROGUE_LITE;
    }
}
