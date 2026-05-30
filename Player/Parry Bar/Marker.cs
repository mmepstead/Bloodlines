using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;

public class Marker : MonoBehaviour
{
    bool inGoal = false;
    bool movingRight = true;
    public int roundsToWin = 3;
    public int currentRound = 0;
    float speed = 1f;
    float leftLimit = -0.65f;
    float rightLimit = 0.65f;
    float startX;
    float goalRange = 0.5f;
    float baseDistance = 0.3f;
    public float sizeDecrease = 0.1f;
    public GameObject goalHitSparksPrefab;
    GameObject goal;

    [Header("Warning Indicators")]
    [Tooltip("Prefab for the warning line sprite shown before the goal moves.")]
    public GameObject warningLinePrefab;

    [Tooltip("How far off the predicted center each warning line is placed. " +
             "Larger values = wider/rougher estimate shown to the player.")]
    public float warningRoughness = 0.1f;

    // The two live warning indicator instances
    GameObject warningLeft;
    GameObject warningRight;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Goal")
            inGoal = true;
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Goal")
            inGoal = false;
    }

    void Start()
    {
        startX = transform.position.x + 0.65f;
        goal = GameObject.FindGameObjectWithTag("Goal");

        // Spawn initial warnings for the very first incoming position
        SpawnWarnings();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            // Always destroy warnings on button press (hit or miss)
            DestroyWarnings();

            if (inGoal)
            {
                movingRight = !movingRight;
                float newX;
                currentRound += 1;

                if (currentRound >= roundsToWin)
                {
                    GameObject.Find("Player").GetComponent<Movement>().endHardParry(true);
                    return;
                }

                if (movingRight)
                {
                    newX = UnityEngine.Random.Range(
                        Math.Min(transform.position.x + baseDistance, startX + goalRange),
                        Math.Max(transform.position.x + baseDistance, startX + goalRange));
                }
                else
                {
                    newX = UnityEngine.Random.Range(
                        Math.Min(startX - goalRange, transform.position.x - baseDistance),
                        Math.Max(startX - goalRange, transform.position.x - baseDistance));
                }

                goal.transform.localScale = new Vector3(
                    goal.transform.localScale.x - goal.transform.localScale.x * sizeDecrease,
                    goal.transform.localScale.y,
                    goal.transform.localScale.z);

                Instantiate(goalHitSparksPrefab, goal.transform.position, Quaternion.identity);
                goal.transform.position = new Vector2(newX, goal.transform.position.y);

                // Spawn fresh warnings for the next incoming position
                SpawnWarnings();
            }
        }

        // Move left or right until given limit
        if (movingRight)
        {
            transform.Translate(Vector2.right * speed * Time.deltaTime);
            if (transform.position.x >= startX + rightLimit)
            {
                transform.position = new Vector2(startX + rightLimit, transform.position.y);
                GameObject.Find("Player").GetComponent<Movement>().endHardParry(false);
                DestroyWarnings();
            }
            else
            {
                speed = 0.8f;
            }
        }
        else
        {
            transform.Translate(Vector2.left * speed * Time.deltaTime);
            if (transform.position.x <= startX + leftLimit)
            {
                transform.position = new Vector2(startX + leftLimit, transform.position.y);
                GameObject.Find("Player").GetComponent<Movement>().endHardParry(false);
                DestroyWarnings();
            }
            else
            {
                speed = 0.8f;
            }
        }
    }

    /// <summary>
    /// Calculates the midpoint of the range where the goal will land on the NEXT
    /// press (based on the current marker position and movement direction), then
    /// spawns two warning lines offset by warningRoughness on either side of it.
    /// </summary>
    void SpawnWarnings()
    {
        if (warningLinePrefab == null || goal == null) return;

        // Predict where the goal's center will be on the next successful press.
        // We mirror the current move logic: next press flips direction, so the
        // next goal side is the OPPOSITE of movingRight.
        bool nextMovingRight = !movingRight;
        float predictedMin, predictedMax;

        // Use the current marker position as an approximation of where it will be
        // when W is pressed. This is intentionally imprecise — warningRoughness
        // adds further visual fuzziness on top.
        float approxMarkerX = transform.position.x;

        if (nextMovingRight)
        {
            predictedMin = Math.Min(approxMarkerX + baseDistance, startX + goalRange);
            predictedMax = Math.Max(approxMarkerX + baseDistance, startX + goalRange);
        }
        else
        {
            predictedMin = Math.Min(startX - goalRange, approxMarkerX - baseDistance);
            predictedMax = Math.Max(startX - goalRange, approxMarkerX - baseDistance);
        }

        float predictedCenter = (predictedMin + predictedMax) * 0.5f;
        float warningY = goal.transform.position.y;

        Vector3 posLeft  = new Vector3(predictedCenter - warningRoughness, warningY, 0f);
        Vector3 posRight = new Vector3(predictedCenter + warningRoughness, warningY, 0f);

        warningLeft  = Instantiate(warningLinePrefab, posLeft,  Quaternion.identity);
        warningRight = Instantiate(warningLinePrefab, posRight, Quaternion.identity);

        // Wire up the shake scripts
        SetupShake(warningLeft,  posLeft);
        SetupShake(warningRight, posRight);
    }

    void SetupShake(GameObject warning, Vector3 basePos)
    {
        WarningShake shake = warning.GetComponent<WarningShake>();
        if (shake == null)
            shake = warning.AddComponent<WarningShake>();

        shake.marker = transform;
        shake.goal   = goal.transform;
        shake.SetBasePosition(basePos);
    }

    void DestroyWarnings()
    {
        if (warningLeft  != null) Destroy(warningLeft);
        if (warningRight != null) Destroy(warningRight);
        warningLeft  = null;
        warningRight = null;
    }
}