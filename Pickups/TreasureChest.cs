using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
public class TreasureChest : MonoBehaviour {
    public Animator animator;
    public GameObject openParticles;
    public GameObject sparkleParticles;
    public int redPesetaCount = 0;
    public int pesetaCount = 0;
    public int silverPesetaCount = 0;
    public GameObject redPesetaPrefab;
    public GameObject pesetaPrefab;
    public GameObject silverPesetaPrefab;
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q) && transform.Find("Key").gameObject.active == true)
        {
            //Open chest code here
            animator.enabled = true;
            openParticles.SetActive(true);
            sparkleParticles.SetActive(false);
            transform.Find("Key").gameObject.SetActive(false);
            Destroy(gameObject.GetComponent<ButtonPrompt>());
            StartCoroutine(openChest());
            //openParticles2.SetActive(true);
        }
    }

    private IEnumerator openChest()
    {
        yield return new WaitForSeconds(1f);
        //Spawn pesetas here
        for(int i = 0; i < redPesetaCount; i++)
        {
            GameObject peseta = Instantiate(redPesetaPrefab, transform.position + new Vector3(UnityEngine.Random.Range(-0.5f,0.5f), UnityEngine.Random.Range(0f,0.5f), 0), Quaternion.identity);
            Rigidbody2D rigidbody = peseta.GetComponent<Rigidbody2D>();
            rigidbody.linearVelocity += new Vector2(0, 2);
        }
        for(int i = 0; i < pesetaCount; i++)
        {
            GameObject peseta = Instantiate(pesetaPrefab, transform.position + new Vector3(UnityEngine.Random.Range(-0.5f,0.5f), UnityEngine.Random.Range(0f,0.5f), 0), Quaternion.identity);
            Rigidbody2D rigidbody = peseta.GetComponent<Rigidbody2D>();
            rigidbody.linearVelocity += new Vector2(0, 2);
        }
        for(int i = 0; i < silverPesetaCount; i++)
        {
            GameObject peseta = Instantiate(silverPesetaPrefab, transform.position + new Vector3(UnityEngine.Random.Range(-0.5f,0.5f), UnityEngine.Random.Range(0f,0.5f), 0), Quaternion.identity);
            Rigidbody2D rigidbody = peseta.GetComponent<Rigidbody2D>();
            rigidbody.linearVelocity += new Vector2(0, 2);
        }
    }
}
