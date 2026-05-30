using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
public class DestroyAttackOnExit : StateMachineBehaviour {
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        Animator playerAnimator = GameObject.Find("Player").GetComponentInChildren<Animator>();
        int IsAttacking = Animator.StringToHash("Attacking");
        playerAnimator.SetBool(IsAttacking, false);
        Destroy(animator.gameObject);
    }
}