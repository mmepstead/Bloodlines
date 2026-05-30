using System;
using UnityEngine;
using UnityEngine.Serialization;
public class DestroyRiposteOnExit : StateMachineBehaviour {
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        Animator playerAnimator = GameObject.Find("Player").GetComponentInChildren<Animator>();
        int IsRiposting = Animator.StringToHash("Riposting");
        playerAnimator.SetBool(IsRiposting, false);
    }
}