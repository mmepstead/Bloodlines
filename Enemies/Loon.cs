using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Loon : MonoBehaviour
{
    public GameObject featherPrefab;
    public GameObject featherParticlePrefab;
    public void ThrowFeather()
    {
        // Implement the logic for throwing a feather here
        Instantiate(featherPrefab, transform.position+ new Vector3(GetComponent<EnemyAI>().facingRight ? 1f : -1f, 0, 0), transform.rotation);
    }

    public void PuffFeathers()
    {
        // Implement the logic for throwing a feather here
        Instantiate(featherParticlePrefab, transform.position+ new Vector3(0, 0, 0), transform.rotation);
    }
} 