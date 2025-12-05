using UnityEngine;

/// <summary>
/// Information about a hit on an IHittable object
/// Used for projectile reflection, environmental interactions, etc.
/// </summary>
public struct HitInfo
{
    public Vector3 HitPoint;      // Where the hit occurred
    public Vector3 HitNormal;     // Normal at the hit point
    public Transform Attacker;     // Who/what caused the hit
    public float Force;           // Force of the hit (for physics reactions)
    public IDamageDealer Dealer;  // The damage dealer (if applicable)
    
    public HitInfo(Vector3 hitPoint, Vector3 hitNormal, Transform attacker, float force = 0f, IDamageDealer dealer = null)
    {
        HitPoint = hitPoint;
        HitNormal = hitNormal;
        Attacker = attacker;
        Force = force;
        Dealer = dealer;
    }
}

/// <summary>
/// Interface for any object that can be hit and respond to hits
/// Examples: projectiles (reflection), barrels (break), bells (ring), interactive props
/// </summary>
public interface IHittable
{
    /// <summary>
    /// Called when this object is hit by a weapon or other source
    /// </summary>
    /// <param name="hitInfo">Information about the hit</param>
    void OnHit(HitInfo hitInfo);
    
    /// <summary>
    /// Check if this object can currently be hit
    /// </summary>
    /// <returns>True if the object can be hit right now</returns>
    bool CanBeHit();
}

public interface IControllerInput
{
    public void SetPlayerControlType(PlayerControlType controlType);
}

public interface IPickupableItem
{
    void Initialize(ResourceScriptableObj data, int count = 1);
    string GetItemName();
    string GetItemDescription();
    Sprite GetItemImage();
}

public interface IInteractiveBase
{
    bool CanInteract();
    string GetInteractionText();
    object Interact();
}

public interface IInteractive<out T> : IInteractiveBase
{
    new T Interact(); // Hides the base Interact method with a strongly-typed version
}

public interface IPossessable
{
    void OnPossess();
    void OnUnpossess();
    void PossessedUpdate();
    void Movement(Vector3 movement);
    /// <summary>
    /// Initiate an attack (called by player input)
    /// This starts the attack animation, actual damage is executed via IAttackOwner.Attack() animation event
    /// </summary>
    void InitiateAttack();
    void Dash();
    WeaponScriptableObj GetEquipped();
    void EquipWeapon(WeaponScriptableObj weapon);
    Transform GetTransform();
}

public interface IDamageable
{
    [SerializeField] public float Health { get; set; } // Property for current health
    [SerializeField] public float MaxHealth { get; set; } // Property for max health
    [SerializeField] public float Poise { get; set; } // Property for current poise
    [SerializeField] public float MaxPoise { get; set; } // Property for max poise

    // Character type for VFX and sound effects
    CharacterType CharacterType { get; }

    // Event that fires when damage is taken, providing the damage amount and remaining health
    event System.Action<float, float> OnDamageTaken;
    // Event that fires when healing occurs, providing the heal amount and new health
    event System.Action<float, float> OnHeal;
    // Event that fires when poise is broken, providing the poise damage amount and remaining poise
    event System.Action<float, float> OnPoiseBroken;
    // Event that fires when the entity dies
    event System.Action OnDeath;

    // Hit reaction tracking (for procedural IK reactions)
    Vector3 LastHitOrigin { get; set; } // World position where last damage came from
    float LastHitTime { get; set; } // Time when last damage was taken
    float LastHitPoiseDamage { get; set; } // Poise damage from last hit (used to scale reaction intensity)

    /// <summary>
    /// Unified method to handle all types of damage using DamageInfo struct
    /// </summary>
    /// <param name="damageInfo">Complete damage information including source, type, and flags</param>
    void TakeDamage(DamageInfo damageInfo);
    void Heal(float amount);       // Optional: Method to handle healing
    void Die();
    Allegiance GetAllegiance(); // Method to get the allegiance of the entity
    
