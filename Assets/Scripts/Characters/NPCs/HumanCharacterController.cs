using UnityEngine;
using UnityEngine.AI;

public class HumanCharacterController : MonoBehaviour
{
    [Header("Movement Parameters")]
    public float moveMaxSpeed = 10f; // Speed at which the player moves normally
    public float rotationSpeed = 720f; // Speed at which the player rotates

    protected bool isAttacking; // Whether the player is currently attacking

    public Animator animator;
    protected CharacterCombat characterCombat;

    /// <summary>
    /// Used to switch player controls to a new npc
    /// </summary>
    /// <param name="isAIControlled"></param>
    public void ToggleNPCComponents(bool isAIControlled, GameObject npc)
    {
        npc.GetComponent<NavMeshAgent>().enabled = isAIControlled;
        npc.GetComponent<NarrativeInteractive>().enabled = isAIControlled;
        npc.GetComponent<SettlerNPC>().enabled = isAIControlled;

        foreach (var item in npc.GetComponents<_TaskState>())
        {
            item.enabled = isAIControlled;
        }

        if (isAIControlled)
        {
            npc.transform.SetParent(null);
            npc.GetComponent<SettlerNPC>().ChangeTask(TaskType.WANDER);
            npc.GetComponent<CharacterAnimationEvents>().Setup();
        }
        else
        {
            transform.parent = PlayerController.Instance.gameObject.transform;
            PlayerController.Instance.animator = npc.GetComponent<Animator>();
            PlayerController.Instance.playerCamera.target = npc.transform;
            npc.GetComponent<CharacterAnimationEvents>().Setup(PlayerCombat.Instance, PlayerController.Instance, PlayerInventory.Instance);
            PlayerController.Instance.PossessNPC(npc);
        }
    }

    public void EquipMeleeWeapon(int equipped)
    {
        animator.SetInteger("Equipped", equipped);
    }

    protected virtual void UpdateAnimations()
    {
        
    }

    //Called from animator state class CombatAnimationState
    public void StopAttacking()
    {
        isAttacking = false;
        if(characterCombat != null)
            characterCombat.StopAttacking();
    }
}
