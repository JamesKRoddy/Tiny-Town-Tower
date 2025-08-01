using UnityEngine;

public class RogueLikeBuildingEntrance : MonoBehaviour
{
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform[] weaponSpawnPoints;

    public Transform PlayerSpawnPoint => playerSpawnPoint;
    public Transform[] WeaponSpawnPoints => weaponSpawnPoints;
}
