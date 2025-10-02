using System;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class CharacterCombat : MonoBehaviour
{
    private CharacterInventory characterInventory;

    [System.Serializable]
    public class AttackDirectionTransform
    {
        [Tooltip("The attack direction this transform represents")]
        public MeleeAttackDirection attackDirection;
        
        [Tooltip("Transform where VFX should be spawned and oriented")]
        public Transform vfxTransform;
    }

    [System.Serializable]
    public class MeleeElementalEffect
    {
        [Tooltip("The elemental type this effect is for")]
        public AttackElement element;
        
        [Tooltip("Effect definition for melee attacks")]
        public EffectDefinition meleeEffect;
    }

    [System.Serializable]
    public class DashElementalEffect
    {
        [Tooltip("The elemental type this effect is for")]
        public AttackElement element;
        
        [Tooltip("Effect definition for dash attacks")]
        public EffectDefinition dashEffect;
    }

    [Header("Attack Direction Transforms")]
    [Tooltip("Transforms for different melee attack directions")]
    [SerializeField] private AttackDirectionTransform[] attackDirectionTransforms = new AttackDirectionTransform[4];

    [Header("Dash VFX")]
    [Tooltip("Transform where dash VFX should be spawned")]
    [SerializeField] private Transform dashVfxTransform;

    [Header("Melee Elemental Effects")]
    [Tooltip("Effect definitions for different elemental types for melee attacks")]
    [SerializeField] private MeleeElementalEffect[] meleeElementalEffects = new MeleeElementalEffect[0];

    [Header("Dash Elemental Effects")]
    [Tooltip("Effect definitions for different elemental types for dash attacks")]
    [SerializeField] private DashElementalEffect[] dashElementalEffects = new DashElementalEffect[0];

    [Header("Settings")]
    [Tooltip("Default duration for attack VFX if not specified in effect definition")]
    [SerializeField] private float defaultVfxDuration = 2f;

    private Dictionary<MeleeAttackDirection, AttackDirectionTransform> directionTransformMap;
    private Dictionary<AttackElement, MeleeElementalEffect> meleeElementEffectMap;
    private Dictionary<AttackElement, DashElementalEffect> dashElementEffectMap;

    protected virtual void Awake()
    {
        characterInventory = GetComponent<CharacterInventory>();
        InitializeVFXMaps();
    }

    private void InitializeVFXMaps()
    {
        // Initialize direction transform mapping
        directionTransformMap = new Dictionary<MeleeAttackDirection, AttackDirectionTransform>();
        foreach (var directionTransform in attackDirectionTransforms)
        {
            if (directionTransform.vfxTransform != null)
            {
                directionTransformMap[directionTransform.attackDirection] = directionTransform;
            }
        }

        // Initialize melee element effect mapping
        meleeElementEffectMap = new Dictionary<AttackElement, MeleeElementalEffect>();
        foreach (var elementEffect in meleeElementalEffects)
        {
            meleeElementEffectMap[elementEffect.element] = elementEffect;
        }

        // Initialize dash element effect mapping
        dashElementEffectMap = new Dictionary<AttackElement, DashElementalEffect>();
        foreach (var elementEffect in dashElementalEffects)
        {
            dashElementEffectMap[elementEffect.element] = elementEffect;
        }
    }

    public void AttackVFX(int attackDirection)
    {
        WeaponScriptableObj equippedWeapon = characterInventory.equippedWeaponScriptObj;

        if (equippedWeapon == null)
        {
            Debug.LogWarning("No weapon is equipped!");
            return;
        }

        WeaponBase weaponBase = characterInventory.equippedWeaponBase;

        if (weaponBase == null)
        {
            Debug.LogWarning("No weapon base found on equipped weapon!");
            return;
        }

        switch (weaponBase)
        {
            case MeleeWeapon meleeWeapon:
                PlayMeleeAttackVFX((MeleeAttackDirection)attackDirection, meleeWeapon);
                break;

            case RangedWeapon rangedWeapon:
                RangedAttackVFX(rangedWeapon);
                break;

            case ThrowableWeapon throwableWeaponVFX:
                ThrowableWeaponVFX(throwableWeaponVFX);
                break;

            default:
                Debug.LogWarning($"{equippedWeapon.objectName} is of an unsupported weapon type!");
                break;
        }
    }    

    private void RangedAttackVFX(RangedWeapon rangedWeapon)
    {
        // TODO: Implement ranged weapon VFX
        // This should be similar to MeleeAttackVFX but for ranged weapons
        // You'll need to create a RangedAttackVFX prefab with appropriate particle systems
        Debug.LogWarning("Ranged weapon VFX not implemented yet!");
    }

    private void ThrowableWeaponVFX(ThrowableWeapon throwableWeaponVFX)
    {
        // TODO: Implement throwable weapon VFX
        // This should be similar to MeleeAttackVFX but for throwable weapons
        // You'll need to create a ThrowableAttackVFX prefab with appropriate particle systems
        Debug.LogWarning("Throwable weapon VFX not implemented yet!");
    }

    public void StopAttacking()
    {
        if (characterInventory.equippedWeaponBase != null)
            characterInventory.equippedWeaponBase.StopUse();
    }

    public void DashVFX()
    {
        PlayDashVFX(PlayerInventory.Instance.dashElement);
    }

    /// <summary>
    /// Plays melee attack VFX for the specified direction and element
    /// </summary>
    /// <param name="attackDirection">Direction of the melee attack</param>
    /// <param name="meleeWeapon">The melee weapon being used</param>
    private void PlayMeleeAttackVFX(MeleeAttackDirection attackDirection, MeleeWeapon meleeWeapon)
    {
        if (!directionTransformMap.TryGetValue(attackDirection, out var directionTransform))
        {
            Debug.LogWarning($"[CharacterCombat] No transform found for attack direction: {attackDirection}");
            return;
        }

        if (!meleeElementEffectMap.TryGetValue(meleeWeapon.WeaponData.weaponElement, out var elementEffect) || elementEffect.meleeEffect == null)
        {
            Debug.LogWarning($"[CharacterCombat] No melee effect found for element: {meleeWeapon.WeaponData.weaponElement}");
            return;
        }

        // Calculate position and rotation
        Vector3 position = directionTransform.vfxTransform.position;
        Vector3 normal = directionTransform.vfxTransform.forward;
        Quaternion rotation = directionTransform.vfxTransform.rotation;

        // Play the effect using EffectManager
        GameObject effectInstance = EffectManager.Instance.PlayEffect(
            position, 
            normal, 
            rotation, 
            directionTransform.vfxTransform, 
            elementEffect.meleeEffect,
            defaultVfxDuration
        );

        // Apply attack speed to particle systems if effect was created
        if (effectInstance != null)
        {
            ApplyAttackSpeedToEffect(effectInstance, meleeWeapon.GetCurrentAttackSpeed());
        }
    }

    /// <summary>
    /// Plays dash VFX for the specified element
    /// </summary>
    /// <param name="element">Elemental type of the dash</param>
    private void PlayDashVFX(AttackElement element)
    {
        if (dashVfxTransform == null)
        {
            Debug.LogWarning("[CharacterCombat] No dash transform specified!");
            return;
        }

        if (!dashElementEffectMap.TryGetValue(element, out var elementEffect) || elementEffect.dashEffect == null)
        {
            Debug.LogWarning($"[CharacterCombat] No dash effect found for element: {element}");
            return;
        }

        // Calculate position and rotation
        Vector3 position = dashVfxTransform.position;
        Vector3 normal = dashVfxTransform.forward;
        Quaternion rotation = dashVfxTransform.rotation;

        // Play the effect using EffectManager (not parented to transform)
        EffectManager.Instance.PlayEffect(
            position, 
            normal, 
            rotation, 
            null, // Don't parent to transform for dash effects
            elementEffect.dashEffect,
            defaultVfxDuration
        );
    }

    /// <summary>
    /// Applies attack speed multiplier to particle systems in the effect
    /// </summary>
    /// <param name="effectInstance">The effect GameObject</param>
    /// <param name="attackSpeed">Speed multiplier to apply</param>
    private void ApplyAttackSpeedToEffect(GameObject effectInstance, float attackSpeed)
    {
        if (effectInstance == null) return;

        // Find all particle systems in the effect and apply speed multiplier
        ParticleSystem[] particleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.simulationSpeed = attackSpeed;
            }
        }
    }

    /// <summary>
    /// Validates that all required transforms and effects are properly configured
    /// </summary>
    [ContextMenu("Validate VFX Configuration")]
    public void ValidateVFXConfiguration()
    {
        Debug.Log("[CharacterCombat] Validating VFX configuration...");

        // Check attack direction transforms
        foreach (MeleeAttackDirection direction in Enum.GetValues(typeof(MeleeAttackDirection)))
        {
            if (!directionTransformMap.ContainsKey(direction))
            {
                Debug.LogError($"[CharacterCombat] Missing transform for attack direction: {direction}");
            }
        }

        // Check melee elemental effects
        foreach (AttackElement element in Enum.GetValues(typeof(AttackElement)))
        {
            if (element == AttackElement.NONE) continue; // Skip NONE element

            if (!meleeElementEffectMap.ContainsKey(element))
            {
                Debug.LogWarning($"[CharacterCombat] No melee effect configuration for element: {element}");
            }
        }

        // Check dash elemental effects
        foreach (AttackElement element in Enum.GetValues(typeof(AttackElement)))
        {
            if (element == AttackElement.NONE) continue; // Skip NONE element

            if (!dashElementEffectMap.ContainsKey(element))
            {
                Debug.LogWarning($"[CharacterCombat] No dash effect configuration for element: {element}");
            }
        }

        // Check dash transform
        if (dashVfxTransform == null)
        {
            Debug.LogError("[CharacterCombat] Dash VFX transform is not assigned!");
        }

        Debug.Log("[CharacterCombat] VFX configuration validation complete.");
    }
}