    // Elemental resistance system
    DamageResistance GetResistance(AttackElement damageType); // Get resistance level for a specific damage type
    float GetDamageMultiplier(AttackElement damageType); // Get damage multiplier for a specific damage type
    
    /// <summary>
    /// Gets the target transform for VFX spawning and projectile targeting.
    /// This is typically the mesh/model transform where visual effects should spawn.
    /// For enemies with separate mesh objects (like drones), this returns the mesh transform.
    /// Falls back to the main transform if no specific target is set.
    /// </summary>
    /// <returns>The transform to use for VFX and projectile targeting</returns>
    Transform GetTargetTransform();
}

/// <summary>
/// Interface for buildings and structures that can be damaged and have building-specific effects
/// </summary>
public interface IBuildingDamageable : IDamageable
{
    // Additional building-specific damage handling if needed
}

/// <summary>
/// Interface for objects that can be saved and loaded
/// </summary>
public interface ISaveable
{
    string GetSaveId();
    object GetSaveData();
    void LoadFromSaveData(object saveData);
}

/// <summary>
/// Interface for managers that need to save/load their state
/// </summary>
public interface ISaveableManager
{
    object GetSaveData();
    void LoadFromSaveData(object saveData);
    void Initialize(); // For initialization after loading
}

/// <summary>
/// Interface for placeable structures to provide common functionality
/// </summary>
public interface IPlaceableStructure
{
    float GetCurrentHealth();
    float GetMaxHealth();
    void Heal(float amount);
    PlaceableObjectParent GetStructureScriptableObj();
    bool IsOperational();
    bool IsUnderConstruction();
    void SetCurrentWorkTask(WorkTask workTask);
    void TriggerUpgradeEvent();
    void StartDestruction();
    
    // Save/Load related methods
    string GetSaveId();
    void RestoreFromSaveData(object saveData);
}

/// <summary>
/// Interface for NPCs that can engage in conversations
/// Provides methods to pause and resume AI behavior during dialogue
/// </summary>
public interface INarrativeTarget
{
    /// <summary>
    /// Pauses the NPC's AI and movement during conversations
    /// </summary>
    void PauseForConversation();
    
    /// <summary>
    /// Resumes the NPC's AI and movement after conversations
    /// </summary>
    void ResumeAfterConversation();
    
    /// <summary>
    /// Gets the Transform of this conversation target for positioning and interaction detection
    /// </summary>
    Transform GetTransform();
}

/// <summary>
/// Interface for objects that can receive status effects
/// Objects implementing this interface own their status effect data (gameplay)
/// EffectManager handles VFX/presentation layer
/// </summary>
public interface IStatusEffectTarget
{
    /// <summary>
    /// Add a status effect to this target (gameplay data)
    /// Objects are the source of truth for their active effects
    /// </summary>
    /// <param name="effectType">The type of status effect to add</param>
    /// <returns>True if added, false if already present</returns>
    bool AddStatusEffect(StatusEffectType effectType);
    
    /// <summary>
    /// Remove a status effect from this target (gameplay data)
    /// </summary>
    /// <param name="effectType">The type of status effect to remove</param>
    /// <returns>True if removed, false if not present</returns>
    bool RemoveStatusEffect(StatusEffectType effectType);
    
    /// <summary>
    /// Check if this target has a specific status effect
    /// </summary>
    /// <param name="effectType">The type of status effect to check</param>
    /// <returns>True if the effect is active</returns>
    bool HasStatusEffect(StatusEffectType effectType);
    
    /// <summary>
    /// Get all active status effects on this target
    /// </summary>
    /// <returns>Collection of active status effect types</returns>
    System.Collections.Generic.IReadOnlyCollection<StatusEffectType> GetActiveStatusEffects();
    
