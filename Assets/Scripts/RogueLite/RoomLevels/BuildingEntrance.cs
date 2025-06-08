using UnityEngine;

public class BuildingEntrance : MonoBehaviour
{
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform[] weaponSpawnPoints;

    public Transform PlayerSpawnPoint => playerSpawnPoint;
    public Transform[] WeaponSpawnPoints => weaponSpawnPoints;
}
