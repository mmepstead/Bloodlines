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
    [Tooltip("Prefab for the warning line sprite placed at each edge of where the goal will land.")]
    public GameObject warningLinePrefab;

    [Tooltip("How far each warning line is offset from the exact next goal position. " +
             "0 = perfectly accurate, larger = wider bracket.")]
    public float warningRoughness = 0.1f;

    // The two live warning indicator instances
    GameObject warningLeft;
    GameObject warningRight;

    // Pre-rolled destination for the next goal move
    float nextGoalX;

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

        // Pre-roll and show warnings for the first round
        nextGoalX = RollNextGoalX();
        SpawnWarnings();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            DestroyWarnings();

            if (inGoal)
            {
                movingRight = !movingRight;
                currentRound += 1;

                if (currentRound >= roundsToWin)
                {
                    GameObject.Find("Player").GetComponent<Movement>().endHardParry(true);
                    return;
                }

                // Move the goal to the already-decided position
                goal.transform.localScale = new Vector3(
                    goal.transform.localScale.x - goal.transform.localScale.x * sizeDecrease,
                    goal.transform.localScale.y,
                    goal.transform.localScale.z);

                Instantiate(goalHitSparksPrefab, goal.transform.position, Quaternion.identity);
                goal.transform.position = new Vector2(nextGoalX, goal.transform.position.y);

                // Pre-roll the NEXT destination and show its warnings
                nextGoalX = RollNextGoalX();
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
            }
            else
            {
                speed = 0.8f;
            }
        }
    }

    /// <summary>
    /// Rolls the random X position the goal will move to on the NEXT successful press,
    /// mirroring the original range logic but for the direction after the next flip.
    /// </summary>
    float RollNextGoalX()
    {
        // After the next successful press, movingRight will flip to !movingRight
        bool nextMovingRight = !movingRight;

        if (nextMovingRight)
        {
            return UnityEngine.Random.Range(
                Math.Min(transform.position.x + baseDistance, startX + goalRange),
                Math.Max(transform.position.x + baseDistance, startX + goalRange));
        }
        else
        {
            return UnityEngine.Random.Range(
                Math.Min(startX - goalRange, transform.position.x - baseDistance),
                Math.Max(startX - goalRange, transform.position.x - baseDistance));
        }
    }

    /// <summary>
    /// Spawns two warning lines offset by warningRoughness around the exact pre-rolled goal position.
    /// </summary>
    void SpawnWarnings()
    {
        if (warningLinePrefab == null || goal == null) return;

        float warningY = goal.transform.position.y;
        Vector3 posLeft  = new Vector3(nextGoalX - warningRoughness, warningY, 0f);
        Vector3 posRight = new Vector3(nextGoalX + warningRoughness, warningY, 0f);

        warningLeft  = Instantiate(warningLinePrefab, posLeft,  Quaternion.identity);
        warningRight = Instantiate(warningLinePrefab, posRight, Quaternion.identity);

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