using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Manages spawn points and scene transitions for the roguelike overworld.
    /// Tracks where the player should spawn based on their previous location.
    /// This manager persists across scenes, but references scene-specific OverworldReferences.
    /// </summary>
    public class OverworldManager : MonoBehaviour
    {
        // Tracks the door spawn point when entering buildings (used when returning to overworld)
        private Vector3 currentBuildingDoorSpawnPoint;
        
        // Flag to determine which spawn point to use
        private bool shouldUseDefaultSpawn = true;

        /// <summary>
        /// Gets the default overworld spawn point from the scene-specific OverworldReferences.
        /// Returns Vector3.zero if OverworldReferences is not available (scene not loaded).
        /// </summary>
        private Vector3 OverWorldSpawnPoint
        {
            get
            {
                if (OverworldReferences.Instance == null)
                {
                    Debug.LogWarning("[OverworldManager] OverworldReferences not found - overworld scene may not be loaded yet");
                    return Vector3.zero;
                }

                if (OverworldReferences.Instance.OverWorldSpawnPoint == null)
                {
                    Debug.LogError("[OverworldManager] OverWorldSpawnPoint transform is null in OverworldReferences!");
                    return Vector3.zero;
                }

                return OverworldReferences.Instance.OverWorldSpawnPoint.position;
            }
        }

        /// <summary>
        /// Gets the appropriate spawn point for the overworld based on context.
        /// Returns the building door spawn point if returning from a building,
        /// otherwise returns the default overworld spawn point from OverworldReferences.
        /// </summary>
        public Vector3 GetOverWorldSpawnPoint()
        {
            // If returning from a building, use the stored door spawn point
            if (!shouldUseDefaultSpawn && currentBuildingDoorSpawnPoint != Vector3.zero)
            {
                return currentBuildingDoorSpawnPoint;
            }

            // Otherwise, get the default spawn point from the overworld scene
            Vector3 defaultSpawn = OverWorldSpawnPoint;
            if (defaultSpawn == Vector3.zero)
            {
                Debug.LogError("[OverworldManager] Failed to get overworld spawn point - returning Vector3.zero!");
            }
            
            return defaultSpawn;
        }

        /// <summary>
        /// Called when player enters a building from the overworld.
        /// Stores the door's spawn point so player can return to it later.
        /// </summary>
        /// <param name="spawnPoint">The door's spawn point transform</param>
        public void EnteredBuilding(Transform spawnPoint)
        {
            if (spawnPoint != null)
            {
                currentBuildingDoorSpawnPoint = spawnPoint.position;
            }
            else
            {
                Debug.LogWarning("[OverworldManager] Attempted to enter building with null spawn point!");
            }
        }

        /// <summary>
        /// Called when player exits a building back to the overworld.
        /// Triggers scene transition and sets flag to use the stored door spawn point.
        /// </summary>
        public void ExitedBuilding()
        {
            shouldUseDefaultSpawn = false; // Use the stored building door spawn point
            SceneTransitionManager.Instance.LoadScene(SceneNames.OverworldScene, GameMode.ROGUE_LITE, true, OnReturnedToOverworld);
        }

        /// <summary>
        /// Called after returning to the overworld from a building.
        /// Resets spawn tracking to prepare for next building entry.
        /// </summary>
        private void OnReturnedToOverworld()
        {
            // Reset for next time - next overworld entry from camp should use default spawn
            shouldUseDefaultSpawn = true;
        }

        /// <summary>
        /// Resets all spawn point tracking. Called when entering overworld from camp.
        /// </summary>
        public void ResetSpawnTracking()
        {
            currentBuildingDoorSpawnPoint = Vector3.zero;
            shouldUseDefaultSpawn = true;
        }
    }
}
