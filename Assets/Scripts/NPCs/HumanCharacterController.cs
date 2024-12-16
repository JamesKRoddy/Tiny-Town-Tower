using UnityEngine;

public class HumanCharacterController : MonoBehaviour
{
    [Header("Movement Parameters")]
    public float moveSpeed = 10f; // Speed at which the player moves normally
    public float rotationSpeed = 720f; // Speed at which the player rotates

    protected bool isAttacking; // Whether the player is currently attacking

    [SerializeField] protected Animator animator;
    protected CharacterCombat characterCombat;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EquipMeleeWeapon(int equipped)
    {
        animator.SetInteger("Equipped", equipped);
    }

    protected virtual void UpdateAnimations()
    {
        //TODO fill this out for AI characters to use a nav agent in a different child class
    }
}
