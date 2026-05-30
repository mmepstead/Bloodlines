using System;
using UnityEngine;
using UnityEngine.Serialization;
public class Stake : MonoBehaviour {
    public int damage = 2;
    public GameObject coreBreakPrefab;
    static readonly int coreBroken = Animator.StringToHash("Broken");
    public Rigidbody2D rigidBody;
    public Vector3 startingPoint;
    void Awake()
    {
        int speed = GameObject.Find("Player").transform.localScale.x > 0 ? 7 : -7;
        if(speed < 0) {
            transform.localScale = new Vector3(-0.5f,0.5f,1);
        }
        rigidBody.linearVelocity = new Vector3(speed,0,0);
        startingPoint = transform.position;
    }
    void Update()
    {
        if((transform.position - startingPoint).magnitude > 10)
        {
            Destroy(gameObject);
        }
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        //Check for a match with the specific tag on any GameObject that collides with your GameObject
        if (collision.gameObject.tag == "Enemy")
        {
            Combo combo = GameObject.Find("Combo").GetComponent<Combo>();
            combo.extendCombo();
            //If the GameObject has the same tag as specified, output this message in the console
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            enemy.impact(damage, collision.gameObject.transform.position);
            Destroy(gameObject);
        }
    }
}
