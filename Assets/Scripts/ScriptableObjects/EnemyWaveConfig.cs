using UnityEngine;

public class EnemyWaveConfig : ScriptableObject
{
    public GameObject[] enemyPrefabs;  // Array of enemy prefabs
    public int minWaves = 1;           // Minimum wave count
    public int maxWaves = 5;           // Maximum wave count
    public int minEnemiesPerWave = 1;  // Minimum enemies per wave
    public int maxEnemiesPerWave = 10; // Maximum enemies per wave
    public int enemyWaveDifficulty; // The specific difficulty this config is designed for
}
