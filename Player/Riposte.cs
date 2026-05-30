using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
public class Riposte : MonoBehaviour {
    public GameObject riposteHitPrefab;
    void OnTriggerEnter2D(Collider2D collision)
    {
        //Check for a match with the specified name on any GameObject that collides with your GameObject
        // if (collision.gameObject.tag == "Attack")
        // {
        //     GameObject riposteHit = Instantiate(riposteHitPrefab, collision.gameObject.transform.position, Quaternion.identity, transform);
        //     riposteHit.transform.localScale = new Vector3(GameObject.Find("Player").transform.localScale.x == -1 ? -2 : -2, 2, 1);
        //     Destroy(collision.gameObject);
        // }
    }
}
