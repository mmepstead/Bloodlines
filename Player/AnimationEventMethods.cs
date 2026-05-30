using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
class AnimationEventMethods : MonoBehaviour
{
    public void playWalkSound()
    {
        gameObject.transform.parent.gameObject.GetComponent<Movement>().playWalkSound();
    }
}