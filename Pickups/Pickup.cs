using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
public class Pickup : MonoBehaviour 
{
    public string pickupType;
    [Header("Bezier Points")]
    public Transform endPoint;

    [Header("Movement Settings")]
    [Tooltip("Speed multiplier for curve movement")]
    public float speed = 1f;

    [Tooltip("Whether to loop back and forth along the curve")]
    public bool loop = false;

    private float t = 0f;
    private bool reversing = false;
    public GameObject burstEffectPrefab;

    public void pickup()
    {
        switch(pickupType) 
        {
            case "peseta":
                PlayerData.pesetas += gameObject.GetComponent<Peseta>().value;
                Destroy(gameObject);
                break;
            case "sun_medal":
                PlayerData.gainMedal();
                StartCoroutine(pickupAnimation("Sun Medal Acquired!" + (PlayerData.medalCount % 4 == 0 ? "\nMax Health Increased!" : "\n(" + PlayerData.medalCount + "/4)")));
                break;
            case "medallion":
                PlayerData.medallionsCollected += 1;
                StartCoroutine(pickupAnimation("Medallion Acquired! ( " + PlayerData.medallionsCollected + " )"));
                break;
            case "scroll":
                break;
            case "page":
                PlayerData.journalPages.Add(gameObject.name);
                StartCoroutine(pickupAnimation("New Journal Page Acquired!"));
                break;
        }
    }

    IEnumerator pickupAnimation(string pickupText) 
    {
        gameObject.GetComponent<Collider2D>().enabled = false;
        gameObject.GetComponent<BobUpDown>().enabled = false;
        // Turn off rigidbody physics
        gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        gameObject.GetComponent<SpriteRenderer>().sortingLayerName = "UI";
        gameObject.GetComponent<SpriteRenderer>().sortingOrder = 100;
        // Shut off particle system
        gameObject.GetComponent<ParticleSystem>().Stop();
        gameObject.transform.localScale = gameObject.transform.localScale * 1.25f;
        gameObject.transform.parent = GameObject.Find("Main Camera").transform;
        if (endPoint == null)
        {
            yield break;
        }
        // Calculate position along the Bezier curve
        Vector3 startPosition = transform.position;
        GameObject.Find("Pickup Description").GetComponent<SlideInPrefabSpawner>().SpawnAndSlideIn(pickupText);
        gameObject.transform.parent = GameObject.Find("Pickup Description").GetComponent<SlideInPrefabSpawner>().instance.transform;
        while(transform.position.x != endPoint.position.x && transform.position.y != endPoint.position.y)
        {
            float delta = Time.deltaTime * speed;
            t = Mathf.Clamp01(t + delta);
            Vector2 position = CalculateQuadraticBezierPoint(t, startPosition, startPosition + new Vector3(-2f,2f,0), new Vector3(endPoint.position.x, endPoint.position.y, 0));
            transform.position = position;
            yield return null;
        }
        GameObject burst = Instantiate(burstEffectPrefab, transform.position, Quaternion.identity, transform);
        burst.transform.Rotate(90,0,0);

        float maxTime = 1.5f;
        float timer = 0f;
        while(timer < maxTime)
        {
            timer += Time.deltaTime;
            burst.transform.Rotate(0, -(90/maxTime)*Time.deltaTime, 0);
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(GameObject.Find("Pickup Description").GetComponent<SlideInPrefabSpawner>().SlideOut());
        // Destroy(gameObject);
    }

    /// <summary>
    /// Calculates a point on a quadratic Bezier curve given t, start, control, and end points.
    /// </summary>
    Vector2 CalculateQuadraticBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float u = 1 - t;
        return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
    }
}
