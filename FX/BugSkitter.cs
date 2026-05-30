using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class BugSkitter : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float moveDuration = 0.2f;
    public float waitTimeMin = 0.5f;
    public float waitTimeMax = 1.5f;
    public float roamRadius = 3f;

    [Header("Facing Settings")]
    bool faceWithFlip = false; // If true, flips instead of rotating

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 startPosition;
    private float waitTimer;
    private float moveTimer;
    private Vector2 moveDirection;
    private bool isMoving;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        waitTimer = Random.Range(waitTimeMin, waitTimeMax);
    }

    void Update()
    {
        if (isMoving)
        {
            moveTimer -= Time.deltaTime;
            if (moveTimer <= 0f)
            {
                StopMoving();
            }
        }
        else
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                TryStartMove();
            }
        }
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            Vector2 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
    }

    void TryStartMove()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector2 potentialTarget = rb.position + randomDir * moveSpeed * moveDuration;

        if (Vector2.Distance(startPosition, potentialTarget) <= roamRadius)
        {
            moveDirection = randomDir;
        }
        else
        {
            // Reflect direction back toward center
            moveDirection = (startPosition - rb.position).normalized;
        }

        // Face the direction of movement
        FaceDirection(moveDirection);

        moveTimer = moveDuration;
        isMoving = true;
        animator.speed = 1f;
        animator.Play("Bug");
    }

    void StopMoving()
    {
        isMoving = false;
        waitTimer = Random.Range(waitTimeMin, waitTimeMax);
        moveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        animator.Play("Bug", 0, 0f); // Restart animation at frame 0
        animator.speed = 0f;         // Freeze to idle pose
    }

    void FaceDirection(Vector2 dir)
    {
        if (faceWithFlip)
        {
            if (dir.x != 0)
                spriteRenderer.flipX = dir.x < 0;
        }
        else
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}