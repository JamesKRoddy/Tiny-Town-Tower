using UnityEngine;

/// <summary>
/// Friendly room where NPCs spawn and no combat occurs
/// </summary>
public class FriendlyRoom : RogueLiteRoom
{
    [Header("Friendly Room Settings")]
    [SerializeField] private Transform[] npcSpawnPoints;
    [SerializeField] private GameObject[] friendlyNPCPrefabs;
    [SerializeField] private bool autoSpawnNPCs = true;
    [SerializeField] private bool showNPCSpawnGizmos = true;
    [SerializeField, Range(0f, 100f)] private float npcSpawnChance = 75f; // 75% chance to spawn an NPC per spawn point
    
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
        
        // Spawn NPCs if auto spawn is enabled
        if (autoSpawnNPCs)
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
        if (friendlyNPCPrefabs == null || friendlyNPCPrefabs.Length == 0)
        {
            Debug.LogWarning($"[FriendlyRoom] No friendly NPC prefabs assigned to {gameObject.name}");
            return;
        }
        
        for (int i = 0; i < npcSpawnPoints.Length; i++)
        {
            Transform spawnPoint = npcSpawnPoints[i];
            if (spawnPoint == null) continue;
            
            // Random chance to spawn an NPC at this point
            if (Random.Range(0f, 100f) <= npcSpawnChance)
            {
                // Select a random NPC prefab
                GameObject npcPrefab = friendlyNPCPrefabs[Random.Range(0, friendlyNPCPrefabs.Length)];
                
                // Spawn the NPC
                GameObject spawnedNPC = Instantiate(npcPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
                spawnedNPCs[i] = spawnedNPC;
                
                Debug.Log($"[FriendlyRoom] Spawned NPC '{npcPrefab.name}' at spawn point {i + 1} in room '{gameObject.name}'");
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
    /// Set the NPC spawn chance (0-100%)
    /// </summary>
    public void SetNPCSpawnChance(float chance)
    {
        npcSpawnChance = Mathf.Clamp(chance, 0f, 100f);
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
                    float radius = Mathf.Lerp(0.5f, 1.5f, npcSpawnChance / 100f);
                    Gizmos.DrawSphere(spawnPoint.position + Vector3.up * 0.1f, radius);
                    
                    #if UNITY_EDITOR
                    UnityEditor.Handles.color = Color.white;
                    UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 2.5f, 
                        $"NPC Spawn {i + 1}\n{npcSpawnChance:F0}% chance");
                    #endif
                }
            }
        }
    }
    #endif
} 