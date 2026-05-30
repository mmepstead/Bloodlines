using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
public class Peseta : MonoBehaviour {
    public Rigidbody2D rigidbody;
    System.Random rnd = new System.Random();
    public int value = 1;
    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.linearVelocity = new Vector2(rnd.Next(-1,1), rigidbody.linearVelocity.y);
    }
    void FixedUpdate()
    {
        // Slow peseta over time until it stops
        rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x * 0.9f, rigidbody.linearVelocity.y);
    }
}
