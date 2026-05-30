using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Fade : MonoBehaviour {
    public float opacity = 1;
    bool fadingOut = false;
    bool fadingIn = false;
    public SpriteRenderer renderer;

    void Start()
    {
        fadeIn();
    }
    void Update()
    {
        if(fadingOut)
        {
            if(opacity < 1)
            {
                opacity += 0.025f;
            }
            else
            {
                fadingOut = false;
            }
            Color colour = renderer.color;
            renderer.color = new Color(colour.r, colour.g, colour.b, opacity);
        }
        if(fadingIn)
        {
            if(opacity > 0)
            {
                opacity -= 0.025f;
            }
            else
            {
                fadingIn = false;
            }
            Color colour = renderer.color;
            renderer.color = new Color(colour.r, colour.g, colour.b, opacity);
        }
    }

    public void fadeOut() 
    {
        fadingOut = true;
        fadingIn = false;
    }

    public void fadeIn() 
    {
        fadingIn = true;
        fadingOut = false;
    }

}
