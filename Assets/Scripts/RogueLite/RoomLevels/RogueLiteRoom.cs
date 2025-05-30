using UnityEngine;
using System.Collections.Generic;
using Managers;

public class RogueLiteRoom : MonoBehaviour
{
    [Header("Room Settings")]
    public int roomDifficulty;
    public bool isCleared;
    public bool isLocked;
    
    [Header("Room Components")]
    public List<RogueLiteDoor> doors = new List<RogueLiteDoor>();
    public List<ChestParent> chests = new List<ChestParent>();
    
    private void Awake()
    {
        // Cache all doors and chests in the room
        doors.AddRange(GetComponentsInChildren<RogueLiteDoor>());
        chests.AddRange(GetComponentsInChildren<ChestParent>());
    }

    public void Setup()
    {
        isCleared = false;
        isLocked = false;
        
        // Initialize all doors
        foreach (var door in doors)
        {
            door.Initialize(this);
        }
        
        // Initialize all chests
        foreach (var chest in chests)
        {
            chest.SetupChest(roomDifficulty);
        }
    }

    public void OnRoomEntered(RogueLiteDoor entranceDoor)
    {
        // Handle room entry logic
        if (!isCleared && RogueLiteManager.Instance != null)
        {
            // Start room encounter
            RogueLiteManager.Instance.SetEnemySetupState(EnemySetupState.WAVE_START);
        }
    }

    public void OnRoomExited(RogueLiteDoor exitDoor)
    {
        // Handle room exit logic
        if (!isCleared && RogueLiteManager.Instance != null && RogueLiteManager.Instance.RoomManager != null)
        {
            // Mark room as cleared if all enemies are defeated
            if (RogueLiteManager.Instance.GetCurrentEnemyCount() == 0)
            {
                isCleared = true;
                RogueLiteManager.Instance.RoomManager.ClearCurrentRoom();
            }
        }
    }

    public void LockRoom()
    {
        isLocked = true;
        foreach (var door in doors)
        {
            door.SetLocked(true);
        }
    }

    public void UnlockRoom()
    {
        isLocked = false;
        foreach (var door in doors)
        {
            door.SetLocked(false);
        }
    }

    public void ResetRoom()
    {
        isCleared = false;
        isLocked = false;
        
        // Reset all doors
        foreach (var door in doors)
        {
            door.Reset();
        }
        
        // Reset all chests by setting them up again
        foreach (var chest in chests)
        {
            chest.SetupChest(roomDifficulty);
        }
    }
}
