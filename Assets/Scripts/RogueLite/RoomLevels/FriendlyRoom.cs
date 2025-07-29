using UnityEngine;
using Managers;

/// <summary>
/// Friendly room where NPCs spawn and no combat occurs
/// </summary>
public class FriendlyRoom : RogueLiteRoom
{
    [Header("Friendly Room Settings")]
    [SerializeField] private Transform[] npcSpawnPoints;
    [SerializeField] private bool showNPCSpawnGizmos = true;
    
    [Header("Room Ambiance")]
    [SerializeField] private AudioClip ambientSound;
    [SerializeField] private ParticleSystem[] ambientEffects;
    
    private GameObject[] spawnedNPCs;
    
    public override RogueLikeRoomType RoomType => RogueLikeRoomType.FRIENDLY;
    
    protected override void OnRoomAwake()
    {
        // Find NPC spawn points if not manually assigned
        if (npcSpawnPoints == null || npcSpawnPoints.Length == 0)
        {
            // Look for child objects with "NPCSpawn" in the name or a specific tag
            Transform[] allChildren = GetComponentsInChildren<Transform>();
            System.Collections.Generic.List<Transform> foundSpawnPoints = new System.Collections.Generic.List<Transform>();
            
            foreach (Transform child in allChildren)
            {
                if (child != transform && (child.name.ToLower().Contains("npcspawn") || child.CompareTag("NPCSpawn")))
                {
                    foundSpawnPoints.Add(child);
                }
            }
            
            npcSpawnPoints = foundSpawnPoints.ToArray();
        }
        
        if (npcSpawnPoints.Length == 0)
        {
            Debug.LogWarning($"[FriendlyRoom] No NPC spawn points found in friendly room: {gameObject.name}. Consider adding child objects with 'NPCSpawn' in the name.");
        }
        
        // Initialize spawned NPCs array
        spawnedNPCs = new GameObject[npcSpawnPoints.Length];
    }
    
    protected override void OnRoomSetup()
    {
        // Start ambient effects
        StartAmbientEffects();
        
        // Spawn NPCs if auto spawn is enabled (from building data)
        if (GetAutoSpawnNPCs())
        {
            SpawnNPCs();
        }
        
        Debug.Log($"[FriendlyRoom] Friendly room '{gameObject.name}' setup complete with {npcSpawnPoints.Length} NPC spawn points");
    }
    
