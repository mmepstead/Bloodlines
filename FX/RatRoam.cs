using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class RatRoamer : MonoBehaviour
{
    public float roamDistance = 5f;
    public float walkSpeed = 1.5f;
    public float stateChangeIntervalMin = 2f;
    public float stateChangeIntervalMax = 5f;

    private enum RatState { Idle, Walking, Scouting }
    private RatState currentState;

    private Vector2 originPosition;
    private float stateTimer;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float walkDirection = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        originPosition = transform.position;
        PickNewState();
    }

    void Update()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            PickNewState();
        }

        switch (currentState)
        {
            case RatState.Idle:
                StopMovement();
                animator.Play("Idle");
                break;

            case RatState.Walking:
                Walk();
                animator.Play("Walking");
                break;

            case RatState.Scouting:
                StopMovement();
                animator.Play("Scouting");
                break;
        }
    }

    void Walk()
    {
        Vector2 pos = transform.position;

        // Reverse direction if at roam bounds
        if (pos.x < originPosition.x - roamDistance)
            walkDirection = 1f;
        else if (pos.x > originPosition.x + roamDistance)
            walkDirection = -1f;

        rb.linearVelocity = new Vector2(walkDirection * walkSpeed, 0f);
        spriteRenderer.flipX = walkDirection < 0;
    }

    void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
    }

    void PickNewState()
    {
        // Randomly choose one of the three states
        int r = Random.Range(0, 3);
        currentState = (RatState)r;

        // Random duration before changing state again
        stateTimer = Random.Range(stateChangeIntervalMin, stateChangeIntervalMax);

        // Randomize walk direction if entering Walking state
        if (currentState == RatState.Walking)
        {
            walkDirection = Random.value < 0.5f ? -1f : 1f;
        }
    }
}
