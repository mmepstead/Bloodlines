using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
public class Parallax : MonoBehaviour
{
    public float speedRatio;
    public GameObject player;
    float oldX;
    float newX;
    void Start()
    {
        player = GameObject.Find("Player");
        oldX = player.transform.position.x;
    }
    void Update()
    {
        newX = player.transform.position.x;
        if (oldX != newX)
        {
            gameObject.transform.position += new Vector3(speedRatio * (newX - oldX), 0,0);
        }
        oldX = newX;
    }
}
