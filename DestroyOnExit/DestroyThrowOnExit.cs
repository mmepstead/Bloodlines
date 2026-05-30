using System;
using UnityEngine;
using UnityEngine.Serialization;
public class DestroyThrowOnExit : StateMachineBehaviour {
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        Destroy(animator.gameObject);
    }
}