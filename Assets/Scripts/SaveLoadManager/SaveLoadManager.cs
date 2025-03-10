using System;
using System.IO;
using UnityEngine;

[Serializable]
public class GameData //This is all just placeholder shit
{
    //public int playerLevel;
    //public float playerHealth;
    //public Vector3 playerPosition;
}

public class SaveLoadManager : MonoBehaviour
{
    private static SaveLoadManager _instance;
    public static SaveLoadManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SaveLoadManager>();
                if (_instance == null)
                {
                    Debug.LogError("SaveLoadManager instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    private string saveFilePath;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
    }

    public void SaveGame()
    {
        GameData data = new GameData
        {
            //playerLevel = PlayerStats.Instance.Level,
            //playerHealth = PlayerStats.Instance.Health,
            //playerPosition = PlayerController.Instance.transform.position
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game Saved: " + saveFilePath);
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            GameData data = JsonUtility.FromJson<GameData>(json);

            //PlayerStats.Instance.Level = data.playerLevel;
            //PlayerStats.Instance.Health = data.playerHealth;
            //PlayerController.Instance.transform.position = data.playerPosition;

            Debug.Log("Game Loaded");
        }
        else
        {
            Debug.LogWarning("No save file found");
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("Save file deleted");
        }
        else
        {
            Debug.LogWarning("No save file to delete");
        }
    }
}
