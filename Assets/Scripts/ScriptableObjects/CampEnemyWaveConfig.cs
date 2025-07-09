using UnityEngine;

[CreateAssetMenu(fileName = "CampEnemyWaveConfig", menuName = "Scriptable Objects/Camp/Enemies/CampEnemyWaveConfig", order = 1)]
public class CampEnemyWaveConfig : EnemyWaveConfig
{
    [Header("Camp Wave Specific Settings")]
    [SerializeField] private int campWaveDifficulty = 1;
    [SerializeField] private float waveDuration = 60f; // How long the wave lasts
    
    public int CampWaveDifficulty => campWaveDifficulty;
    public float WaveDuration => waveDuration;
}
