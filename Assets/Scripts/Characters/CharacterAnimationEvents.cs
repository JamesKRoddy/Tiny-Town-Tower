using UnityEngine;

public class CharacterAnimationEvents : MonoBehaviour
{
    public CharacterCombat playerCombat;
    public HumanCharacterController playerController;

    public void Setup(CharacterCombat characterCombat =  null, HumanCharacterController characterController = null) //TOD add this to the npc swap event, this also might be stupid
    {
        playerCombat = characterCombat;
        playerController = characterController;
    }

    public void AttackVFX(int attackDirection)
    {
        if(playerCombat != null)
            playerCombat.AttackVFX(attackDirection);
    }

    public void StopAttacking()
    {
        playerController.StopAttacking();
    }
}
