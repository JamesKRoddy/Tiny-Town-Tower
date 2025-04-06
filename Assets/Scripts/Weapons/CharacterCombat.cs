using System;
using UnityEngine;

public class CharacterCombat : MonoBehaviour
{
    private AttackVFXHolder attackVFXHolder;

    private CharacterInventory characterInventory;

    private DashVFX dashVfx;

    protected virtual void Awake()
    {
        characterInventory = GetComponent<CharacterInventory>();
        attackVFXHolder = GetComponentInChildren<AttackVFXHolder>();
        dashVfx = GetComponentInChildren<DashVFX>();
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
                attackVFXHolder.MeleeAttackVFX((MeleeAttackDirection)attackDirection, meleeWeapon);
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
        dashVfx.Play(PlayerInventory.Instance.dashElement, attackVFXHolder.GetDashTransform());
    }
}
