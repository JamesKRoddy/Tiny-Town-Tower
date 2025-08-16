using UnityEngine;

public class AnimatorStateBehaviour : StateMachineBehaviour
{

    [SerializeField] private bool isRootMotionEnabled = false;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Check if this layer should control root motion by comparing with other active layers
        if (ShouldControlRootMotion(animator, layerIndex))
        {
            animator.applyRootMotion = isRootMotionEnabled;
        }
    }

    private bool ShouldControlRootMotion(Animator animator, int currentLayerIndex)
    {
        // Get the current layer's weight
        float currentLayerWeight = animator.GetLayerWeight(currentLayerIndex);
        
        // If this layer has zero weight, it shouldn't control root motion
        if (currentLayerWeight <= 0f)
            return false;

        // Check all other layers to see if any higher priority layer is active
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (i == currentLayerIndex) continue;
            
            // Check if another layer is active and has higher priority
            if (animator.GetLayerWeight(i) > 0f && IsStateActuallyPlaying(animator.GetCurrentAnimatorStateInfo(i)))
            {
                // If this layer has lower priority (higher index) and the other layer is active,
                // this layer shouldn't control root motion
                if (currentLayerIndex > i)
                    return false;
            }
        }
        
        return true;
    }

    private bool IsStateActuallyPlaying(AnimatorStateInfo stateInfo)
    {
        // Check if the state is actually playing by looking at multiple indicators
        return stateInfo.length > 0f && 
               stateInfo.normalizedTime >= 0f && 
               stateInfo.normalizedTime < 1f &&
               stateInfo.fullPathHash != 0;
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
