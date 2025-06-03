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
    } 
}