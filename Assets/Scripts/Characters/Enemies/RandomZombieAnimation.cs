using UnityEngine;

public enum AnimatorParamType
{
    INT,
    FLOAT
}

public class RandomZombieAnimation : StateMachineBehaviour
{
    public AnimatorParamType paramType = AnimatorParamType.INT;
    public string paramName;
    public int minNumber = 0;
    public int maxNumber = 1;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int randomNumber = Random.Range(minNumber, maxNumber + 1);

        switch (paramType)
        {
            case AnimatorParamType.INT:
                animator.SetInteger(paramName, randomNumber);
                break;
            case AnimatorParamType.FLOAT:
                animator.SetFloat(paramName, randomNumber);
                break;
            default:
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
