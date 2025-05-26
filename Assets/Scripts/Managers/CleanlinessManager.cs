using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Managers
{
    public class CleanlinessManager : MonoBehaviour
    {
        [Header("Cleanliness Settings")]
        [SerializeField] private float maxCleanliness = 100f;
        [ReadOnly, SerializeField] private float currentCleanliness = 100f;
        [SerializeField] private float dirtPileSpawnInterval = 60f;
        [SerializeField] private float npcDirtinessMultiplier = 0.05f; // How much each NPC increases dirt pile spawn chance
        [SerializeField] private int maxDirtPiles = 10; // Maximum number of dirt piles allowed
        private float lastDirtPileSpawnTime;

        [Header("Dirt Pile Settings")]
        [SerializeField] private GameObject dirtPilePrefab;
        [SerializeField] private float dirtPileCleanlinessDecrease = 10f;
        private Coroutine dirtPileSpawnCheckCoroutine;

        private List<DirtPileTask> activeDirtPiles = new List<DirtPileTask>();
        private List<Toilet> toilets = new List<Toilet>();
        private List<WasteBin> wasteBins = new List<WasteBin>();
        private List<CleaningStation> cleaningStations = new List<CleaningStation>();

        // Events
        public event System.Action<float> OnCleanlinessChanged;
        public event System.Action<DirtPileTask> OnDirtPileSpawned;
        public event System.Action<DirtPileTask> OnDirtPileCleaned;
        public event System.Action<WasteBin> OnWasteBinFull;

        public void NotifyWasteBinFull(WasteBin bin)
        {
            OnWasteBinFull?.Invoke(bin);
        }

        public void Initialize()
        {           
            
            // Subscribe to NPC count changes
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.OnNPCCountChanged += HandleNPCCountChanged;
            }

            // Subscribe to scene transition events
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.OnSceneTransitionBegin += HandleSceneTransitionBegin;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from NPC count changes
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.OnNPCCountChanged -= HandleNPCCountChanged;
            }

            // Unsubscribe from scene transition events
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.OnSceneTransitionBegin -= HandleSceneTransitionBegin;
            }
        }

        private void HandleSceneTransitionBegin(GameMode nextGameMode)
        {
            dirtPileSpawnCheckCoroutine = null;

            if(nextGameMode == GameMode.CAMP){
                lastDirtPileSpawnTime = Time.time;
                dirtPileSpawnCheckCoroutine = StartCoroutine(DirtPileSpawnCheckCoroutine());
            }
        }

        private void HandleNPCCountChanged(int newCount)
        {
            // NPC count changed, this will affect the spawn chance in CheckAndSpawnDirtPile
            // No need to do anything here as the count is checked when spawning
        }

        private IEnumerator DirtPileSpawnCheckCoroutine()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);

            while (true)
            {
                if (Time.time - lastDirtPileSpawnTime >= dirtPileSpawnInterval)
                {
                    CheckAndSpawnDirtPile();
                    lastDirtPileSpawnTime = Time.time;
                }
                yield return wait;
            }
        }

        private void CheckAndSpawnDirtPile()
        {
            // Don't spawn if we've reached max dirt piles or cleanliness is zero
            if (activeDirtPiles.Count >= maxDirtPiles || currentCleanliness <= 0)
            {
                return;
            }

            // Calculate spawn chance based on NPC count
            float npcCount = NPCManager.Instance.TotalNPCs;
            float spawnChance = 1f + (npcCount * npcDirtinessMultiplier);

            // Only proceed if random roll succeeds
            if (Random.value <= spawnChance)
            {
                // Try to find a waste bin with space
                WasteBin availableBin = FindAvailableWasteBin();
                
                if (availableBin != null)
                {
                    // Add waste to the bin instead of spawning a dirt pile
                    availableBin.AddWaste(dirtPileCleanlinessDecrease);
                }
                else
                {
                    // No available bins, spawn a dirt pile
                    SpawnDirtPile();
                }
            }
        }

        private WasteBin FindAvailableWasteBin()
        {
            foreach (var bin in wasteBins)
            {
                if (!bin.IsFull())
                {
                    return bin;
                }
            }
            return null;
        }

        private void SpawnDirtPile()
        {
            if (dirtPilePrefab == null) return;

            // Find a random position on the NavMesh
            Vector3 randomPosition = GetRandomNavMeshPosition();
            GameObject dirtPileObj = Instantiate(dirtPilePrefab, randomPosition, Quaternion.identity);
            DirtPileTask dirtPile = dirtPileObj.GetComponent<DirtPileTask>();
            
            if (dirtPile != null)
            {
                activeDirtPiles.Add(dirtPile);
                DecreaseCleanliness(dirtPileCleanlinessDecrease);
                OnDirtPileSpawned?.Invoke(dirtPile);
            }
        }

        private Vector3 GetRandomNavMeshPosition()
        {
            // Try up to 30 times to find a valid position
            for (int i = 0; i < 30; i++)
            {
                // Get a random point within the camp bounds
                Vector3 randomPoint = new Vector3(
                    UnityEngine.Random.Range(-50f, 50f),
                    0f,
                    UnityEngine.Random.Range(-50f, 50f)
                );

                // Sample the NavMesh at this point
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    // Check if the position is far enough from other dirt piles
                    bool tooClose = false;
                    foreach (var dirtPile in activeDirtPiles)
                    {
                        if (Vector3.Distance(hit.position, dirtPile.transform.position) < 5f)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        return hit.position;
                    }
                }
            }

            Debug.LogWarning("[CleanlinessManager] Failed to find valid NavMesh position for dirt pile");
            return Vector3.zero;
        }

        public void RegisterToilet(Toilet toilet)
        {
            if (!toilets.Contains(toilet))
            {
                toilets.Add(toilet);
            }
        }

        public void UnregisterToilet(Toilet toilet)
        {
            toilets.Remove(toilet);
        }

        public void RegisterWasteBin(WasteBin bin)
        {
            if (!wasteBins.Contains(bin))
            {
                wasteBins.Add(bin);
                // Check if the bin is already full
                if (bin.IsFull())
                {
                    NotifyWasteBinFull(bin);
                }
            }
        }

        public void UnregisterWasteBin(WasteBin bin)
        {
            wasteBins.Remove(bin);
        }

        public void RegisterCleaningStation(CleaningStation station)
        {
            if (!cleaningStations.Contains(station))
            {
                cleaningStations.Add(station);
            }
        }

        public void UnregisterCleaningStation(CleaningStation station)
        {
            cleaningStations.Remove(station);
        }

        public void HandleDirtPileCleaned(DirtPileTask dirtPile)
        {
            if (activeDirtPiles.Remove(dirtPile))
            {
                OnDirtPileCleaned?.Invoke(dirtPile);
                IncreaseCleanliness(10f); // Reward for cleaning
            }
        }

        private void DecreaseCleanliness(float amount)
        {
            float previousCleanliness = currentCleanliness;
            currentCleanliness = Mathf.Max(0, currentCleanliness - amount);
            
            if (previousCleanliness != currentCleanliness)
            {
                OnCleanlinessChanged?.Invoke(GetCleanlinessPercentage());
            }
        }

        public void IncreaseCleanliness(float amount)
        {
            float previousCleanliness = currentCleanliness;
            currentCleanliness = Mathf.Min(maxCleanliness, currentCleanliness + amount);
            
            if (previousCleanliness != currentCleanliness)
            {
                OnCleanlinessChanged?.Invoke(GetCleanlinessPercentage());
            }
        }

        public float GetCleanliness()
        {
            return currentCleanliness;
        }

        public float GetCleanlinessPercentage()
        {
            return (currentCleanliness / maxCleanliness) * 100f;
        }

        public List<DirtPileTask> GetActiveDirtPiles()
        {
            return new List<DirtPileTask>(activeDirtPiles);
        }

        public List<Toilet> GetFullToilets()
        {
            return toilets.FindAll(t => t.IsFull());
        }

        public List<WasteBin> GetFullWasteBins()
        {
            return wasteBins.FindAll(b => b.IsFull());
        }
    }
}