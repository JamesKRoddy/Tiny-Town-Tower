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
                horizontalLeftVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            case MeleeAttackDirection.HORIZONTAL_RIGHT:
                horizontalRightVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            case MeleeAttackDirection.VERTICAL_DOWN:
                verticalDownVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
                break;
            case MeleeAttackDirection.VERTICAL_UP:
                verticalUpVfx.Play(meleeWeapon.weaponScriptableObj.weaponElement);
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
        horizontalLeftVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
        horizontalRightVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
        verticalDownVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
        verticalUpVfx.Stop(meleeWeapon.weaponScriptableObj.weaponElement);
    }
}
