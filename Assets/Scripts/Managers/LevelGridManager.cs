using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class LevelGridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 50;
        [SerializeField] private int gridHeight = 50;
        [SerializeField] private float cellSize = 10f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;
        
        [Header("Debug")]
        [SerializeField] private bool showGrid = false;
        [SerializeField] private Color gridColor = Color.white;
        
        private bool[,] occupiedCells;
        private Dictionary<Vector2Int, RoomPlacementData> roomGrid;
        
        private void Awake()
        {
            InitializeGrid();
        }
        
        private void InitializeGrid()
        {
            occupiedCells = new bool[gridWidth, gridHeight];
            roomGrid = new Dictionary<Vector2Int, RoomPlacementData>();
        }
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 localPosition = worldPosition - gridOrigin;
            int x = Mathf.RoundToInt(localPosition.x / cellSize);
            int z = Mathf.RoundToInt(localPosition.z / cellSize);
            return new Vector2Int(x, z);
        }
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return gridOrigin + new Vector3(gridPosition.x * cellSize, 0, gridPosition.y * cellSize);
        }
        
        /// <summary>
        /// Check if a room can be placed at the given grid position
        /// </summary>
        public bool CanPlaceRoom(Vector2Int gridPosition, Vector2Int roomSize)
        {
            // Check if the room fits within the grid bounds
            if (gridPosition.x < 0 || gridPosition.y < 0 || 
                gridPosition.x + roomSize.x > gridWidth || 
                gridPosition.y + roomSize.y > gridHeight)
            {
                return false;
            }
            
            // Check if any cells are already occupied
            for (int x = gridPosition.x; x < gridPosition.x + roomSize.x; x++)
            {
                for (int y = gridPosition.y; y < gridPosition.y + roomSize.y; y++)
                {
                    if (occupiedCells[x, y])
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Mark grid cells as occupied by a room
        /// </summary>
        public void PlaceRoom(Vector2Int gridPosition, Vector2Int roomSize, RoomPlacementData roomData)
        {
            for (int x = gridPosition.x; x < gridPosition.x + roomSize.x; x++)
            {
                for (int y = gridPosition.y; y < gridPosition.y + roomSize.y; y++)
                {
                    occupiedCells[x, y] = true;
                }
            }
            
            roomGrid[gridPosition] = roomData;
        }
        
        /// <summary>
        /// Find the best position for a room near the given position
        /// </summary>
        public Vector3 FindBestRoomPosition(Vector3 nearPosition, Vector2Int roomSize, Vector3 preferredDirection)
        {
            Vector2Int startGrid = WorldToGrid(nearPosition);
            Vector2Int directionGrid = WorldToGrid(preferredDirection.normalized);
            
            // Search in expanding rings around the preferred position
            for (int radius = 1; radius <= 10; radius++)
            {
                List<Vector2Int> candidatePositions = GetPositionsInRadius(startGrid, radius);
                
                // Sort by preference (closer to preferred direction)
                candidatePositions.Sort((a, b) => 
                {
                    float distA = Vector2.Distance(a, startGrid + directionGrid * radius);
                    float distB = Vector2.Distance(b, startGrid + directionGrid * radius);
                    return distA.CompareTo(distB);
                });
                
                foreach (var pos in candidatePositions)
                {
                    if (CanPlaceRoom(pos, roomSize))
                    {
                        return GridToWorld(pos);
                    }
                }
            }
            
            // Fallback to original position if no better position found
            return GridToWorld(startGrid);
        }
        
        private List<Vector2Int> GetPositionsInRadius(Vector2Int center, int radius)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                    {
                        int distance = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                        if (distance == radius)
                        {
                            positions.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// Get room size in grid cells based on bounds
        /// </summary>
        public Vector2Int GetRoomGridSize(Bounds roomBounds)
        {
            int width = Mathf.CeilToInt(roomBounds.size.x / cellSize);
            int height = Mathf.CeilToInt(roomBounds.size.z / cellSize);
            return new Vector2Int(width, height);
        }
        
        /// <summary>
        /// Clear all occupied cells
        /// </summary>
        public void ClearGrid()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    occupiedCells[x, y] = false;
                }
            }
            roomGrid.Clear();
        }
        
        private void OnDrawGizmos()
        {
            if (!showGrid) return;
            
            Gizmos.color = gridColor;
            
            // Draw grid lines
            for (int x = 0; x <= gridWidth; x++)
            {
                Vector3 start = GridToWorld(new Vector2Int(x, 0));
                Vector3 end = GridToWorld(new Vector2Int(x, gridHeight));
                Gizmos.DrawLine(start, end);
            }
            
            for (int y = 0; y <= gridHeight; y++)
            {
                Vector3 start = GridToWorld(new Vector2Int(0, y));
                Vector3 end = GridToWorld(new Vector2Int(gridWidth, y));
                Gizmos.DrawLine(start, end);
            }
            
            // Draw occupied cells
            if (occupiedCells != null)
            {
                Gizmos.color = Color.red;
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        if (occupiedCells[x, y])
                        {
                            Vector3 cellCenter = GridToWorld(new Vector2Int(x, y));
                            Gizmos.DrawCube(cellCenter, Vector3.one * cellSize * 0.8f);
                        }
                    }
                }
            }
        }
    }
} 