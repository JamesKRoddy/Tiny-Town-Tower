// =========================
// BUILDING ENUMS
// =========================

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
    FARMING, // TODO: might have to break this down into several parts
    GATHER_WOOD,
    GATHER_ROCK,
    WEAVE_FABRIC,
    MAKE_AMMO,
    GENERATE_ELECTRICITY,
    BUILD_STRUCTURE,
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
    COMBAT_MOVEMENT, // In combat during roguelike sections
    CAMP_MOVEMENT, // Player's movement in the camp
    BUILDING, // Placing a building from the build menu
    IN_CONVERSATION, // Talking to an NPC
    IN_MENU, // In any menu
    TURRET_PLACEMENT // Placing a turret from the turret menu
}

// =========================
// GAME MODE AND ROOM SETUP ENUMS
// =========================

public enum CurrentGameMode
{
    NONE,
    ROGUE_LITE,
    CAMP,
    TURRET
}

public enum RoomSetupState
{
    NONE,
    ENTERING_ROOM, //Player choses to open a door
    PRE_ENEMY_SPAWNING, //Period when the new room is spawned in, props, chests etc. are spawned in and nav mesh is baked
    ENEMIES_SPAWNED, //The room game play has started
    ROOM_CLEARED // enemies are all dea, player is free the move around and choose the next path
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
// MISCELLANEOUS ENUMS
// =========================

