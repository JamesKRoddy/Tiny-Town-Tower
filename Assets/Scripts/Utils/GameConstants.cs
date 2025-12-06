using UnityEngine;

/// <summary>
/// Centralized constants for all hardcoded strings in the game.
/// This prevents typos and makes it easier to refactor string literals.
/// </summary>
public static class GameConstants
{
    #region Tags
    public static class Tags
    {
        public const string Enemy = "Enemy";
        public const string Player = "Player";
        public const string NPCSpawn = "NPCSpawn";
        public const string Destructible = "Destructible";
    }
    #endregion

    #region Layers
    public static class Layers
    {
        public const string Enemy = "Enemy";
        public const string Player = "Player";
        public const string Settler = "Settler";
        public const string Weapon = "Weapon";
        public const string Water = "Water";
        public const string IgnoreRaycast = "Ignore Raycast";

        // Layer mask integers (cached for performance)
        public static readonly int EnemyLayer = LayerMask.NameToLayer(Enemy);
        public static readonly int PlayerLayer = LayerMask.NameToLayer(Player);
        public static readonly int SettlerLayer = LayerMask.NameToLayer(Settler);
        public static readonly int WeaponLayer = LayerMask.NameToLayer(Weapon);
        public static readonly int WaterLayer = LayerMask.NameToLayer(Water);
        public static readonly int IgnoreRaycastLayer = LayerMask.NameToLayer(IgnoreRaycast);
        
        /// <summary>
        /// Layer mask that excludes character layers (for raycasting to ground)
        /// Includes Water layer so triggers like poison pools and water can be detected by footsteps
        /// Use this for footstep detection, surface detection, etc.
        /// </summary>
        public static readonly int GroundDetectionMask = ~LayerMask.GetMask(Enemy, Player, Settler, IgnoreRaycast);
    }
    #endregion

    #region Animator Parameters
    public static class AnimatorParams
    {
        // Triggers
        public const string Attack = "Attack";
        public const string IsDashing = "IsDashing";
        public const string IsVaulting = "IsVaulting";
        public const string IsRolling = "IsRolling";
        public const string IsClimbing = "IsClimbing";
        public const string Damaged = "Damaged";
        public const string Knockback = "Knockback";
        public const string Dead = "Dead";
        public const string IsEating = "IsEating";

        // Bools
        public const string IsPushing = "IsPushing";

        // Floats
        public const string Speed = "Speed";
        public const string AttackSpeed = "AttackSpeed";
        public const string RotationSpeedMultiplier = "RotationSpeedMultiplier";
        public const string HitDirectionX = "HitDirectionX";
        public const string HitDirectionY = "HitDirectionY";

        // Integers
        public const string WeaponType = "WeaponType";
        public const string DamageType = "DamageType";
        public const string AttackType = "AttackType";

        // Hash IDs for performance (prevents string lookups every frame)
        public static readonly int SpeedHash = Animator.StringToHash(Speed);
        public static readonly int AttackSpeedHash = Animator.StringToHash(AttackSpeed);
        public static readonly int RotationSpeedMultiplierHash = Animator.StringToHash(RotationSpeedMultiplier);
        public static readonly int HitDirectionXHash = Animator.StringToHash(HitDirectionX);
        public static readonly int HitDirectionYHash = Animator.StringToHash(HitDirectionY);
        public static readonly int WeaponTypeHash = Animator.StringToHash(WeaponType);
        public static readonly int DamageTypeHash = Animator.StringToHash(DamageType);
        public static readonly int AttackTypeHash = Animator.StringToHash(AttackType); // Used by AI and Player for the attack type, for players it can be 1 = Light Attack, 2 = Heavy Attack, 3 = Special Attack etc.
        public static readonly int AttackHash = Animator.StringToHash(Attack);
        public static readonly int IsDashingHash = Animator.StringToHash(IsDashing);
        public static readonly int IsVaultingHash = Animator.StringToHash(IsVaulting);
        public static readonly int IsRollingHash = Animator.StringToHash(IsRolling);
        public static readonly int IsClimbingHash = Animator.StringToHash(IsClimbing);
        public static readonly int DamagedHash = Animator.StringToHash(Damaged);
        public static readonly int KnockbackHash = Animator.StringToHash(Knockback);
        public static readonly int DeadHash = Animator.StringToHash(Dead);
        public static readonly int IsEatingHash = Animator.StringToHash(IsEating);
        public static readonly int IsPushingHash = Animator.StringToHash(IsPushing);
    }
    #endregion

    #region Input Axes
    public static class InputAxes
    {
        // Movement axes
        public const string Horizontal = "Horizontal";
        public const string Vertical = "Vertical";
        public const string RightStickHorizontal = "RightStickHorizontal";
        public const string RightStickVertical = "RightStickVertical";

