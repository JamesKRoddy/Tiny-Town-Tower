using UnityEngine;

namespace Managers
{
    public class OverworldManager : MonoBehaviour
    {
        private Vector3 currentBuildingSpawnPoint; //Used for when the player returns to the overworld

        private Vector3 OverWorldSpawnPoint => OverworldReferences.Instance.OverWorldSpawnPoint.position;

        public Vector3 GetOverWorldSpawnPoint()
        {
            if(currentBuildingSpawnPoint != Vector3.zero){
                return currentBuildingSpawnPoint;
            }
            return OverWorldSpawnPoint;
        }

        public void EnteredBuilding(Transform spawnPoint)
        {
            currentBuildingSpawnPoint = spawnPoint.position;
        }

        public void ExitedBuilding()
        {
            SceneTransitionManager.Instance.LoadScene(SceneNames.OverworldScene, GameMode.ROGUE_LITE, true, OnSceneLoaded);
        }

        void OnSceneLoaded()
        {
            if (currentBuildingSpawnPoint != Vector3.zero)
            {
                currentBuildingSpawnPoint = Vector3.zero;
            }
        }
    }
}
