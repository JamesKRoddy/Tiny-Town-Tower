using UnityEngine;
using System.Collections.Generic;
using Managers;

public class RogueLiteRoom : MonoBehaviour
{
    [Header("Room Components")]
    public List<RogueLikeRoomDoor> doors = new List<RogueLikeRoomDoor>();
    public List<ChestParent> chests = new List<ChestParent>();
    
    private void Awake()
    {
        // Cache all doors and chests in the room
        doors.AddRange(GetComponentsInChildren<RogueLikeRoomDoor>());
        chests.AddRange(GetComponentsInChildren<ChestParent>());
    }

    public void Setup()
    {                
        // Initialize all doors
        foreach (var door in doors)
        {
            door.Initialize(this);
        }
        
        // Initialize all chests
        foreach (var chest in chests)
        {
            chest.SetupChest(DifficultyManager.Instance.GetCurrentRoomDifficulty());
        }
    }
}
