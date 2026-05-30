using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CircleFade : MonoBehaviour {
    private float speed = 1f;
    bool fadingOut = false;
    bool fadingIn = false;
    public SpriteRenderer renderer;

    void Start()
    {
        // fadeIn(10);
    }
    void Update()
    {
        if(fadingOut)
        {
            if(transform.localScale.x > 0)
            {
                transform.localScale -= new Vector3(0.025f * speed, 0.025f * speed, 0);
            }
            else
            {
                fadingOut = false;
            }
        }
        if(fadingIn)
        {
            if(transform.localScale.x < 25)
            {
                transform.localScale += new Vector3(0.025f * speed, 0.025f * speed, 0);
            }
            else
            {
                fadingIn = false;
                // Black screen is fully faded in, make sure it's fully transparent and offscreen
                Color colour = renderer.color;
                renderer.color = new Color(colour.r, colour.g, colour.b, 0);
                transform.localScale = new Vector3(0, 0, 1);
            }
            
        }
    }

    public void fadeOut(float speed = 1) 
    {
        // Make sure we've got a black screen to fade out from
        Color colour = renderer.color;
        renderer.color = new Color(colour.r, colour.g, colour.b, 1);
        this.transform.localScale = new Vector3(25, 25, 1);
        this.transform.position = GameObject.Find("Player").transform.position;
        this.speed = speed;
        fadingOut = true;
        fadingIn = false;
    }

    public void fadeIn(float speed = 1) 
    {
        this.transform.position = GameObject.Find("Player").transform.position;
        this.speed = speed;
        fadingIn = true;
        fadingOut = false;
    }
}
