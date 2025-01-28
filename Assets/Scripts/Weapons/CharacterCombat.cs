using System;
using UnityEngine;

public class CharacterCombat : MonoBehaviour
{
    private AttackVFXHolder attackVFXHolder;

    private CharacterInventory characterInventory;

    public virtual void Start() //TODO add this to the event for when a player takes over an NPC
    {
        characterInventory = GetComponentInParent<CharacterInventory>();
    }

    public void AttackVFX(int attackDirection)
    {
        WeaponScriptableObj equippedWeapon = characterInventory.equippedWeaponScriptObj;

        if (equippedWeapon == null)
        {
            Debug.LogWarning("No weapon is equipped!");
            return;
        }

        switch (equippedWeapon.weaponPrefab.GetComponent<WeaponBase>())
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
            case MeleeAttackDirection.HORIZONTAL_LEFT:
                attackVFXHolder.horizontalLeftVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            case MeleeAttackDirection.HORIZONTAL_RIGHT:
                attackVFXHolder.horizontalRightVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            case MeleeAttackDirection.VERTICAL_DOWN:
                attackVFXHolder.verticalDownVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            case MeleeAttackDirection.VERTICAL_UP:
                attackVFXHolder.verticalUpVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            default:
                Debug.LogWarning("Invalid attack direction.");
                break;
        }
    }

    private void StopAllMeleeVFX(MeleeWeapon meleeWeapon)
    {
        attackVFXHolder.horizontalLeftVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
        attackVFXHolder.horizontalRightVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
        attackVFXHolder.verticalDownVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
        attackVFXHolder.verticalUpVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
    }

    private void RangedAttackVFX(RangedWeapon rangedWeapon)
    {
        throw new NotImplementedException();
    }

    private void ThrowableWeaponVFX(ThrowableWeapon throwableWeaponVFX)
    {
        throw new NotImplementedException();
    }

    public void StopAttacking()
    {
        if (characterInventory.equippedWeaponBase != null)
            characterInventory.equippedWeaponBase.StopUse();
    }
}
