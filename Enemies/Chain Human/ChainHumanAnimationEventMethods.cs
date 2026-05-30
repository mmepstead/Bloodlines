using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
class ChainHumanAnimationEventMethods : MonoBehaviour
{
    public GameObject groundHitPrefab;
    public void playWalkSound()
    {
        gameObject.transform.parent.gameObject.GetComponent<Movement>().playWalkSound();
    }

    public void spawnGroundHitEffect()
    {
        Instantiate(groundHitPrefab, transform.position + new Vector3(transform.localScale.x < 0 ? 1 : -1.2f,-0.4f,transform.position.z), Quaternion.identity);
    }
}