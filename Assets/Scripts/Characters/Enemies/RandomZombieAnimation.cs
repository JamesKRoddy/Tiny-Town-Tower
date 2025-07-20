using UnityEngine;

/// <summary>
/// Animation State Machine Behaviour that randomizes animation parameters when entering a state.
/// 
/// IMPORTANT: This script determines the TYPE/STYLE of walk animation the zombie will use,
/// NOT the speed of the NavMesh agent. The agent's speed is controlled by the NavMeshAgent
/// component and the movement scripts.
/// 
/// For example, this might select between different walking styles:
/// - paramName: "WalkStyle" with values 0-2 for different walk animations
/// - paramName: "IdleVariation" with values 0-3 for different idle poses
/// 
/// The actual movement speed is controlled by:
/// - NavMeshAgent.speed (for non-root motion)
/// - Root motion from the selected animation (for root motion)
/// - Animation parameter "WalkType" (0 = idle, 1 = moving)
/// </summary>
public enum AnimatorParamType
{
    INT,
    FLOAT
}

public class RandomZombieAnimation : StateMachineBehaviour
{
    [Header("Animation Randomization")]
    [Tooltip("Type of parameter to randomize")]
    public AnimatorParamType paramType = AnimatorParamType.INT;
    
    [Tooltip("Name of the animation parameter to randomize (e.g., 'WalkStyle', 'IdleVariation')")]
    public string paramName;
    
    [Tooltip("Minimum value for randomization (inclusive)")]
    public int minNumber = 0;
    
    [Tooltip("Maximum value for randomization (inclusive for INT, exclusive for FLOAT)")]
    public int maxNumber = 1;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Generate random number within specified range
        int randomNumber = Random.Range(minNumber, maxNumber + 1);

        // Apply the random value to the specified parameter
        switch (paramType)
        {
            case AnimatorParamType.INT:
                animator.SetInteger(paramName, randomNumber);
                break;
            case AnimatorParamType.FLOAT:
                animator.SetFloat(paramName, randomNumber);
                break;
            default:
                Debug.LogWarning($"Unknown AnimatorParamType: {paramType}");
                break;
        }
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