    /// <summary>
    /// Called when a status effect is applied (after AddStatusEffect)
    /// Used for custom gameplay logic (movement penalties, behavior changes, etc.)
    /// </summary>
    /// <param name="effectType">The type of status effect being applied</param>
    /// <param name="duration">Duration of the effect in seconds (0 = permanent until removed)</param>
    void OnStatusEffectApplied(StatusEffectType effectType, float duration);
    
    /// <summary>
    /// Called when a status effect is removed (after RemoveStatusEffect)
    /// Used for custom gameplay logic cleanup
    /// </summary>
    /// <param name="effectType">The type of status effect being removed</param>
    void OnStatusEffectRemoved(StatusEffectType effectType);
    
    /// <summary>
    /// Gets the character type for status effect configuration lookups
    /// </summary>
    /// <returns>The CharacterType for this target</returns>
    CharacterType GetCharacterType();
}


/*
 * Known bugs:

    Navmesh issues:
    RuntimeNavMeshBuilder: Source mesh SM_Wep_Bat_01 does not allow read access. This will work in playmode in the editor but not in player
    UnityEngine.AI.NavMeshBuilder:BuildNavMeshData (UnityEngine.AI.NavMeshBuildSettings,System.Collections.Generic.List`1<UnityEngine.AI.NavMeshBuildSource>,UnityEngine.Bounds,UnityEngine.Vector3,UnityEngine.Quaternion)
    Unity.AI.Navigation.NavMeshSurface:BuildNavMesh () (at ./Library/PackageCache/com.unity.ai.navigation@9f76b145f0a8/Runtime/NavMeshSurface.cs:272)
    RoomSectionRandomizer/<DelayedBakeNavMesh>d__13:MoveNext () (at Assets/Scripts/RogueLite/RoomLevels/RoomSectionRandomizer.cs:135)
    UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

    Can open and place mutations in the genetic mutation menu, while no npcs are possessed
    NPCs still sliding around when they are assigned a task

    Construction site 2x2 is not working, causing errors of grid spaces not being found
    
 * Next work:

    ******Combat and Boss******

    Setup different hitboxes on the weapons for horizontal and vertical attacks
    
    Weapon impacts, for basic weapons its just dirt
    For fire/electric should check if its hit a viable mesh and should spread to the mesh using it as an emission point?

    Try to setup a boss, large zombie w/ cyberpunk aspects to shoot lazers, projectiles, explosions etc.

    Boss UI, health bar etc.
    Effects for player hitting boss
    Boss death
    
    Can just use a mech for a boss
    Easy way to make the boss harder is to just speed up its animator

    Can do a runningtowards the player attack, nav agent and animation should work for this.

    ******Camp Management******

    Have to setup a menu for task details, they should all use the same preview menu just with different entries. tasks that need this are:
    ResearchTask, use the research manager to track whats been researched and what can be researched, scriptable obj for research items
    Later:
    FarmingTask

    ******UI******

    See if preview base prefabs (buttons and screens) can be more generic and share the same prefab

*/

// ========================================
// Next Large Things to Move Onto
// ========================================

// 1. Genetic Mutations & Inventory System
// - Introduce contamination mechanics, requiring purification before re-entering the camp.
/*
 *maybe the reason you have to go into the menu each time is because certain parts of the grid give you boosts like 2x the effect?
 Mutation Ideas
 Increase Dash Distance
 Increase Dash Speed
 Increase dash cooldown
 Increase Movement Speed
 Spawn a little turret or something that shoots at the zombies
 
 */

// 2. NPC Characteristics System
// - Implement a pool of positive and negative traits for NPCs to make them unique.
// - Examples of positive traits: Hard Worker, Sharpshooter, Medic.
// - Examples of negative traits: Lazy, Gluttonous, Psychotic.
// - Traits should affect job performance, combat effectiveness, and social interactions in camp.

