using System;
using UnityEngine;

public class CharacterCombat : MonoBehaviour
{
    private AttackVFXHolder attackVFXHolder;

    private CharacterInventory characterInventory;

    private DashVFX dashVfx;
    private Transform dashVfxPoint;

    protected virtual void Awake()
    {
        characterInventory = GetComponent<CharacterInventory>();
        attackVFXHolder = GetComponentInChildren<AttackVFXHolder>();
        dashVfx = GetComponentInChildren<DashVFX>();
        dashVfxPoint = attackVFXHolder.GetDashTransform();
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
                attackVFXHolder.MeleeAttackVFX((MeleeAttackDirection)attackDirection, meleeWeapon);
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

    public void DashVFX()
    {
        dashVfx.Play(PlayerInventory.Instance.dashElement, dashVfxPoint);
    }
}
