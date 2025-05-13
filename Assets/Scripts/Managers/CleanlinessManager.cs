using UnityEngine;
using System.Collections.Generic;

namespace Managers
{
    public class CleanlinessManager : MonoBehaviour
    {
        [Header("Cleanliness Settings")]
        [SerializeField] private float maxCleanliness = 100f;
        [ReadOnly, SerializeField] private float currentCleanliness = 100f;
        [SerializeField] private float baseDirtinessRate = 0.1f;
        [SerializeField] private float npcDirtinessMultiplier = 0.05f;
        [SerializeField] private float fullToiletDirtinessMultiplier = 2f;
        [SerializeField] private float fullBinDirtinessMultiplier = 1.5f;

        [Header("Dirt Pile Settings")]
        [SerializeField] private GameObject dirtPilePrefab;
        [SerializeField] private float dirtPileSpawnThreshold = 30f;
        [SerializeField] private float dirtPileSpawnInterval = 60f;
        private float lastDirtPileSpawnTime;

        private Coroutine cleanlinessCoroutine;
        private List<DirtPile> activeDirtPiles = new List<DirtPile>();
        private List<Toilet> toilets = new List<Toilet>();
        private List<WasteBin> wasteBins = new List<WasteBin>();
        private List<CleaningStation> cleaningStations = new List<CleaningStation>();

        // Events
        public event System.Action<float> OnCleanlinessChanged;
        public event System.Action<DirtPile> OnDirtPileSpawned;
        public event System.Action<DirtPile> OnDirtPileCleaned;

        public void Initialize()
        {
            cleanlinessCoroutine = StartCoroutine(CleanlinessCoroutine());
            lastDirtPileSpawnTime = Time.time;
        }

        private System.Collections.IEnumerator CleanlinessCoroutine()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);

            while (true)
            {
                float previousCleanliness = currentCleanliness;
                float dirtinessRate = CalculateDirtinessRate();
                currentCleanliness = Mathf.Max(0, currentCleanliness - dirtinessRate * 0.1f);
                
                if (previousCleanliness != currentCleanliness)
                {
                    OnCleanlinessChanged?.Invoke(GetCleanlinessPercentage());
                }

                // Check for dirt pile spawning
                if (Time.time - lastDirtPileSpawnTime >= dirtPileSpawnInterval)
                {
                    CheckAndSpawnDirtPile();
                    lastDirtPileSpawnTime = Time.time;
                }

                yield return wait;
            }
        }

        private float CalculateDirtinessRate()
        {
            float rate = baseDirtinessRate;
            
            // Add NPC-based dirtiness
            int npcCount = NPCManager.Instance.TotalNPCs;
            rate += npcCount * npcDirtinessMultiplier;

            // Add toilet-based dirtiness
            foreach (var toilet in toilets)
            {
                if (toilet.IsFull())
                {
                    rate *= fullToiletDirtinessMultiplier;
                }
            }

            // Add bin-based dirtiness
            foreach (var bin in wasteBins)
            {
                if (bin.IsFull())
                {
                    rate *= fullBinDirtinessMultiplier;
                }
            }

            return rate;
        }

        private void CheckAndSpawnDirtPile()
        {
            if (GetCleanlinessPercentage() <= dirtPileSpawnThreshold)
            {
                SpawnDirtPile();
            }
        }

        private void SpawnDirtPile()
        {
            if (dirtPilePrefab == null) return;

            // Find a random position on the NavMesh
            Vector3 randomPosition = GetRandomNavMeshPosition();
            GameObject dirtPileObj = Instantiate(dirtPilePrefab, randomPosition, Quaternion.identity);
            DirtPile dirtPile = dirtPileObj.GetComponent<DirtPile>();
            
            if (dirtPile != null)
            {
                activeDirtPiles.Add(dirtPile);
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

        public void HandleDirtPileCleaned(DirtPile dirtPile)
        {
            if (activeDirtPiles.Remove(dirtPile))
            {
                OnDirtPileCleaned?.Invoke(dirtPile);
                IncreaseCleanliness(10f); // Reward for cleaning
            }
        }

        public void OnNPCCountChanged(int newCount)
        {
            // NPC count changed, dirtiness rate will be updated in the next coroutine iteration
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

        public List<DirtPile> GetActiveDirtPiles()
        {
            return new List<DirtPile>(activeDirtPiles);
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