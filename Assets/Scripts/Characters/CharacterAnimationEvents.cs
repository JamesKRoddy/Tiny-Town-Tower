using UnityEngine;

public class CharacterAnimationEvents : MonoBehaviour
{
    private CharacterCombat playerCombat;
    private HumanCharacterController playerController;
    private CharacterInventory playerInventory;

    public void Setup(CharacterCombat characterCombat =  null, HumanCharacterController characterController = null, CharacterInventory characterInventory = null) //TOD add this to the npc swap event, this also might be stupid
    {
        playerCombat = characterCombat;
        playerController = characterController;
        playerInventory = characterInventory;
    }

    public void AttackVFX(int attackDirection)
    {
        if(playerCombat != null)
            playerCombat.AttackVFX(attackDirection);
    }

    //Called from animator
    public void UseWeapon()
    {
        if (playerInventory.equippedWeaponScriptObj != null)
            playerInventory.equippedWeaponBase.Use();
    }

    public void StopWeapon()
    {
        if (playerInventory.equippedWeaponScriptObj != null)
            playerInventory.equippedWeaponBase.StopUse();
    }
}
