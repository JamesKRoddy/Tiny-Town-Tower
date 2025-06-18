using System;
using UnityEngine;

public class AttackVFXHolder : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private Transform dashVFXPoint;

    [Header("Melee Attack")]
    [SerializeField] private MeleeAttackVFX horizontalLeftVfx;
    [SerializeField] private MeleeAttackVFX horizontalRightVfx;
    [SerializeField] private MeleeAttackVFX verticalDownVfx;
    [SerializeField] private MeleeAttackVFX verticalUpVfx;


    public void MeleeAttackVFX(MeleeAttackDirection attackDirection, MeleeWeapon meleeWeapon)
    {
        // Stop all VFX to reset their states
        StopAllMeleeVFX(meleeWeapon);

        switch (attackDirection)
        {
            case MeleeAttackDirection.HORIZONTAL_LEFT:
                horizontalLeftVfx.Play(meleeWeapon.WeaponData.weaponElement, meleeWeapon.GetCurrentAttackSpeed());
                break;
            case MeleeAttackDirection.HORIZONTAL_RIGHT:
                horizontalRightVfx.Play(meleeWeapon.WeaponData.weaponElement, meleeWeapon.GetCurrentAttackSpeed());
                break;
            case MeleeAttackDirection.VERTICAL_DOWN:
                verticalDownVfx.Play(meleeWeapon.WeaponData.weaponElement, meleeWeapon.GetCurrentAttackSpeed());
                break;
            case MeleeAttackDirection.VERTICAL_UP:
                verticalUpVfx.Play(meleeWeapon.WeaponData.weaponElement, meleeWeapon.GetCurrentAttackSpeed());
                break;
            default:
                Debug.LogWarning("Invalid attack direction.");
                break;
        }
    }

    internal Transform GetDashTransform()
    {
        return dashVFXPoint;
    }

    private void StopAllMeleeVFX(MeleeWeapon meleeWeapon)
    {
        horizontalLeftVfx.Stop(meleeWeapon.WeaponData.weaponElement);
        horizontalRightVfx.Stop(meleeWeapon.WeaponData.weaponElement);
        verticalDownVfx.Stop(meleeWeapon.WeaponData.weaponElement);
        verticalUpVfx.Stop(meleeWeapon.WeaponData.weaponElement);
    }
}
