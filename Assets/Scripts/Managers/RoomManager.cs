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
        }

        public void EnterRoom(Vector3 position, RogueLiteDoor entranceDoor)
        {
            if (!roomGrid.ContainsKey(position))
            {
                Debug.LogError($"Attempted to enter non-existent room at {position}");
                return;
            }

            roomHistory.Push(currentRoomPosition);
            currentRoomPosition = position;

            RoomData roomData = roomGrid[position];
            roomData.roomComponent.OnRoomEntered(entranceDoor);
        }

        public void ExitRoom(Vector3 position, RogueLiteDoor exitDoor)
        {
            if (!roomGrid.ContainsKey(position))
            {
                Debug.LogError($"Attempted to exit non-existent room at {position}");
                return;
            }

            RoomData roomData = roomGrid[position];
            roomData.roomComponent.OnRoomExited(exitDoor);
        }

        public bool CanMoveBack()
        {
            return roomHistory.Count > 0;
        }

        public Vector3 GetPreviousRoomPosition()
        {
            return roomHistory.Count > 0 ? roomHistory.Peek() : Vector3.zero;
        }

        public RoomData GetCurrentRoom()
        {
            return roomGrid.ContainsKey(currentRoomPosition) ? roomGrid[currentRoomPosition] : null;
        }

        public RoomData GetRoomAtPosition(Vector3 position)
        {
            return roomGrid.ContainsKey(position) ? roomGrid[position] : null;
        }

        public void ClearCurrentRoom()
        {
            if (roomGrid.ContainsKey(currentRoomPosition))
            {
                roomGrid[currentRoomPosition].isCleared = true;
            }
        }
    } 
}