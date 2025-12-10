using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BossScriptableObj defines a boss encounter for roguelite buildings.
/// Contains the boss prefab, the room/arena it spawns in, and configuration data.
/// This allows each boss to have a unique room tailored to their fight mechanics.
/// 
/// LOOT SYSTEM:
/// Bosses use the same loot system as regular enemies:
/// - bossLootTable: References a LootTableScriptableObj with random loot based on rarity
/// - guaranteedLoot: List of resources that ALWAYS drop when boss is defeated
/// - lootDropMultiplier: Scales the amount/chance of loot (default 2x for bosses)
/// 
/// The boss enemy prefab should have the standard EnemyBase.Die() method which calls
/// ResourceManager.SpawnCharacterLoot() - this will use the boss's CharacterType
/// (e.g., BOSS_1, BOSS_2, BOSS_3) to find the correct loot table.
/// 
/// SETUP:
/// 1. Create a RoomParentDataScriptableObj for your boss arena
/// 2. Create a LootTableScriptableObj for your boss
/// 3. In ResourceManager (GameManager prefab), add a LootTableForCharacterType entry:
///    - CharacterType: BOSS_1 (or BOSS_2, BOSS_3)
///    - LootTable: Your boss loot table
/// 4. Assign the loot table to this BossScriptableObj's bossLootTable field
/// 5. (Optional) Add guaranteed loot items that always drop
/// 
/// USAGE EXAMPLE:
/// In your building generation code:
/// 
/// // Check if we should spawn a boss at the end
/// if (buildingData.ShouldSpawnBoss())
/// {
///     BossScriptableObj boss = buildingData.GetBossForDifficulty(currentDifficulty);
///     if (boss != null && boss.bossRoomData != null)
///     {
///         // Get the boss room parent prefab from the boss room data
///         GameObject bossRoomParentPrefab = boss.bossRoomData.bossRoomParentPrefab;
///         
///         // Instantiate the boss room parent (complete building section)
///         GameObject roomParentInstance = Instantiate(bossRoomParentPrefab, position, rotation);
///         BossRoomParent bossRoomParent = roomParentInstance.GetComponent<BossRoomParent>();
///         
///         if (bossRoomParent != null)
///         {
///             // Assign the boss and generate the room layout using boss room data
///             bossRoomParent.SetBoss(boss);
///             bossRoomParent.GenerateBossArena(boss.bossRoomData, currentDifficulty);
///             // Boss spawns automatically after room generation
///         }
///     }
/// }
/// </summary>
[CreateAssetMenu(fileName = "BossScriptableObj", menuName = "Scriptable Objects/Roguelite/Enemies/BossScriptableObj")]
public class BossScriptableObj : ScriptableObject
{
    [Header("Boss Information")]
    [Tooltip("Name of the boss for identification and UI display")]
    public string bossName;
    
    [Tooltip("Description of the boss and its abilities")]
    [TextArea(3, 6)]
    public string bossDescription;
    
    [Header("Boss Assets")]
    [Tooltip("The boss enemy prefab to spawn (should have EnemyBase component)")]
    public GameObject bossPrefab;
    
    [Tooltip("Boss arena room configuration - contains the BossRoomParent prefab and room layout configuration")]
    public RoomParentDataScriptableObj bossRoomData;
    
    [Header("Boss Configuration")]
    [Tooltip("Minimum difficulty level required to spawn this boss")]
    [SerializeField, Range(0, 10)] private int minimumDifficulty = 0;
    
    [Tooltip("Relative spawn weight - higher values = more likely to be selected")]
    [SerializeField, Range(1, 100)] private int spawnWeight = 10;
    
    [Tooltip("If true, this boss won't spawn during testing/development")]
    [SerializeField] private bool excludeFromTesting = false;
    
    [Header("Rewards")]
    [Tooltip("Loot table that defines what resources this boss drops when defeated. Uses the same loot system as regular enemies.")]
    [SerializeField] private LootTableScriptableObj bossLootTable;
    
    [Tooltip("Guaranteed loot drops - these resources will always drop when the boss is defeated")]
    [SerializeField] private List<ResourceItemCount> guaranteedLoot = new List<ResourceItemCount>();
    
    [Tooltip("Drop chance multiplier for this boss (1.0 = normal, 2.0 = double drops, etc.)")]
    [SerializeField, Range(1f, 5f)] private float lootDropMultiplier = 2f; // Bosses drop more loot by default
    
    /// <summary>
    /// Check if this boss can spawn at the given difficulty level
    /// </summary>
    public bool CanSpawnAtDifficulty(int difficulty)
    {
        return difficulty >= minimumDifficulty && !excludeFromTesting;
    }
    
    /// <summary>
    /// Get the spawn weight for selection probability
    /// </summary>
    public int GetSpawnWeight()
    {
        return spawnWeight;
    }
    
    /// <summary>
    /// Get the minimum difficulty for this boss
    /// </summary>
    public int GetMinimumDifficulty()
    {
        return minimumDifficulty;
    }
    
    /// <summary>
    /// Check if this boss is excluded from testing
    /// </summary>
    public bool IsExcludedFromTesting()
    {
        return excludeFromTesting;
    }
    
    /// <summary>
    /// Get the boss's loot table for spawning rewards
    /// </summary>
    public LootTableScriptableObj GetBossLootTable()
    {
        return bossLootTable;
    }
    
    /// <summary>
    /// Get the guaranteed loot that always drops from this boss
    /// </summary>
    public List<ResourceItemCount> GetGuaranteedLoot()
    {
        return guaranteedLoot ?? new List<ResourceItemCount>();
    }
    
    /// <summary>
    /// Get the loot drop multiplier for this boss
    /// </summary>
    public float GetLootDropMultiplier()
    {
        return lootDropMultiplier;
    }
}

