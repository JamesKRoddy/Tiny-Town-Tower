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

    public void AttackVFX(int attackDirection)
    {
        if(combat != null)
            combat.AttackVFX(attackDirection);
    }

    //Called from animator
    public void UseWeapon()
    {
        if (inventory.equippedWeaponScriptObj != null)
            inventory.equippedWeaponBase.Use();
    }

    public void StopWeapon()
    {
        if (inventory.equippedWeaponScriptObj != null)
            inventory.equippedWeaponBase.StopUse();
    }
}