    /// <summary>
    /// Spawn friendly NPCs at the designated spawn points
    /// </summary>
    public void SpawnNPCs()
    {
        var settlerNPCs = GetBuildingNPCs();
        if (settlerNPCs == null || settlerNPCs.Length == 0)
        {
            Debug.LogWarning($"[FriendlyRoom] No settler NPCs available for building in {gameObject.name}");
            return;
        }
        
        for (int i = 0; i < npcSpawnPoints.Length; i++)
        {
            Transform spawnPoint = npcSpawnPoints[i];
            if (spawnPoint == null) continue;
            
            // Random chance to spawn an NPC at this point
            if (Random.Range(0f, 100f) <= GetNPCSpawnChance())
            {
                // Select a random settler NPC
                NPCScriptableObj settlerNPC = settlerNPCs[Random.Range(0, settlerNPCs.Length)];
                
                // Get the prefab from the settler NPC and spawn it
                GameObject npcPrefab = settlerNPC.prefab;
                if (npcPrefab != null)
                {
                    GameObject spawnedNPC = Instantiate(npcPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
                    spawnedNPCs[i] = spawnedNPC;
                    
                    // Set the initialization context for fresh spawns in roguelike rooms
                    if (spawnedNPC.TryGetComponent<SettlerNPC>(out var settlerNPCComponent))
                    {
                        settlerNPCComponent.SetInitializationContext(NPCInitializationContext.FRESH_SPAWN);
                    }
                    
                    Debug.Log($"[FriendlyRoom] Spawned NPC '{settlerNPC.nPCName}' at spawn point {i + 1} in room '{gameObject.name}'");
                }
                else
                {
                    Debug.LogWarning($"[FriendlyRoom] Settler NPC '{settlerNPC.nPCName}' has no prefab assigned!");
                }
            }
        }
    }
    
    /// <summary>
    /// Remove all spawned NPCs from this room
    /// </summary>
    public void ClearNPCs()
    {
        for (int i = 0; i < spawnedNPCs.Length; i++)
        {
            if (spawnedNPCs[i] != null)
            {
                Destroy(spawnedNPCs[i]);
                spawnedNPCs[i] = null;
            }
        }
    }
    
    /// <summary>
    /// Start ambient effects for the friendly room
    /// </summary>
    private void StartAmbientEffects()
    {
        // Start particle effects
        if (ambientEffects != null)
        {
            foreach (var effect in ambientEffects)
            {
                if (effect != null && !effect.isPlaying)
                {
                    effect.Play();
                }
            }
        }
        
        // Play ambient sound (you might want to integrate this with your audio manager)
        if (ambientSound != null)
        {
            // AudioSource component or audio manager integration would go here
            Debug.Log($"[FriendlyRoom] Starting ambient sound for room '{gameObject.name}'");
        }
    }
    
    /// <summary>
    /// Get all NPC spawn points in this friendly room
    /// </summary>
    public Transform[] GetNPCSpawnPoints()
    {
        return npcSpawnPoints ?? new Transform[0];
    }
    
    /// <summary>
    /// Get all currently spawned NPCs
    /// </summary>
    public GameObject[] GetSpawnedNPCs()
    {
        return spawnedNPCs ?? new GameObject[0];
    }
    
    /// <summary>
    /// Check if this room has any valid NPC spawn points
    /// </summary>
    public bool HasNPCSpawnPoints()
    {
        return npcSpawnPoints != null && npcSpawnPoints.Length > 0;
    }
    
    /// <summary>
    /// Set the NPC spawn chance (0-100%) in the building data
    /// </summary>
    public void SetNPCSpawnChance(float chance)
    {
        if (RogueLiteManager.Instance != null && 
            RogueLiteManager.Instance.BuildingManager != null && 
            RogueLiteManager.Instance.BuildingManager.CurrentBuilding != null)
        {
            RogueLiteManager.Instance.BuildingManager.CurrentBuilding.SetNPCSpawnChance(chance);
        }
    }
    
    /// <summary>
    /// Get the available settler NPCs for this room from the building data
    /// </summary>
    public NPCScriptableObj[] GetBuildingNPCs()
    {
        // Get settler NPCs from the current building data
        if (RogueLiteManager.Instance != null && 
            RogueLiteManager.Instance.BuildingManager != null && 
            RogueLiteManager.Instance.BuildingManager.CurrentBuilding != null)
        {
            return RogueLiteManager.Instance.BuildingManager.CurrentBuilding.GetBuildingNPCs();
        }
        
        Debug.LogWarning($"[FriendlyRoom] Cannot access building data to get settler NPCs for {gameObject.name}");
        return new NPCScriptableObj[0];
    }

    /// <summary>
    /// Get whether NPCs should auto-spawn from building data
    /// </summary>
    private bool GetAutoSpawnNPCs()
    {
        if (RogueLiteManager.Instance != null && 
            RogueLiteManager.Instance.BuildingManager != null && 
            RogueLiteManager.Instance.BuildingManager.CurrentBuilding != null)
        {
            return RogueLiteManager.Instance.BuildingManager.CurrentBuilding.GetAutoSpawnNPCs();
        }
        
        return true; // Default fallback
    }

    /// <summary>
    /// Get the NPC spawn chance from building data
    /// </summary>
    private float GetNPCSpawnChance()
    {
        if (RogueLiteManager.Instance != null && 
            RogueLiteManager.Instance.BuildingManager != null && 
            RogueLiteManager.Instance.BuildingManager.CurrentBuilding != null)
        {
            return RogueLiteManager.Instance.BuildingManager.CurrentBuilding.GetNPCSpawnChance();
        }
        
        return 75f; // Default fallback
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showNPCSpawnGizmos) return;
        
        // Draw NPC spawn points
        if (npcSpawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var spawnPoint in npcSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                    Gizmos.DrawRay(spawnPoint.position, Vector3.up * 2f);
                    
                    // Draw a small person icon
                    Gizmos.DrawWireCube(spawnPoint.position + Vector3.up * 1f, new Vector3(0.3f, 0.6f, 0.1f));
                }
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showNPCSpawnGizmos) return;
        
        // Draw more detailed spawn point info when selected
        if (npcSpawnPoints != null)
        {
            for (int i = 0; i < npcSpawnPoints.Length; i++)
            {
                var spawnPoint = npcSpawnPoints[i];
                if (spawnPoint != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(spawnPoint.position, 0.3f);
                    
                    // Draw spawn chance indicator
                    Gizmos.color = new Color(0, 1, 0, 0.3f);
                    float spawnChance = GetNPCSpawnChance();
                    float radius = Mathf.Lerp(0.5f, 1.5f, spawnChance / 100f);
                    Gizmos.DrawSphere(spawnPoint.position + Vector3.up * 0.1f, radius);
                    
                    #if UNITY_EDITOR
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 2.5f, 
                        $"NPC Spawn {i + 1}\n{spawnChance:F0}% chance");
                    #endif
                }
            }
        }
    }
    #endif
} 