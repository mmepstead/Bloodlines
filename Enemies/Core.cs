using System;
using UnityEngine;
using UnityEngine.Serialization;
public class Core : MonoBehaviour {

    int shineTimer = 300;
    public Animator animator;
    System.Random rnd = new System.Random();
    static readonly int IsShining = Animator.StringToHash("Shine");
    private void Update()
    {
        shineTimer -= 1;
        if(shineTimer == 0 && animator != null)
        {
            animator.SetTrigger(IsShining);
            shineTimer = rnd.Next(800,1200);
        }
    }
}
