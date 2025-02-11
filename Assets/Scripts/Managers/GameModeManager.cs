using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameModeManager<TWaveConfig, TBuildingData> : MonoBehaviour where TWaveConfig : ScriptableObject where TBuildingData : ScriptableObject
{
    [SerializeField] protected List<TWaveConfig> waveConfigs;
    [SerializeField] protected List<TBuildingData> buildingDataScriptableObjs;

    protected EnemySetupState enemySetupState;
    public Action<EnemySetupState> OnRoomSetupStateChanged;

    protected virtual void Start()
    {
        OnRoomSetupStateChanged += EnemySetupStateChanged;
        enemySetupState = EnemySetupState.ALL_WAVES_CLEARED;
    }

    protected virtual void OnDestroy()
    {
        OnRoomSetupStateChanged -= EnemySetupStateChanged;
    }

    protected abstract void EnemySetupStateChanged(EnemySetupState newState);

    public void SetEnemySetupState(EnemySetupState newState)
    {
        if (enemySetupState != newState)
        {
            enemySetupState = newState;
            OnRoomSetupStateChanged?.Invoke(enemySetupState);
        }
    }

    public EnemySetupState GetEnemySetupState()
    {
        return enemySetupState;
    }

    public EnemyWaveConfig GetWaveConfig(int difficulty)
    {
        foreach (var config in waveConfigs)
        {
            if (config is EnemyWaveConfig enemyWaveConfig && enemyWaveConfig.enemyWaveDifficulty <= difficulty)
            {
                return enemyWaveConfig;
            }
        }
        Debug.LogWarning($"No matching waveConfig found for difficulty {difficulty}. Returning null.");
        return null;
    }

    public GameObject GetBuildingParent(BuildingType buildingType, int difficulty, out BuildingDataScriptableObj selectedBuilding)
    {
        foreach (var buildingData in buildingDataScriptableObjs)
        {
            if (buildingData is BuildingDataScriptableObj building && building.buildingType == buildingType)
            {
                selectedBuilding = building;
                return building.GetBuildingParent(difficulty);
            }
        }
        Debug.LogWarning($"No building parent found for type {buildingType} with difficulty {difficulty}.");
        selectedBuilding = null;
        return null;
    }
}
