using System;
using UnityEngine;
using UnityEngine.Serialization;
public class PointToMouse : MonoBehaviour {
    private GameObject player;
    public bool onAwake = false;

    public void Awake()
    {
        player = GameObject.Find("Player");
        pointTowardsMouse();
    }
    private void Update()
    {
        if(!onAwake)
        {
            pointTowardsMouse();
        }
    }
    void pointTowardsMouse()
    {
        bool flip = player.transform.localScale.x < 0;
        if(flip)
        {
            transform.localScale = new Vector3( transform.localScale.x < 0 ? transform.localScale.x : -1*transform.localScale.x, transform.localScale.y, 1);
        }
        else
        {
            transform.localScale = new Vector3( transform.localScale.x > 0 ? transform.localScale.x : -1*transform.localScale.x, transform.localScale.y, 1);
        }
        
        Vector2 mouse = Input.mousePosition;
        Vector2 screenPos = Camera.main.ScreenToWorldPoint(mouse);
        if((screenPos.x <= player.transform.position.x && player.transform.localScale.x < 0) || (screenPos.x >= player.transform.position.x && player.transform.localScale.x > 0))
        {
            Vector2 lookAt = screenPos - new Vector2(transform.position.x, transform.position.y);
            float angle = Mathf.Atan2(lookAt.y, lookAt.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0,0,angle);
        }

    }
}
