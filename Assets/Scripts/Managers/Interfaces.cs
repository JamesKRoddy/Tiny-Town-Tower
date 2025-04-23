using UnityEngine;

public interface IControllerInput
{
    public void SetPlayerControlType(PlayerControlType controlType);
}

public interface IPickupableItem
{
    void Initialize(ResourceScriptableObj data);
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
    void Attack();
    void Dash();
    WeaponScriptableObj GetEquipped();
    void EquipWeapon(WeaponScriptableObj weapon);
    Transform GetTransform();
}

public interface IDamageable
{
    [SerializeField] public float Health { get; set; } // Property for current health
    [SerializeField] public float MaxHealth { get; set; } // Property for max health

    // Character type for VFX and sound effects
    CharacterType CharacterType { get; }

    // Event that fires when damage is taken, providing the damage amount and remaining health
    event System.Action<float, float> OnDamageTaken;
    // Event that fires when healing occurs, providing the heal amount and new health
    event System.Action<float, float> OnHeal;
    // Event that fires when the entity dies
    event System.Action OnDeath;

    void TakeDamage(float amount, Transform damageSource = null); // Method to handle damage
    void Heal(float amount);       // Optional: Method to handle healing
    void Die();
    Allegiance GetAllegiance(); // Method to get the allegiance of the entity
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
    Overall popups feel a bit off, need to fix when they open and close
    
 * Next work:

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

 */

/*
    Next Large Things to Move Onto
    
    A camp inventory system to track resources
    A building health system
    A research system
    A cleanliness system for the camp

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
