using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Player;
using UnityEngine;

public class LandingState : StateMachineBehaviour
{
    public float timeCanMove = 0.3f;
	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        AInput input = animator.GetComponent<AInput>();
        if (input)
        {
            input.SetLockMove(true);
            input.SetLockMove(false, timeCanMove);
        }
    }

	// OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
	//override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

    }

	// OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
	//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}
//    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
//    {
//        AInput input = animator.GetComponent<AInput>();
//        if (input)
//        {
//            input.SetLockMove(true);
//        }
//    }
//
//    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
//    {
//        AInput input = animator.GetComponent<AInput>();
//        if (input)
//        {
//            input.SetLockMove(false, 1f);
//        }
//    }
}
