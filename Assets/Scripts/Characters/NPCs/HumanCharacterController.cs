using UnityEngine;



public class HumanCharacterController : MonoBehaviour
{
    [Header("Movement Parameters")]
    public float moveMaxSpeed = 10f; // Speed at which the player moves normally
    public float rotationSpeed = 720f; // Speed at which the player rotates

    protected bool isAttacking; // Whether the player is currently attacking

    public Animator animator;
    protected CharacterCombat characterCombat;

    public void EquipMeleeWeapon(int equipped)
    {
        animator.SetInteger("Equipped", equipped);
    }

    protected virtual void UpdateAnimations()
    {
        
    }

    //Called from animator
    public void StopAttacking()
    {
        isAttacking = false;
    }
}
