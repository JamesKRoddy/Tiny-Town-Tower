using UnityEngine;
using System.Collections.Generic;
using Managers;

public class RogueLiteRoom : MonoBehaviour
{
    [Header("Room Settings")]
    public int roomDifficulty;
    
    [Header("Room Components")]
    public List<RogueLiteDoor> doors = new List<RogueLiteDoor>();
    public List<ChestParent> chests = new List<ChestParent>();
    
    private void Awake()
    {
        // Cache all doors and chests in the room
        doors.AddRange(GetComponentsInChildren<RogueLiteDoor>());
        chests.AddRange(GetComponentsInChildren<ChestParent>());
        Debug.Log($"RogueLiteRoom {gameObject.name} initialized with {doors.Count} doors and {chests.Count} chests");
    }

    public void Setup()
    {        
        Debug.Log($"Setting up room {gameObject.name} at position {transform.position}");
        
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
}