        // Trigger axes
        public const string RT = "RT";
        public const string LT = "LT";

        // Mouse axes
        public const string MouseX = "Mouse X";
        public const string MouseY = "Mouse Y";
        public const string MouseScrollWheel = "Mouse ScrollWheel";
    }
    #endregion

    #region Input Buttons
    public static class InputButtons
    {
        // Face buttons
        public const string A = "A";
        public const string B = "B";
        public const string X = "X";
        public const string Y = "Y";

        // Shoulder buttons
        public const string LB = "LB";
        public const string RB = "RB";

        // Stick presses
        public const string LeftStickPress = "LeftStickPress";
        public const string RightStickPress = "RightStickPress";

        // Menu buttons
        public const string Start = "Start";
        public const string Select = "Select";
    }
    #endregion

    #region Shader Properties
    public static class ShaderProperties
    {
        public const string Mode = "_Mode";
        public const string NoiseScale = "_NoiseScale";
        public const string MaskCutOut = "_MaskCutOut";

        // Property IDs for performance
        public static readonly int ModeID = Shader.PropertyToID(Mode);
        public static readonly int NoiseScaleID = Shader.PropertyToID(NoiseScale);
        public static readonly int MaskCutOutID = Shader.PropertyToID(MaskCutOut);
    }
    #endregion

    #region Animation Events
    /// <summary>
    /// Animation event function names called from animation clips
    /// Use these constants when adding animation events to prevent typos
    /// </summary>
    public static class AnimationEvents
    {
        // Footstep events
        public const string FootstepLeft = "FootstepLeft";
        public const string FootstepRight = "FootstepRight";
        public const string Footstep = "Footstep";

        // Combat events - STANDARDIZED names for all characters (NPCs and Enemies)
        public const string AttackVFX = "AttackVFX";
        public const string Attack = "Attack";           // Standardized: Execute damage on animation frame
        public const string AttackEnd = "AttackEnd";     // Standardized: End attack animation
        
        // DEPRECATED: Use Attack and AttackEnd instead
        [System.Obsolete("Use Attack instead")]
        public const string UseWeapon = "Attack";
        [System.Obsolete("Use AttackEnd instead")]
        public const string StopWeapon = "AttackEnd";

        // Work/Task events
        public const string PlayTaskAnimationEffect = "PlayTaskAnimationEffect";

        /// <summary>
        /// Attack direction values passed as int parameters to AttackVFX events
        /// </summary>
        public static class AttackDirection
        {
            public const int HorizontalLeft = 0;
            public const int HorizontalRight = 1;
            public const int VerticalDown = 2;
            public const int VerticalUp = 3;
        }

        /// <summary>
        /// Check if an event name is a footstep event
        /// </summary>
        public static bool IsFootstepEvent(string eventName)
        {
            return eventName == FootstepLeft || 
                   eventName == FootstepRight || 
                   eventName == Footstep;
        }

        /// <summary>
        /// Check if an event name is a combat event
        /// </summary>
        public static bool IsCombatEvent(string eventName)
        {
            return eventName == AttackVFX || 
                   eventName == Attack || 
                   eventName == AttackEnd ||
                   eventName == UseWeapon ||  // Deprecated, but still check for backwards compatibility
                   eventName == StopWeapon;   // Deprecated, but still check for backwards compatibility
        }

        /// <summary>
        /// Check if an event name is a work/task event
        /// </summary>
        public static bool IsWorkEvent(string eventName)
        {
            return eventName == PlayTaskAnimationEffect;
        }

        /// <summary>
        /// Get all footstep event names
        /// </summary>
        public static string[] GetFootstepEventNames()
        {
            return new[] { FootstepLeft, FootstepRight, Footstep };
        }

        /// <summary>
        /// Get all combat event names (standardized)
        /// </summary>
        public static string[] GetCombatEventNames()
        {
            return new[] { AttackVFX, Attack, AttackEnd };
        }

        /// <summary>
        /// Get all work event names
        /// </summary>
        public static string[] GetWorkEventNames()
        {
            return new[] { PlayTaskAnimationEffect };
        }

        /// <summary>
        /// Get all animation event names used in the project
        /// </summary>
        public static string[] GetAllEventNames()
        {
            return new[]
            {
                // Footsteps
                FootstepLeft,
                FootstepRight,
                Footstep,
                
                // Combat (standardized)
                AttackVFX,
                Attack,
                AttackEnd,
                
                // Work
                PlayTaskAnimationEffect
            };
        }
    }
    #endregion
}
