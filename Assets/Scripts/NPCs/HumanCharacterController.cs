using UnityEngine;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]

public class HumanCharacterController : MonoBehaviour
{
    [Header("Movement Parameters")]
    public float moveMaxSpeed = 10f; // Speed at which the player moves normally
    public float rotationSpeed = 720f; // Speed at which the player rotates

    protected bool isAttacking; // Whether the player is currently attacking

    [SerializeField] protected Animator animator;
    protected CharacterCombat characterCombat;

    public void EquipMeleeWeapon(int equipped)
    {
        animator.SetInteger("Equipped", equipped);
    }

    protected virtual void UpdateAnimations()
    {
        //TODO fill this out for AI characters to use a nav agent in a different child class
    }
}
