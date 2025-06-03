using System.Collections.Generic;
using UnityEngine;
using Managers;

namespace Managers
{
    public class RoomManager : MonoBehaviour
    {
        [System.Serializable]
        public class RoomData
        {
            public GameObject roomObject;
            public RogueLiteRoom roomComponent;
            public Dictionary<Vector3, RogueLiteDoor> doors = new Dictionary<Vector3, RogueLiteDoor>();
            public int difficulty;
            public bool isCleared;
            public Vector3 position;
        }

        private Dictionary<Vector3, RoomData> roomGrid = new Dictionary<Vector3, RoomData>();
        private Vector3 currentRoomPosition;
        private Stack<Vector3> roomHistory = new Stack<Vector3>();
        private float roomSpacing = 100f; // Increased from 50f to 100f for more space between rooms

        public void RegisterRoom(GameObject roomObject, Vector3 position, int difficulty)
        {
            RogueLiteRoom roomComponent = roomObject.GetComponent<RogueLiteRoom>();
            if (roomComponent == null)
            {
                Debug.LogError($"Room at {position} has no RogueLiteRoom component!");
                return;
            }

            RoomData roomData = new RoomData
            {
                roomObject = roomObject,
                roomComponent = roomComponent,
                difficulty = difficulty,
                position = position
            };

            // Find and register all doors in the room
            RogueLiteDoor[] doors = roomObject.GetComponentsInChildren<RogueLiteDoor>();
            foreach (var door in doors)
            {
                roomData.doors[door.transform.position] = door;
            }

            roomGrid[position] = roomData;
            Debug.Log($"Registered room at position {position} with {doors.Length} doors");
        }

        public Vector3 CalculateNewRoomPosition(Vector3 currentPosition, RogueLiteDoor entranceDoor)
        {
            // Get the door's forward direction in world space
            Vector3 doorDirection = entranceDoor.transform.forward;
            
            // Calculate the new position based on the door's direction
            Vector3 newPosition = currentPosition + (doorDirection * roomSpacing);
            
            // Round the position to avoid floating point issues
            newPosition = new Vector3(
                Mathf.Round(newPosition.x / roomSpacing) * roomSpacing,
                Mathf.Round(newPosition.y / roomSpacing) * roomSpacing,
                Mathf.Round(newPosition.z / roomSpacing) * roomSpacing
            );
            
            Debug.Log($"Calculating new room position: Current={currentPosition}, Door Direction={doorDirection}, New={newPosition}");
            return newPosition;
        }

        public void EnterRoom(Vector3 position, RogueLiteDoor entranceDoor)
        {
            if (!roomGrid.ContainsKey(position))
            {
                Debug.LogError($"Attempted to enter non-existent room at {position}");
                return;
            }

            Debug.Log($"Entering room at {position} from door at {entranceDoor.transform.position}");
            
            // Store current room in history before updating
            if (currentRoomPosition != Vector3.zero)
            {
                roomHistory.Push(currentRoomPosition);
                Debug.Log($"Added previous room {currentRoomPosition} to history. History count: {roomHistory.Count}");
            }

            currentRoomPosition = position;

            RoomData roomData = roomGrid[position];
        }

        public RoomData GetCurrentRoom()
        {
            if (roomGrid.ContainsKey(currentRoomPosition))
            {
                Debug.Log($"Getting current room at {currentRoomPosition}");
                return roomGrid[currentRoomPosition];
            }
            Debug.LogWarning($"No room found at current position {currentRoomPosition}");
            return null;
        }

        public RoomData GetRoomAtPosition(Vector3 position)
        {
            if (roomGrid.ContainsKey(position))
            {
                Debug.Log($"Getting room at position {position}");
                return roomGrid[position];
            }
            Debug.LogWarning($"No room found at position {position}");
            return null;
        }
    } 
}