using System;
using UnityEngine;
using UnityEngine.Serialization;
public class BulletTime : MonoBehaviour {
    public float timer = 0;
    public float maxTime = 1;
    private void Update()
    {
        if(Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
        {
            bulletTime();
        }
        if(Input.GetMouseButtonUp(0))
        {
            Time.timeScale = 1;
            timer = 0;
        }
        if(timer >= maxTime)
        {
            Time.timeScale = 1;
        }
    }
    void bulletTime()
    {
        Time.timeScale = 0.3f;
        timer += Time.deltaTime;
    }
}
