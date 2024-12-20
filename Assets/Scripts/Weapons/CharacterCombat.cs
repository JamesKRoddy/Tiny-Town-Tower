using System;
using UnityEngine;

public class CharacterCombat : MonoBehaviour
{
    [Header("Melee VFX")]
    public MeleeAttackVFX horizontalLeftVfx;
    public MeleeAttackVFX horizontalRightVfx;
    public MeleeAttackVFX verticalDownVfx;
    public MeleeAttackVFX verticalUpVfx;

    private CharacterInventory characterInventory;

    public virtual void Start() //TODO add this to the event for when a player takes over an NPC
    {
        characterInventory = GetComponentInParent<CharacterInventory>();
    }

    public void AttackVFX(int attackDirection)
    {
        WeaponBase equippedWeapon = characterInventory.equippedWeapon;

        if (equippedWeapon == null)
        {
            Debug.LogWarning("No weapon is equipped!");
            return;
        }

        switch (equippedWeapon)
        {
            case MeleeWeapon meleeWeapon:
                MeleeAttackVFX((MeleeAttackDirection)attackDirection, meleeWeapon);
                break;

            case RangedWeapon rangedWeapon:
                RangedAttackVFX(rangedWeapon);
                break;

            case ThrowableWeapon throwableWeaponVFX:
                ThrowableWeaponVFX(throwableWeaponVFX);
                break;

            default:
                Debug.LogWarning($"{equippedWeapon.name} is of an unsupported weapon type!");
                break;
        }
    }

    public void MeleeAttackVFX(MeleeAttackDirection attackDirection, MeleeWeapon meleeWeapon)
    {
        // Stop all VFX to reset their states
        StopAllMeleeVFX(meleeWeapon);

        switch (attackDirection)
        {
            case MeleeAttackDirection.HorizontalLeft:
                horizontalLeftVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            case MeleeAttackDirection.HorizontalRight:
                horizontalRightVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            case MeleeAttackDirection.VerticalDown:
                verticalDownVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            case MeleeAttackDirection.VerticalUp:
                verticalUpVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            default:
                Debug.LogWarning("Invalid attack direction.");
                break;
        }
    }

    private void StopAllMeleeVFX(MeleeWeapon meleeWeapon)
    {
        horizontalLeftVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
        horizontalRightVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
        verticalDownVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
        verticalUpVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
    }

    private void RangedAttackVFX(RangedWeapon rangedWeapon)
    {
        throw new NotImplementedException();
    }

    private void ThrowableWeaponVFX(ThrowableWeapon throwableWeaponVFX)
    {
        throw new NotImplementedException();
    }
}


public enum MeleeAttackDirection
{
    HorizontalLeft,
    HorizontalRight,
    VerticalDown,
    VerticalUp
}
