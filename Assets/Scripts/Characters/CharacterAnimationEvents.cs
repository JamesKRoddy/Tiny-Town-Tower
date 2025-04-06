using UnityEngine;

public class CharacterAnimationEvents : MonoBehaviour
{
    private CharacterCombat combat;
    private HumanCharacterController controller;
    private CharacterInventory inventory;

    public void Setup(CharacterCombat characterCombat =  null, HumanCharacterController characterController = null, CharacterInventory characterInventory = null) //TOD add this to the npc swap event, this also might be stupid
    {
        combat = characterCombat;
        controller = characterController;
        inventory = characterInventory;
    }

    #region Animation Events
    /// <summary>
    /// Called from animator, enables the weapon vfx
    /// </summary>
    /// <param name="attackDirection"></param>
    public void AttackVFX(int attackDirection)
    {
        if(combat != null)
            combat.AttackVFX(attackDirection);
    }

    /// <summary>
    /// Called from animator, enables the weapon hitbox for melee weapons
    /// </summary>
    public void UseWeapon()
    {
        if (inventory.equippedWeaponScriptObj != null)
            inventory.equippedWeaponBase.Use();
    }

    /// <summary>
    /// Called from animator, disables the weapon hitbox for melee weapons
    /// </summary>
    public void StopWeapon()
    {
        if (inventory.equippedWeaponScriptObj != null)
            inventory.equippedWeaponBase.StopUse();
    }
    #endregion
}
