// =========================
// SCENE NAMES ENUMS
// =========================

public enum SceneNames
{
    NONE,
    MainMenuScene,
    CampScene,
    LoadingScene,
    TransitionScene,
    TurretScene,
    OverworldScene,
    RogueLikeScene
}


// =========================
// BUILDING ENUMS
// =========================

/// <summary>
/// Building type used in roguelike section to determine the type of building the rooms spawn in
/// Also used by the effect system to determine which effects to play for buildings
/// </summary>
public enum RogueLikeBuildingType
{
    // Category 0: None/Default
    NONE = 0,
    
    // Early Game Buildings
    ABANDONED_HOUSE = 1,
    SMALL_WAREHOUSE = 2,
    CORNER_STORE = 3,
    
    // Mid Game Buildings  
    SHOPPING_MALL = 10,
    OFFICE_BUILDING = 11,
    APARTMENT_COMPLEX = 12,
    
    // Late Game Buildings
    HOSPITAL = 20,
    MILITARY_BASE = 21,
    RESEARCH_FACILITY = 22,
}

public enum RogueLikeRoomType
{
    NONE,
    HOSTILE,
    FRIENDLY
}



public enum CampBuildingCategory
{
    NONE,
    BASIC_BUILDING
}


[System.Serializable]
public enum CampPlaceableObjectCategory
{
    GENERAL,
    FOOD,
    ELECTRICITY,
    DECORATION,
    WEAPONS,
    CLEANING,
    RESOURCES,
    BUNKER,
    WALL,
    CAMP_DEFENSE
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
    EAT,
    FLEE,
    SHELTERED, // Added for NPCs in bunkers
    SLEEP, // Added for night time behavior
    MEDICAL_TREATMENT // Added for sick NPCs to receive medical treatment
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
    CLEARING_PLOT,
    //Sleep animation
    SLEEPING
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
    BUILDING_PLACEMENT = 303, // Placing a building or turret from the build menu    
    CAMP_ATTACK_CAMERA_MOVEMENT = 304, //Default when attacked by enemies

    /// <summary>
    /// Menus
    /// </summary>

    IN_CONVERSATION = 400, // Talking to an NPC
    IN_MENU = 401, // In any menu

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
// INPUT DEVICE ENUMS
// =========================

/// <summary>
/// Defines the type of input device being used
/// </summary>
public enum InputDeviceType
{
    MOUSE_KEYBOARD = 0,
    CONTROLLER = 1
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
    CAMP_ATTACK = 500
}

public enum EnemySetupState
{
    NONE,
    WAVE_START, //Player choses to open a door
    PRE_ENEMY_SPAWNING, //Period when the new room is spawned in, props, chests etc. are spawned in and nav mesh is baked
    ENEMY_SPAWN_START, //Enemies are spawned in
    ENEMIES_SPAWNED, //The room game play has started
    ALL_WAVES_CLEARED // enemies are all dea, player is free the move around and choose the next path
}

/// <summary>
/// Overall camp attack state - tracks whether the camp is under attack (entire wave cycle) or peaceful
/// </summary>
public enum CampAttackState
{
    PEACEFUL,        // No attack happening, NPCs can do normal activities
    UNDER_ATTACK     // Camp is under attack (wave cycle active), NPCs should flee/hide until morning or all waves cleared
}

public enum WallType
{
    ENABLED, // Model can be seen, collider enabled
    DISABLED, // Model and collider disabled
    HIDDEN // Model disabled, collider enabled
}

public enum DoorStatus
{
    LOCKED,   // Door is locked, cannot pass through
    UNLOCKED  // Door is unlocked, can pass through to next room or exit
}

// =========================
// ENEMY AND TARGET ENUMS
// =========================

// =========================
// RESOURCE ENUMS
// =========================

[System.Serializable]
public enum ItemCategory //TODO: Use enum for resource categories for inventory menus
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

public enum ItemRarity
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

public enum AttackElement
{
    NONE,
    BASIC,
    FIRE,
    ICE,
    ELECTRIC,
    POISON,
    BLEED,
    HOLY,
    SHADOW,
    PHYSICAL
}

public enum WeaponAnimationType
{
    NONE,
    ONE_HANDED,
    TWO_HANDED
}

/// <summary>
/// Defines resistance levels for different damage types
/// Used by characters to determine damage multipliers
/// </summary>
public enum DamageResistance
{
    IMMUNE = 0,      // 0% damage taken (0x multiplier)
    RESISTANT = 1,   // 50% damage taken (0.5x multiplier)
    NORMAL = 2,      // 100% damage taken (1x multiplier)
    WEAK = 3,        // 150% damage taken (1.5x multiplier)
    VULNERABLE = 4   // 200% damage taken (2x multiplier)
}

public enum GeneticMutation
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
    ZOMBIE_SPLITTING = 204,

