using System;
using UnityEngine;
using UnityEngine.Serialization;
public class CoreOrbit : MonoBehaviour {
    static readonly int isBroken = Animator.StringToHash("Broken");
    public Animator coreAnimator;
    private void Update()
    {
        if(transform.Find("Core/Core Break(Clone)") == null)
        {
            transform.Rotate(new Vector3(0,0,2*Time.timeScale));   
        }     
    }
}
