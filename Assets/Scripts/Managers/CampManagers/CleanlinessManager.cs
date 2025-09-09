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
    
    [Header("Work-Based Dirt Generation")]
    [SerializeField] private float workDirtGenerationRate = 0.1f; // Dirt generated per second of work
    [SerializeField] private float dirtAccumulationThreshold = 5f; // Amount of dirt needed to spawn a pile
    private float accumulatedDirt = 0f;
    
    [Header("Productivity Impact")]
    [SerializeField] private float cleanProductivityBonus = 0.1f; // 10% bonus when very clean (90%+)
    [SerializeField] private float dirtyProductivityPenalty = 0.3f; // 30% penalty when dirty (30% or less)
    [SerializeField] private float veryDirtyProductivityPenalty = 0.6f; // 60% penalty when very dirty (10% or less)
    
    [Header("Health Impact")]
    [SerializeField] private float healthDrainRate = 2f; // Health lost per second when very dirty
    [SerializeField] private float sicknessProbability = 0.1f; // Chance per second of getting sick when filthy
    private float lastHealthDrainTime;
    private float lastSicknessCheckTime;

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
        public event System.Action<float> OnProductivityMultiplierChanged; // New event for productivity changes

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
            if(dirtPileSpawnCheckCoroutine != null){
                StopCoroutine(dirtPileSpawnCheckCoroutine);
                dirtPileSpawnCheckCoroutine = null;
            }

            if(nextGameMode == GameMode.CAMP){
                lastDirtPileSpawnTime = Time.time;
                lastHealthDrainTime = Time.time;
                lastSicknessCheckTime = Time.time;
                dirtPileSpawnCheckCoroutine = StartCoroutine(DirtPileSpawnCheckCoroutine());
                StartCoroutine(HealthEffectsCoroutine());
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

            // Calculate spawn chance based on NPC count (reduced since work now generates dirt)
            float npcCount = NPCManager.Instance.TotalNPCs;
            float baseSpawnChance = 0.3f; // Reduced from 1f since work generates most dirt now
            float spawnChance = baseSpawnChance + (npcCount * npcDirtinessMultiplier * 0.5f); // Reduced multiplier effect

            // Only proceed if random roll succeeds
            if (Random.value <= spawnChance)
            {
                // Try to find a waste bin with space
                WasteBin availableBin = FindAvailableWasteBin();
                
                if (availableBin != null)
                {
                    // Add waste to the bin instead of spawning a dirt pile
                    availableBin.AddWaste(dirtPileCleanlinessDecrease * 0.5f); // Reduced impact from passive generation
                }
                else
                {
                    // No available bins, spawn a dirt pile (but smaller impact)
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

            // Check if CampManager is available
            if (CampManager.Instance == null)
            {
                Debug.LogError("[CleanlinessManager] CampManager not found! Cannot spawn dirt pile with grid system.");
                return;
            }

            // Find an available grid position instead of random NavMesh position
            Vector3 gridPosition = GetAvailableGridPosition();
            
            if (gridPosition == Vector3.zero)
            {
                Debug.LogWarning("[CleanlinessManager] No available grid slots for dirt pile spawn!");
                return;
            }

            GameObject dirtPileObj = Instantiate(dirtPilePrefab, gridPosition, Quaternion.identity);
            DirtPileTask dirtPile = dirtPileObj.GetComponent<DirtPileTask>();
            
            if (dirtPile != null)
            {
                // Mark the grid slot as occupied (dirt piles take up 1x1 space)
                Vector2Int dirtPileSize = new Vector2Int(1, 1);
                CampManager.Instance.MarkSharedGridSlotsOccupied(gridPosition, dirtPileSize, dirtPileObj);
                
                activeDirtPiles.Add(dirtPile);
                DecreaseCleanliness(dirtPileCleanlinessDecrease);
                OnDirtPileSpawned?.Invoke(dirtPile);
            }
        }

        private Vector3 GetAvailableGridPosition()
        {
            // Check if CampManager is available
            if (CampManager.Instance == null)
            {
                Debug.LogError("[CleanlinessManager] CampManager not found! Cannot find available grid position.");
                return Vector3.zero;
            }

            // Try up to 50 times to find an available grid slot
            for (int i = 0; i < 50; i++)
            {
                // Get a random point within the camp grid bounds
                Vector3 randomPoint = new Vector3(
                    UnityEngine.Random.Range(CampManager.Instance.SharedXBounds.x, CampManager.Instance.SharedXBounds.y),
                    0f,
                    UnityEngine.Random.Range(CampManager.Instance.SharedZBounds.x, CampManager.Instance.SharedZBounds.y)
                );

                // Snap to grid
                Vector3 gridPosition = CampManager.Instance.SnapToSharedGrid(randomPoint);
                
                // Check if the grid slot is available (dirt piles take up 1x1 space)
                Vector2Int dirtPileSize = new Vector2Int(1, 1);
                if (CampManager.Instance.AreSharedGridSlotsAvailable(gridPosition, dirtPileSize))
                {
                    // Check if the position is far enough from other dirt piles
                    bool tooClose = false;
                    foreach (var dirtPile in activeDirtPiles)
                    {
                        if (Vector3.Distance(gridPosition, dirtPile.transform.position) < 5f)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        return gridPosition;
                    }
                }
            }

            Debug.LogWarning("[CleanlinessManager] Failed to find available grid position for dirt pile");
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
                // Free the grid slot before destroying the object
                if (CampManager.Instance != null)
                {
                    Vector2Int dirtPileSize = new Vector2Int(1, 1);
                    CampManager.Instance.MarkSharedGridSlotsUnoccupied(dirtPile.transform.position, dirtPileSize);
                }
                
                OnDirtPileCleaned?.Invoke(dirtPile);
                IncreaseCleanliness(10f); // Reward for cleaning
            }
        }

        private void DecreaseCleanliness(float amount)
        {
            float previousCleanliness = currentCleanliness;
            float previousMultiplier = GetProductivityMultiplier();
            
            currentCleanliness = Mathf.Max(0, currentCleanliness - amount);
            
            if (previousCleanliness != currentCleanliness)
            {
                OnCleanlinessChanged?.Invoke(GetCleanlinessPercentage());
                
                // Check if productivity multiplier changed
                float newMultiplier = GetProductivityMultiplier();
                if (Mathf.Abs(previousMultiplier - newMultiplier) > 0.01f)
                {
                    OnProductivityMultiplierChanged?.Invoke(newMultiplier);
                }
            }
        }

        public void IncreaseCleanliness(float amount)
        {
            float previousCleanliness = currentCleanliness;
            float previousMultiplier = GetProductivityMultiplier();
            
            currentCleanliness = Mathf.Min(maxCleanliness, currentCleanliness + amount);
            
            if (previousCleanliness != currentCleanliness)
            {
                OnCleanlinessChanged?.Invoke(GetCleanlinessPercentage());
                
                // Check if productivity multiplier changed
                float newMultiplier = GetProductivityMultiplier();
                if (Mathf.Abs(previousMultiplier - newMultiplier) > 0.01f)
                {
                    OnProductivityMultiplierChanged?.Invoke(newMultiplier);
                }
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
        
        /// <summary>
        /// Called when NPCs perform work to generate dirt based on activity
        /// </summary>
        /// <param name="workDelta">Amount of work performed this frame</param>
        public void GenerateDirtFromWork(float workDelta)
        {
            // Generate dirt based on work activity
            float dirtGenerated = workDelta * workDirtGenerationRate;
            accumulatedDirt += dirtGenerated;
            
            // Check if we've accumulated enough dirt to spawn a pile
            if (accumulatedDirt >= dirtAccumulationThreshold)
            {
                // Try to spawn dirt pile or add to waste bin
                if (activeDirtPiles.Count < maxDirtPiles)
                {
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
                    
                    // Reset accumulated dirt
                    accumulatedDirt = 0f;
                }
            }
        }
        
        /// <summary>
        /// Get the current productivity multiplier based on cleanliness level
        /// </summary>
        /// <returns>Multiplier to apply to work speed (0.4 to 1.1)</returns>
        public float GetProductivityMultiplier()
        {
            float cleanlinessPercentage = GetCleanlinessPercentage();
            
            if (cleanlinessPercentage >= 90f)
            {
                // Very clean: bonus productivity
                return 1f + cleanProductivityBonus;
            }
            else if (cleanlinessPercentage >= 70f)
            {
                // Clean: normal productivity
                return 1f;
            }
            else if (cleanlinessPercentage >= 30f)
            {
                // Somewhat dirty: slight penalty
                float penalty = Mathf.Lerp(0f, dirtyProductivityPenalty * 0.5f, (70f - cleanlinessPercentage) / 40f);
                return 1f - penalty;
            }
            else if (cleanlinessPercentage >= 10f)
            {
                // Dirty: significant penalty
                float penalty = Mathf.Lerp(dirtyProductivityPenalty * 0.5f, dirtyProductivityPenalty, (30f - cleanlinessPercentage) / 20f);
                return 1f - penalty;
            }
            else
            {
                // Very dirty: severe penalty
                return 1f - veryDirtyProductivityPenalty;
            }
        }
        
        /// <summary>
        /// Get a description of the current cleanliness impact on productivity
        /// </summary>
        public string GetProductivityImpactDescription()
        {
            float cleanlinessPercentage = GetCleanlinessPercentage();
            float multiplier = GetProductivityMultiplier();
            
            if (cleanlinessPercentage >= 90f)
            {
                return $"Pristine conditions boost productivity by {(multiplier - 1f) * 100f:F0}%";
            }
            else if (cleanlinessPercentage >= 70f)
            {
                return "Clean conditions maintain normal productivity";
            }
            else if (cleanlinessPercentage >= 30f)
            {
                return $"Messy conditions reduce productivity by {(1f - multiplier) * 100f:F0}%";
            }
            else if (cleanlinessPercentage >= 10f)
            {
                return $"Dirty conditions significantly reduce productivity by {(1f - multiplier) * 100f:F0}%";
            }
            else
            {
                return $"Filthy conditions severely hamper productivity by {(1f - multiplier) * 100f:F0}%";
            }
        }
        
        /// <summary>
        /// Coroutine to handle health effects from poor cleanliness
        /// </summary>
        private IEnumerator HealthEffectsCoroutine()
        {
            WaitForSeconds wait = new WaitForSeconds(1f); // Check every second
            
            while (true)
            {
                float cleanlinessPercentage = GetCleanlinessPercentage();
                
                // Apply health drain when very dirty
                if (cleanlinessPercentage <= 20f)
                {
                    ApplyHealthDrain();
                }
                
                // Check for sickness when filthy
                if (cleanlinessPercentage <= 10f)
                {
                    CheckForSickness();
                }
                
                yield return wait;
            }
        }
        
        /// <summary>
        /// Apply health drain to all NPCs when camp is very dirty
        /// </summary>
        private void ApplyHealthDrain()
        {
            if (NPCManager.Instance == null) return;
            
            float healthDrain = healthDrainRate * Time.deltaTime;
            
            foreach (var npc in NPCManager.Instance.GetAllNPCs())
            {
                if (npc is SettlerNPC settler && settler.Health > 0)
                {
                    settler.TakeDamage(healthDrain);
                    
                    // Log occasionally for feedback
                    if (Time.time - lastHealthDrainTime >= 10f)
                    {
                        Debug.LogWarning($"[CleanlinessManager] Filthy conditions are affecting {settler.name}'s health!");
                        lastHealthDrainTime = Time.time;
                    }
                }
            }
        }
        
        /// <summary>
        /// Check for random sickness events when camp is filthy
        /// </summary>
        private void CheckForSickness()
        {
            if (NPCManager.Instance == null) return;
            if (Time.time - lastSicknessCheckTime < 1f) return; // Only check once per second
            
            lastSicknessCheckTime = Time.time;
            
            foreach (var npc in NPCManager.Instance.GetAllNPCs())
            {
                if (npc is SettlerNPC settler && settler.Health > 0)
                {
                    // Random chance of getting sick
                    if (Random.value < sicknessProbability * Time.deltaTime)
                    {
                        // Apply sickness effect (reduce work speed temporarily)
                        StartCoroutine(ApplySicknessEffect(settler));
                        Debug.LogWarning($"[CleanlinessManager] {settler.name} has fallen ill due to filthy conditions!");
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply temporary sickness effect to an NPC
        /// </summary>
        private IEnumerator ApplySicknessEffect(SettlerNPC settler)
        {
            // Reduce work speed for 30 seconds
            float sicknessDuration = 30f;
            
            // Note: This is a simplified implementation. In a full system, you'd want
            // a proper status effect system to handle temporary modifiers
            
            yield return new WaitForSeconds(sicknessDuration);
            
            Debug.Log($"[CleanlinessManager] {settler.name} has recovered from illness.");
        }
    }
}