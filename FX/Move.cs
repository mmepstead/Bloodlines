using UnityEngine;
using System.Collections;

public class Move : MonoBehaviour
{
    public enum JumpDirection { N, NE, NW }

    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public bool left = true;
    private bool moving = false;
    public bool perpetual = false;

    [Header("Jump Settings")]
    // Horizontal distance covered by a diagonal (NE/NW) jump. Ignored for N (straight up).
    public float jumpHorizontalDistance = 4f;
    // Peak height of the jump arc.
    public float jumpHeight = 1f;

    [Header("Quick Step Settings")]
    // How much extra time (beyond stepDuration) we allow the vertical arc to keep
    // resolving via gravity before we consider a quick step "stuck" and force-land it.
    public float quickStepMaxAirTime = 1f;

    private Rigidbody2D rb;
    private EnemyAI enemyAI; // reuse its groundCheck / groundLayer / ledgeDetectDistance
    private bool isGrounded = true;
    private bool isBusy = false; // true while a quick step or jump is in progress

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
    }

    void Start()
    {

    }

    /// <summary>
    /// Returns true if the Player is to the left of this GameObject (on the negative-x side).
    /// Used to make movement always head towards the player rather than a fixed direction.
    /// </summary>
    public bool IsPlayerToLeft()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            // No player found in the scene; fall back to whatever "left" is currently set to
            // rather than guessing a direction.
            return left;
        }
        return player.transform.position.x < transform.position.x;
    }

    void FixedUpdate()
    {
        if(rb == null)
        {
            return;
        }
        // Keep grounded state up to date every physics step, reusing the same
        // groundCheck/groundLayer/ledgeDetectDistance EnemyAI already exposes.
        if (enemyAI != null && enemyAI.groundCheck != null)
        {
            isGrounded = Physics2D.Raycast(
                enemyAI.groundCheck.position,
                Vector2.down,
                enemyAI.ledgeDetectDistance,
                enemyAI.groundLayer);
        }

        // Dynamic Rigidbody2D: drive normal walking via velocity instead of
        // transform.Translate so it respects collisions, gravity, and doesn't
        // fight the physics engine. Skipped while a quick step / jump is playing
        // out, since those control velocity directly.
        if (isBusy) return;

        if (moving || perpetual)
        {
            Debug.Log("Moving");
            // Always walk towards the player rather than a fixed direction.
            left = IsPlayerToLeft();
            float direction = left ? -1f : 1f;
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);
        }
    }

    public void startMoving(){
        moving = true;
    }

    public void stopMoving(){
        Debug.Log("Stop moving");
        moving = false;
    }

    public void QuickStepEvent()
    {
        int direction = IsPlayerToLeft() ? -1 : 1;
        QuickStep(direction, 5f, 0.05f, 0.4f); // Example: Quick step towards the player with max speed 2, vertical hop of 0.5, duration 0.2 seconds
    }

    /// <summary>
    /// Performs a quick step movement in the specified direction with a slight vertical hop.
    /// Uses the Rigidbody2D's own physics (gravity) to resolve the vertical arc, rather than
    /// kinematically lerping position, so it plays nicely with collisions on a dynamic body.
    /// </summary>
    /// <param name="direction">Direction to step: -1 for left, 1 for right</param>
    /// <param name="maxSpeed">Maximum horizontal speed for the step</param>
    /// <param name="verticalMovement">Peak height of the hop</param>
    /// <param name="stepDuration">Duration of the horizontal push in seconds</param>
    public void QuickStep(int direction, float maxSpeed, float verticalMovement, float stepDuration = 0.2f)
    {
        StartCoroutine(QuickStepCoroutine(direction, maxSpeed, verticalMovement, stepDuration));
    }

    private IEnumerator QuickStepCoroutine(int direction, float maxSpeed, float verticalMovement, float stepDuration)
    {
        isBusy = true;
        isGrounded = false;

        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        // Launch velocity needed to reach the requested peak height under gravity.
        float launchVelocity = verticalMovement > 0f ? Mathf.Sqrt(2f * gravity * verticalMovement) : 0f;

        rb.linearVelocity = new Vector2(direction * maxSpeed, launchVelocity);

        // Push horizontally for stepDuration, letting gravity handle the vertical arc.
        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            rb.linearVelocity = new Vector2(direction * maxSpeed, rb.linearVelocity.y);
            yield return null;
        }

        // Horizontal push is done; keep whatever vertical velocity gravity has produced
        // so the enemy continues to fall/land naturally.
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return WaitUntilGrounded(quickStepMaxAirTime);

        isBusy = false;
    }

    public void JumpBack()
    {
        bool facingRight = gameObject.GetComponent<EnemyAI>().facingRight;
        Jump(facingRight ? JumpDirection.NW : JumpDirection.NE);
    }

    /// <summary>
    /// Makes the enemy jump N (straight up), NE (up-right), or NW (up-left) using
    /// standard projectile physics, then falls back to the ground under gravity.
    /// Distance/height are tuned via jumpHorizontalDistance and jumpHeight.
    /// </summary>
    public void Jump(JumpDirection direction)
    {
        if (!isGrounded || isBusy) return;
        StartCoroutine(JumpCoroutine(direction));
    }

    private IEnumerator JumpCoroutine(JumpDirection direction)
    {
        isBusy = true;
        isGrounded = false;

        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        float vy = Mathf.Sqrt(2f * gravity * jumpHeight);

        float vx = 0f;
        if (direction != JumpDirection.N)
        {
            // Time to complete the full arc (up and back down to the same height),
            // used to derive the horizontal speed needed to cover jumpHorizontalDistance.
            float airTime = 2f * vy / gravity;
            vx = jumpHorizontalDistance / airTime;
            if (direction == JumpDirection.NW)
                vx = -vx;
        }
        rb.linearVelocity = new Vector2(vx, vy);

        // Wait until we've actually left the ground before polling for landing,
        // so the ground check doesn't immediately trigger on the same frame.
        yield return new WaitForFixedUpdate();
        yield return WaitUntilGrounded(3f);

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isBusy = false;
    }

    public bool IsBusy
    {
        get { return isBusy; }
    }

    /// <summary>
    /// Waits until the ground check reports grounded, or bails out after maxWait
    /// seconds so a bad ground layer / missing groundCheck can't stall the enemy forever.
    /// </summary>
    private IEnumerator WaitUntilGrounded(float maxWait)
    {
        float elapsed = 0f;

        // isGrounded uses ledgeDetectDistance (0.5), which is generous enough
        // that right after launch we're still "grounded" while still rising.
        // Don't start trusting it until we've crested the arc and are falling.
        while (rb.linearVelocity.y > 0f && elapsed < maxWait)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        while (!isGrounded && elapsed < maxWait)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}