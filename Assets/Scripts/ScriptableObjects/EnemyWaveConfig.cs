using UnityEngine;

public enum BuildingType
{
    Dungeon,
    Tower,
    Castle,
    Cave
}

[CreateAssetMenu(fileName = "EnemyWaveConfig", menuName = "Scriptable Objects/Enemies/EnemyWaveConfig", order = 1)]
public class EnemyWaveConfig : ScriptableObject
{
    public GameObject[] enemyPrefabs;  // Array of enemy prefabs
    public int minWaves = 1;           // Minimum wave count
    public int maxWaves = 5;           // Maximum wave count
    public int minEnemiesPerWave = 1;  // Minimum enemies per wave
    public int maxEnemiesPerWave = 10; // Maximum enemies per wave

    // Contextual variables
    public BuildingType buildingType;
    public int floor; // The specific floor this config is designed for
}
