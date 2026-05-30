using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Spikes : MonoBehaviour {
    public float delay = 0.5f;
    public BoxCollider2D damageCollider;
    bool isOut = false;
    bool playerInTrigger = false;
    int stayOut = 60;
    bool isPoppingOut = false;

    void Update()
    {
        if(isOut)
        {
            tag = "Hazard";
        }
        else
        {
            tag = null;
        }
        if(playerInTrigger)
        {
            stayOut = 60;
            if(!isPoppingOut) 
            {
                StartCoroutine(popOutSpikes());
            }
        }
        if(stayOut > 0 && !playerInTrigger && isOut)
        {
            stayOut -= 1;
        }
        if(stayOut == 0 && !playerInTrigger && isOut)
        {
            // Spikes go away
            stayOut = 60;
            gameObject.GetComponentInChildren<Animator>().SetTrigger("Pop In");
            isPoppingOut = false;
            isOut = false;
        }
    }

    public IEnumerator popOutSpikes()
    {
        isPoppingOut = true;
        // Trigger animation after delay
        yield return new WaitForSeconds(delay);
        isOut = true;
        // Trigger animation
        gameObject.GetComponentInChildren<Animator>().SetTrigger("Pop Out");
        
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        //Check for a match with the specified name on any GameObject that collides with your GameObject
        if (collision.gameObject.tag == "Player" && !isOut)
        {
            playerInTrigger = true;
            StartCoroutine(popOutSpikes());
        }
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            stayOut = 60;
            playerInTrigger = false;
        }
    }
}
