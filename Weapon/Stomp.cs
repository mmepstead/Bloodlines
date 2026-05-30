using System;
using UnityEngine;
using UnityEngine.Serialization;
public class Stomp : MonoBehaviour
{
    
    public int damage = 5;
    public GameObject coreBreakPrefab;
    static readonly int coreBroken = Animator.StringToHash("Broken");
    void OnTriggerEnter2D(Collider2D collision)
    {
        //Check for a match with the specified name on any GameObject that collides with your GameObject
        // if (collision.gameObject.tag == "Core")
        // {
        //     Enemy enemy = collision.gameObject.transform.parent.parent.gameObject.GetComponent<Enemy>();
        //     // collision.gameObject.GetComponent<Animator>().SetBool(coreBroken, true);
        //     Instantiate(coreBreakPrefab, collision.gameObject.transform.position, Quaternion.identity, collision.gameObject.transform);
        //     Destroy(collision.gameObject.GetComponent<Animator>());
        //     Destroy(collision.gameObject.GetComponent<SpriteRenderer>());
        //     enemy.health -= 10;
        //     Destroy(gameObject);
        //     // Destroy(collision.gameObject.transform.parent.parent.gameObject);
        // }

        //Check for a match with the specific tag on any GameObject that collides with your GameObject
        if (collision.gameObject.tag == "Enemy")
        {
            //If the GameObject has the same tag as specified, output this message in the console
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            enemy.impact(damage, collision.gameObject.transform.position);
        }
    }
}