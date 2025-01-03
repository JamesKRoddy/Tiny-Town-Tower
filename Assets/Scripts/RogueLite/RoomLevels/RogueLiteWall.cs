using UnityEngine;

public enum WallType
{
    ENABLED, //Model can be seen, collider enabled
    DISABLED, //Model and collider disabled
    HIDDEN //Model disabled, collider enabled
}

public class RogueLiteWall : MonoBehaviour
{
    public GameObject wallWithModel;
    public GameObject wallWithNoModel;

    public void Setup(WallType wallType)
    {
        switch (wallType)
        {
            case WallType.ENABLED:
                wallWithModel.SetActive(true);
                wallWithNoModel.SetActive(false);
                break;
            case WallType.DISABLED:
                wallWithModel.SetActive(false);
                wallWithNoModel.SetActive(false);
                break;
            case WallType.HIDDEN:
                wallWithModel.SetActive(false);
                wallWithNoModel.SetActive(true);
                break;
            default:
                break;
        }
    }
}
