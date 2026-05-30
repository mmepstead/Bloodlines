using UnityEngine;

public class WormMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float moveRange = 5f;

    [Header("Pause Settings")]
    public float minPauseTime = 0.5f;
    public float maxPauseTime = 2f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float leftBound;
    private float rightBound;
    private float pauseTimer = 0f;
    private bool isPaused = false;
    private int direction = 1;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Set movement bounds relative to starting position
        leftBound = transform.position.x - moveRange / 2f;
        rightBound = transform.position.x + moveRange / 2f;
    }

    void Update()
    {
        if (isPaused)
        {
            HandlePause();
        }
        else
        {
            Move();
        }
    }

    void Move()
    {
        // Move along x-axis
        transform.Translate(Vector2.right * direction * moveSpeed * Time.deltaTime);

        // Flip sprite to face movement direction
        spriteRenderer.flipX = direction == -1;

        // Play walk animation
        if (animator != null)
            animator.speed = 1f;

        // Check if worm has reached a bound
        if (transform.position.x >= rightBound)
        {
            transform.position = new Vector3(rightBound, transform.position.y, transform.position.z);
            direction = -1;
            TriggerPause();
        }
        else if (transform.position.x <= leftBound)
        {
            transform.position = new Vector3(leftBound, transform.position.y, transform.position.z);
            direction = 1;
            TriggerPause();
        }
        else if (Random.value < 0.001f) // Small random chance to pause mid-path
        {
            TriggerPause();
        }
    }

    void TriggerPause()
    {
        isPaused = true;
        pauseTimer = Random.Range(minPauseTime, maxPauseTime);

        // Freeze animation
        if (animator != null)
            animator.speed = 0f;
    }

    void HandlePause()
    {
        pauseTimer -= Time.deltaTime;

        if (pauseTimer <= 0f)
        {
            isPaused = false;
        }
    }
}