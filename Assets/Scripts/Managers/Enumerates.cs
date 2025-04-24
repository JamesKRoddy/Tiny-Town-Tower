// =========================
// BUILDING ENUMS
// =========================

/// <summary>
/// Building type used in roguelike section to determine the type of building and floor to spawn
/// </summary>
public enum BuildingType
{
    NONE
}

[System.Serializable]
public enum BuildingCategory
{
    GENERAL,
    FOOD,
    ELECTRICITY,
    DECORATION,
    WEAPONS,
    CAMP_UPKEEP
}

// =========================
// WORK AND TASK ENUMS
// =========================

public enum WorkType
{
    NONE,
    FARMING,
    GATHER,
    MAKE_AMMO,
    GENERATE_ELECTRICITY,
    BUILD_STRUCTURE,
    RESEARCH,
    CLEANING,
    COOKING,
    UPGRADE_BUILDING,
    REPAIR_BUILDING,
    UPGRADE_RESOURCE
}

public enum TaskType
{
    NONE,
    WORK,
    WANDER,
    ATTACK,
    TEND_CROPS
}

// =========================
// PLAYER AND CONTROL ENUMS
// =========================

public enum PlayerControlType
{
    NONE, // Default
    MAIN_MENU,

    /// <summary>
    /// RogueLike
    /// </summary>

    COMBAT_NPC_MOVEMENT, // In combat during roguelike sections

    /// <summary>
    /// Camp
    /// </summary>

    CAMP_NPC_MOVEMENT, // Player's movement in the camp
    CAMP_CAMERA_MOVEMENT, //Default
    CAMP_WORK_ASSIGNMENT, // Assigning work to a settler
    BUILDING_PLACEMENT, // Placing a building from the build menu

    /// <summary>
    /// Menus
    /// </summary>

    IN_CONVERSATION, // Talking to an NPC
    IN_MENU, // In any menu

    /// <summary>
    /// Turret
    /// </summary>

    TURRET_CAMERA_MOVEMENT, //Default
    TURRET_PLACEMENT, // Placing a turret from the turret menu

    /// <summary>
    /// Genetic Mutation UI
    /// </summary>

    GENETIC_MUTATION_MOVEMENT //Default
}

// =========================
// GAME MODE AND ROOM SETUP ENUMS
// =========================

public enum GameMode
{
    NONE,
    MAIN_MENU,
    ROGUE_LITE,
    CAMP,
    TURRET
}

public enum EnemySetupState
{
    NONE,
    WAVE_START, //Player choses to open a door
    PRE_ENEMY_SPAWNING, //Period when the new room is spawned in, props, chests etc. are spawned in and nav mesh is baked
    ENEMIES_SPAWNED, //The room game play has started
    ALL_WAVES_CLEARED // enemies are all dea, player is free the move around and choose the next path
}

// =========================
// ROOM AND WALL ENUMS
// =========================

public enum RoomPosition
{
    FRONT,
    BACK,
    LEFT,
    RIGHT
}

public enum WallType
{
    ENABLED, // Model can be seen, collider enabled
    DISABLED, // Model and collider disabled
    HIDDEN // Model disabled, collider enabled
}

public enum DoorStatus
{
    LOCKED, //Door is locked unable to pass through
    ENTRANCE, //Door opens, the player can paass through when the level is cleared
    EXIT //Door the player spawns infront of, unable to pass through
}

// =========================
// ENEMY AND TARGET ENUMS
// =========================

public enum EnemyTargetType
{
    NONE,
    PLAYER,
    CLOSEST_NPC,
    TURRET_END
}

// =========================
// RESOURCE ENUMS
// =========================

[System.Serializable]
public enum ResourceCategory
{
    GENERAL,
    FOOD,
    ELECTRICITY,
    DECORATION,
    WEAPONS,
    BASIC_BUILDING_MATERIAL,
    AMMO,
    QUEST_ITEM
}

public enum ResourceRarity
{
    COMMON,
    RARE,
    EPIC,
    LEGENDARY
}

// =========================
// WEAPON ENUMS
// =========================

public enum MeleeAttackDirection
{
    HORIZONTAL_LEFT,
    HORIZONTAL_RIGHT,
    VERTICAL_DOWN,
    VERTICAL_UP
}

public enum WeaponElement
{
    NONE,
    BASIC,
    FIRE,
    ELECTRIC,
    BLEED,
    HOLY
}

public enum WeaponAnimationType
{
    NONE,
    ONE_HANDED,
    TWO_HANDED
}

public enum GeneticMutation
{
    NONE
}

// =========================
// TURRET ENUMS
// =========================

public enum TurretCategory
{
    NONE
}

// =========================
// ALLEGIANCE ENUM
// =========================

public enum Allegiance
{
    FRIENDLY,
    HOSTILE,
    NEUTRAL
}

// =========================
// CHARACTER TYPE ENUMS
// =========================

public enum CharacterType
{
    NONE,
    // Human Types
    HUMAN_MALE_1,
    HUMAN_MALE_2,
    HUMAN_FEMALE_1,
    HUMAN_FEMALE_2,
    // Zombie Types
    ZOMBIE_MELEE,
    ZOMBIE_SPITTER,
    ZOMBIE_TANK,
    // Machine Types
    MACHINE_DRONE,
    MACHINE_TURRET_BASE_TARGET,
    MACHINE_ROBOT,
    // Special Types
    BOSS_1,
    BOSS_2,
    BOSS_3
}

// =========================
// MISCELLANEOUS ENUMS
// =========================

