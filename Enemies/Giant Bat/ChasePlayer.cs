using UnityEngine;
using UnityEngine.UI;
using System;
public class ChasePlayer : MonoBehaviour
{

    public Transform Player;
    public Rigidbody2D rigidBody;
    int maxVelocity = 2;
    int MaxDist = 5;
    int MinDist = 0;




    void Start()
    {

    }

    void Update()
    {
        if(Mathf.Abs(Player.position.x - transform.position.x) < MaxDist)
        {
            if(Player.position.x > transform.position.x)
            {
                transform.localScale = new Vector3(-1*Math.Abs(transform.localScale.x),transform.localScale.y,1);
            }
            else
            {
                transform.localScale = new Vector3(transform.localScale.x,transform.localScale.y,1);
            }

            if (Vector3.Distance(transform.position, Player.position) >= MinDist && rigidBody)
            {
                rigidBody.AddForce((Player.position - transform.position).normalized);
                rigidBody.linearVelocity = Vector2.ClampMagnitude(rigidBody.linearVelocity, maxVelocity);
                //  transform.position = Vector2.MoveTowards(transform.position, Player.position, MoveSpeed * Time.deltaTime);
            }
        }

    }
}