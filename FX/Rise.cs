using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
public class Rise : MonoBehaviour {
    public float riseSpeed = 1f;
    void Update()
    {
        transform.position += new Vector3(0, riseSpeed * Time.deltaTime, 0);
    }
}