    // Category 3: Machine Types (3xx)
    MACHINE_DRONE = 301,
    MACHINE_TURRET_BASE_TARGET = 302,
    MACHINE_ROBOT = 303,

    // Category 4: Boss Types (4xx)
    BOSS_1 = 401,
    BOSS_2 = 402,
    BOSS_3 = 403
}



/// <summary>
/// Defines how an NPC should be initialized when spawned
/// </summary>
public enum NPCInitializationContext
{
    FRESH_SPAWN,      // New NPC spawned in roguelike rooms, needs full random initialization (NO camp registration)
    CAMP_SPAWN,       // New NPC spawned directly in camp (game start/restart), registers with camp managers
    RECRUITED,        // NPC recruited from roguelike, may have predetermined characteristics  
    LOADED_FROM_SAVE  // NPC loaded from save file, should restore previous state
}

// =========================
// STATUS EFFECT ENUMS
// =========================

/// <summary>
/// Enum defining essential status effects that can affect NPCs and enemies
/// Only includes effects that are actually implemented with gameplay mechanics
/// </summary>
[System.Serializable]
public enum StatusEffectType
{
    // Core health states (actually implemented in SettlerNPC)
    HEALTHY,
    HUNGRY,
    STARVING,
    TIRED,
    EXHAUSTED,
    SICK,
    
    // Physical activity states (actually implemented in SettlerNPC)
    SLEEPING,
    WORKING,
    EATING,
    FIGHTING,
    FLEEING,

    
    // Environmental damage effects (used in weapon system)
    ON_FIRE,
    FROZEN,
    ELECTROCUTED,
    BURNING,
    SHOCKED,
    
    // Medical/Treatment effects (actually implemented)
    RECEIVING_MEDICAL_TREATMENT
}

/// <summary>
/// Priority levels for status effects when multiple effects are active
/// Higher priority effects will be displayed more prominently
/// </summary>
public enum StatusEffectPriority
{
    LOW = 0,
    NORMAL = 1,
    HIGH = 2,
    CRITICAL = 3,
    EMERGENCY = 4
}

/// <summary>
/// How status effects should behave when applied
/// </summary>
public enum StatusEffectBehavior
{
    REPLACE_EXISTING,    // Remove any existing effect of same type and apply new one
    STACK,              // Allow multiple instances of the same effect
    REFRESH_DURATION,   // Reset duration if effect already exists
    IGNORE_IF_EXISTS    // Don't apply if effect already active
}

// =========================
// MISCELLANEOUS ENUMS
// =========================

/// <summary>
/// Types of floating text for different visual styles
/// </summary>
public enum FloatingTextType
{
    Normal,
    Warning,
    Error,
    Success
}

/// <summary>
/// Types of projectile behaviors
/// Used to determine which projectile component to attach when spawning projectiles
/// </summary>
public enum ProjectileType
{
    /// <summary>
    /// No projectile behavior (static effect or instant hit)
    /// </summary>
    NONE = 0,
    
    /// <summary>
    /// Straight line projectile with constant velocity
    /// </summary>
    STRAIGHT = 1,
    
    /// <summary>
    /// Arcing projectile that follows a parabolic trajectory
    /// </summary>
    ARC = 2,
    
    /// <summary>
    /// Homing projectile that tracks a target
    /// </summary>
    HOMING = 3,
    
    /// <summary>
    /// Ballistic projectile affected by gravity
    /// </summary>
    BALLISTIC = 4
}

