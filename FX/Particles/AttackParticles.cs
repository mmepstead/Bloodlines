using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class AttackParticles : MonoBehaviour
{
    public ParticleSystem ps;
    void Start()
    {
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);
        // Two parents up is the player transform
        velocity.x = transform.parent.parent.localScale.x < 0 ? new ParticleSystem.MinMaxCurve(1f, 4f) : new ParticleSystem.MinMaxCurve(-1f, -4f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        // StartCoroutine(startAttackMovement());
    }

    public IEnumerator startAttackMovement()
    {
        Vector3 endPoint = ps.gameObject.transform.position + new Vector3(transform.parent.parent.localScale.x < 0 ? 1.3f : 1.3f, 0f, 0f);
        Vector3 startPoint = ps.gameObject.transform.position;
        while(Vector3.Distance(ps.gameObject.transform.position, endPoint) > 0.1f)
        {
            ps.gameObject.transform.position = Vector3.MoveTowards(ps.gameObject.transform.position, endPoint, Time.deltaTime * 12f);
            yield return null;
        }

    }
}