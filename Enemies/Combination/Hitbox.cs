using System;
using UnityEngine;
using UnityEngine.Serialization;
public class Hitbox : MonoBehaviour {
    private void Update()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Vector2 currentPos = rb.position;
        Vector2 offset = new Vector2(0f, 0.001f); // adjust as needed
        rb.MovePosition(currentPos + offset);
    }
}