// 3. NPC Recruitment from the Overworld
// - Allow players to recruit NPCs they find while exploring the overworld.
// - NPCs should require persuasion, trade, or special conditions to join the camp.
// - Some NPCs may refuse to join if camp morale, food supply, or defenses are low.
// - Introduce rare, highly skilled NPCs with unique perks or hidden agendas.

// 4. Expanded NPC Tasks & Camp Automation
// - Add more roles for NPCs in the camp, allowing for task automation.
// - New tasks: Building construction, farming, mining, researching advanced technology, sanitation.
// - Research should allow basic resources to be converted into more complex ones.
// - Implement a task assignment system where players can directly control workforce distribution.

// 5. Overworld Expansion
// - Expand the world beyond the camp, introducing new biomes and areas to explore.
// - Include locations like urban ruins, suburbs, industrial zones, and military bunkers.
// - Add dynamic encounters with rival factions, neutral NPCs, and random events.
// - Introduce environmental hazards such as rain, fog, and heatwaves affecting exploration.

// 6. Survivor Infection System
// - Implement an **infection level** for survivors, where if it reaches 100%, they turn into zombies.
// - Infected survivors will attack and spread the infection to others if not treated in time.
// - Introduce drugs and medicine found during the day that can:
//   - **Reduce infection** (lowers infection percentage).
//   - **Slow infection** (delays progression).
//   - **Cure infection** (fully removes it).

// 7. Roguelike Difficulty Scaling System
// - Implement a difficulty system where progression is controlled by buildings and floors.
// - The deeper into the overworld/dungeons the player goes, the more dangerous enemies become.
// - Buildings and upgrades in the camp should impact enemy scaling (e.g., better defenses attract stronger enemies).

// 8. More Buildings, Weapons, and NPCs
// - Expand the variety of buildings available for construction.
// - New buildings: Barracks (increases survivor capacity), Workshop (crafting & repairs), Medical Wing (faster healing).
// - Introduce more weapons such as makeshift flamethrowers, crossbows, and EMP grenades.
// - Add more NPC variety, including special characters with questlines and unique abilities.

// 9. Merchant & Trading System
// - Set up a dedicated merchant screen for trading resources, weapons, and items.
// - Merchants should rotate stock, and some rare merchants should only appear in specific locations.
// - Allow players to barter or negotiate prices based on their charisma/survivor traits.

// 10. Building Interiors & Environmental Hazards
// - Expand existing buildings with more interactive interior spaces.
// - Add multi-room layouts with destructible doors, hidden areas, and secret stashes.
// - Introduce environmental hazards such as:
//   - Fire: Spreads and damages structures, requiring survivors to extinguish.
//   - Flooding: Blocks pathways and limits movement, requiring alternate routes.
//   - Electrical Hazards: Exposed wires that can be deadly or used as traps.
// - Implement physics-based destruction, allowing walls and barricades to break dynamically.

// 11. Building & Turret Placement System
// - Implement a **system to move buildings and turrets** after they have been placed.
// - This should allow the player to reposition defenses without needing to destroy and rebuild them.
// - Consider adding a cost or penalty for moving structures to balance gameplay.
// - Upgrade system for turrets and buildings

// 12. PlayerSwitchMenu Enhancement
// - Modify the **PlayerSwitchMenu** to use a render texture that follows the currently selected player.
// - This will provide a more immersive and visually appealing transition between characters.

// 13. Possession Screen UI Rework
// - Change the **possession screen** to include a selection box instead of direct control switching.
// - The selection box will allow interactions such as:
//   - **Talking to NPCs** (for quests, dialogue, and story progression).
//   - **Assigning work** (choosing specific tasks for NPCs).
//   - **Possessing survivors** (directly controlling them for specific tasks or combat situations).

// 14. Zombie Animations
// - Setup zombie idle animations for when they are not actively chasing the player.
// - Implement zombie death animations, ensuring smooth transitions when killed.
// - Consider animation variations for different types of zombies (slow walkers, runners, bosses).
