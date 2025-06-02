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
    CLEANING,
    RESOURCES
}

// =========================
// WORK AND TASK ENUMS
// =========================

public enum TaskType
{
    NONE,
    WORK,
    WANDER,
    ATTACK,
    EAT
}

[System.Serializable]
public enum TaskAnimation
{
    NONE,
    COOKING_POT_STIR,
    RESEARCH_COUNTER,
    HAMMER_STANDING,
    HAMMER_COUNTER,
    GENERATE_ELECTRICITY_HAND,
    //Farming animations
    PLANTING_SEEDS,
    WATERING_PLANTS,
    HARVEST_PLANT_STANDING,
    HARVEST_PLANT_KNEELING,
    CLEARING_PLOT
}

// =========================
// IK POINT ENUMS
// =========================

[System.Serializable]
public enum IKPoint
{
    NONE,
    LEFT_HAND,
    RIGHT_HAND,
    LEFT_FOOT,
    RIGHT_FOOT,
    HEAD,
    WEAPON
}

// =========================
// RESEARCH ENUMS
// =========================

[System.Serializable]
public enum ResearchUnlockType //TODO: Use enum for research unlock types for menus
{
    NONE,
    BUILDING,    // Unlocks new building types
    RESOURCE,    // Unlocks new resources
    TECHNOLOGY,  // Unlocks new technologies
    WEAPON,      // Unlocks new weapons
    TURRET,      // Unlocks new turrets
    UPGRADE      // Unlocks upgrades for existing items
}

// =========================
// PLAYER AND CONTROL ENUMS
// =========================

public enum PlayerControlType
{
    NONE = 0, // Default
    MAIN_MENU = 100,
    TRANSITION = 101, // Used for transition between scenes, disables all player input

    /// <summary>
    /// RogueLike
    /// </summary>

    COMBAT_NPC_MOVEMENT = 200, // In combat during roguelike sections

    /// <summary>
    /// Camp
    /// </summary>

    CAMP_NPC_MOVEMENT = 300, // Player's movement in the camp
    CAMP_CAMERA_MOVEMENT = 301, //Default
    CAMP_WORK_ASSIGNMENT = 302, // Assigning work to a settler
    BUILDING_PLACEMENT = 303, // Placing a building from the build menu

    /// <summary>
    /// Menus
    /// </summary>

    IN_CONVERSATION = 400, // Talking to an NPC
    IN_MENU = 401, // In any menu

    /// <summary>
    /// Turret
    /// </summary>

    TURRET_CAMERA_MOVEMENT = 500, //Default
    TURRET_PLACEMENT = 501, // Placing a turret from the turret menu

    /// <summary>
    /// Genetic Mutation UI
    /// </summary>

    GENETIC_MUTATION_MOVEMENT = 600, //Default

    /// <summary>
    /// Robot
    /// </summary>

    ROBOT_MOVEMENT = 700, // Player's movement when controlling the robot
    ROBOT_WORKING = 701, // Robot is performing a work task
}

// =========================
// GAME MODE AND ROOM SETUP ENUMS
// =========================

public enum GameMode
{
    NONE = 0,
    MAIN_MENU = 100,
    ROGUE_LITE = 200,
    CAMP = 300,
    TURRET = 500
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
public enum ResourceCategory //TODO: Use enum for resource categories for inventory menus
{
    GENERAL,
    FOOD,
    ELECTRICITY,
    DECORATION,
    WEAPONS,
    BASIC_BUILDING_MATERIAL,
    AMMO,
    CROP_SEED,
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
    // Category 0: None/Default
    NONE = 0,

    // Category 1: Human Types (1xx)
    HUMAN_MALE_1 = 101,
    HUMAN_MALE_2 = 102,
    HUMAN_FEMALE_1 = 103,
    HUMAN_FEMALE_2 = 104,

    // Category 2: Zombie Types (2xx)
    ZOMBIE_MELEE = 201,
    ZOMBIE_SPITTER = 202,
    ZOMBIE_TANK = 203,

    // Category 3: Machine Types (3xx)
    MACHINE_DRONE = 301,
    MACHINE_TURRET_BASE_TARGET = 302,
    MACHINE_ROBOT = 303,

    // Category 4: Boss Types (4xx)
    BOSS_1 = 401,
    BOSS_2 = 402,
    BOSS_3 = 403
}

// =========================
// MISCELLANEOUS ENUMS
// =========================

