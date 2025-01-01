using UnityEngine;

public class CombatAnimationState : StateMachineBehaviour
{
    HumanCharacterController humanCharacterController;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) //TODO this is stupid find some way of assigning the correct HumanCharacterController
    {
        // Check if the current reference is valid and enabled
        if (humanCharacterController == null || !humanCharacterController.enabled)
        {
            // Traverse up the hierarchy to find the first enabled HumanCharacterController
            Transform current = animator.transform;
            humanCharacterController = null;

            while (current != null)
            {
                HumanCharacterController controller = current.GetComponent<HumanCharacterController>();
                if (controller != null && controller.enabled)
                {
                    humanCharacterController = controller;
                    break;
                }
                current = current.parent; // Move up the hierarchy
            }
        }

        // Check if an enabled HumanCharacterController was found
        if (humanCharacterController == null)
        {
            Debug.LogWarning("No enabled HumanCharacterController was found in the hierarchy.");
            return; // Exit early if necessary
        }

        humanCharacterController.StopAttacking();
        animator.ResetTrigger("LightAttack");
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
