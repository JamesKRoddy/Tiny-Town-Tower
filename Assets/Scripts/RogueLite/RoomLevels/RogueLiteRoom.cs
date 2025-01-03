using UnityEngine;

public class RogueLiteRoom : MonoBehaviour
{
    [Header("Walls")]
    [SerializeField] RogueLiteWall frontWalls;
    [SerializeField] RogueLiteWall backWalls;
    [SerializeField] RogueLiteWall leftWalls;
    [SerializeField] RogueLiteWall rightWalls;

    public void Setup(RoomPosition roomPosition)
    {
        switch (roomPosition)
        {
            case RoomPosition.FRONT:
                frontWalls.Setup(WallType.DISABLED);
                backWalls.Setup(WallType.ENABLED);
                leftWalls.Setup(WallType.ENABLED);
                rightWalls.Setup(WallType.ENABLED);
                break;
            case RoomPosition.BACK:
                frontWalls.Setup(WallType.DISABLED);
                backWalls.Setup(WallType.ENABLED);
                leftWalls.Setup(WallType.ENABLED);
                rightWalls.Setup(WallType.ENABLED);
                break;
            case RoomPosition.LEFT:
                frontWalls.Setup(WallType.DISABLED);
                backWalls.Setup(WallType.ENABLED);
                leftWalls.Setup(WallType.ENABLED);
                rightWalls.Setup(WallType.ENABLED);
                break;
            case RoomPosition.RIGHT:
                frontWalls.Setup(WallType.DISABLED);
                backWalls.Setup(WallType.ENABLED);
                leftWalls.Setup(WallType.ENABLED);
                rightWalls.Setup(WallType.ENABLED);
                break;
            default:
                break;
        }
    }
}
