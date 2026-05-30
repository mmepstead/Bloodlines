using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private enum State { Idle, Combat }
    private State currentState = State.Idle;
    [SerializeField] private Animator animator;
    public EnemyAudioManager audioManager;
    public GameObject alertPrefab;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float idleTimeMin = 1f;
    public float idleTimeMax = 3f;
    private float stateTimer;
    private bool isWalking;
    static readonly int walking = Animator.StringToHash("Walking");
    static readonly int isHardParrying = Animator.StringToHash("Hard Parrying");
    private bool facingRight = true;
    public Vector3 hardParryPositionOffset;

    [Header("Detection Settings")]
    public float ledgeDetectDistance = 0.5f;
    public float wallDetectDistance = 0.3f;
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;
    public Transform player;
    public float detectionRange = 5f;
    public float combatDetectionRange = 10f;
    // Proximity radius for rear/side detection — enemy aggros if player is within
    // this distance regardless of facing direction. Set in Inspector.
    public float proximityAggroRange = 2.5f;
    private float audioRange = 5f;
    public float attackRange = 1.5f;
    public bool attacking = false;
    public bool hardParrying = false;

    [Header("Combat Settings")]
    public float chaseSpeed = 2.5f;
    // How long the enemy will keep chasing after losing line-of-sight before
    // giving up and returning to idle. Prevents instant de-aggro.
    public float lostSightTimeout = 3f;
    private float lostSightTimer = 0f;

    // Cached component references — avoids repeated GetComponent calls every frame
    private Rigidbody2D rb;
    private Enemy enemy;
    private EnemyCombat enemyCombat;
    private Shield shield;

    private void Start()
    {
        rb          = GetComponent<Rigidbody2D>();
        enemy       = GetComponent<Enemy>();
        enemyCombat = GetComponent<EnemyCombat>();
        shield      = GetComponent<Shield>();

        SetIdleState();
    }

    private void Update()
    {
        if (!enemy.dying && !PauseMenu.IsPaused && !hardParrying)
        {
            switch (currentState)
            {
                case State.Idle:
                    HandleIdleState();
                    DetectStateChange();
                    break;
                case State.Combat:
                    HandleCombatState();
                    break;
            }
        }
    }

    // ----------------------------
    // State Handlers
    // ----------------------------

    void HandleIdleState()
    {
        // If something set attacking externally (e.g. damage aggro), enter combat
        if (attacking)
        {
            SetCombatState();
            return;
        }

        stateTimer -= Time.deltaTime;

        if (isWalking)
        {
            animator.SetBool(walking, true);
            rb.linearVelocity = new Vector2(
                (facingRight ? 1 : -1) * walkSpeed * ShieldBrokenSlowDown(),
                rb.linearVelocity.y);

            if (IsAtEdgeOrWall())
                Flip();
        }
        else
        {
            if (animator.GetBool(walking))
                audioManager.getInstance().StopWalk();

            animator.SetBool(walking, false);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        if (stateTimer <= 0f)
            SetIdleState();
    }

    void HandleCombatState()
    {
        bool canSeePlayer = DetectPlayer();

        // Lost-sight grace period: don't immediately de-aggro if LOS is briefly broken
        if (!canSeePlayer)
        {
            lostSightTimer -= Time.deltaTime;
            if (lostSightTimer <= 0f)
            {
                // Truly lost the player — bail out of any current attack and go idle
                SafeResetAttacking();
                SetIdleState();
                return;
            }
            // Still within grace period: keep the last known behaviour but don't advance
            return;
        }
        else
        {
            // Reset the timer whenever we can see the player
            lostSightTimer = lostSightTimeout;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool isStaggering = animator.GetCurrentAnimatorStateInfo(0).IsName("Stagger");

        if (distanceToPlayer > attackRange && !attacking && !isStaggering)
        {
            // Chase
            Vector2 direction = (player.position - transform.position).normalized;
            if ((direction.x > 0 && !facingRight) || (direction.x < 0 && facingRight))
                Flip();

            if (!IsAtEdgeOrWall())
            {
                animator.SetBool(walking, true);
                rb.linearVelocity = new Vector2(
                    direction.x * chaseSpeed * ShieldBrokenSlowDown(),
                    rb.linearVelocity.y);
            }
            else
            {
                animator.SetBool(walking, false);
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
        else if (!attacking && !isStaggering)
        {
            // In range — attack
            rb.linearVelocity = Vector2.zero;
            animator.SetBool(walking, false);
            Attack();
        }
    }

    // ----------------------------
    // Transitions
    // ----------------------------

    void DetectStateChange()
    {
        // Only called while idle (see Update switch)
        if (DetectPlayer())
        {
            if (transform.Find("Alert(Clone)") == null)
                Instantiate(alertPrefab, transform.position + new Vector3(0, 0.6f, 0), Quaternion.identity, transform);

            audioManager.getInstance().PlayAlert();
            SetCombatState();
        }
    }

    /// <summary>
    /// Call this when the enemy takes damage from any source to instantly aggro,
    /// even from behind or outside normal detection range.
    /// Intended to be called by your damage/health script.
    /// </summary>
    public void AggroFromDamage()
    {
        if (currentState == State.Combat) return; // Already aggroed

        // Face the player before entering combat
        float dx = player.position.x - transform.position.x;
        if ((dx > 0 && !facingRight) || (dx < 0 && facingRight))
            Flip();

        if (transform.Find("Alert(Clone)") == null)
            Instantiate(alertPrefab, transform.position + new Vector3(0, 0.6f, 0), Quaternion.identity, transform);

        audioManager.getInstance().PlayAlert();
        SetCombatState();
    }

    /// <summary>
    /// Front arc raycasts for line-of-sight detection, plus an omnidirectional
    /// proximity check so the player can't sneak directly beside or behind the enemy.
    /// </summary>
    bool DetectPlayer(int rayCount = 5, float angleSpread = 30f)
    {
        // --- Proximity check (all directions) ---
        float xDist = Mathf.Abs(player.position.x - transform.position.x);
        if (xDist <= proximityAggroRange)
        {
            // Make sure no solid ground is directly between them
            RaycastHit2D proximityGround = Physics2D.Linecast(
                transform.position,
                player.position,
                LayerMask.GetMask("Ground"));

            if (proximityGround.collider == null)
                return true;
        }

        // --- Directional arc raycast (front) ---
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        float startAngle = -angleSpread / 2f;
        float angleStep  = angleSpread / (rayCount - 1);

        for (int i = 0; i < rayCount; i++)
        {
            float angle       = startAngle + (angleStep * i);
            Vector2 rayDir    = Rotate(direction, angle);

            RaycastHit2D hit  = Physics2D.Raycast(
                transform.position,
                rayDir,
                combatDetectionRange,
                LayerMask.GetMask("Player"));

            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                RaycastHit2D groundHit = Physics2D.Linecast(
                    transform.position,
                    hit.point,
                    LayerMask.GetMask("Ground"));

                if (groundHit.collider == null)
                    return true;
            }
        }

        return false;
    }

    Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin     = Mathf.Sin(radians);
        float cos     = Mathf.Cos(radians);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    // ----------------------------
    // Hard Parry
    // ----------------------------

    public void startHardParry()
    {
        hardParrying = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        GameObject playerObj = GameObject.FindWithTag("Player");
        gameObject.transform.position = playerObj.transform.position +
            (playerObj.transform.localScale.x < 0
                ? (hardParryPositionOffset - new Vector3(2 * hardParryPositionOffset.x, 0, 0))
                : hardParryPositionOffset);
        animator.SetBool(isHardParrying, true);
    }

    public void endHardParry()
    {
        hardParrying = false;
        animator.SetBool(isHardParrying, false);
    }

    // ----------------------------
    // Utility
    // ----------------------------

    /// <summary>
    /// Safely clears the attacking flag and notifies EnemyCombat.
    /// Use this instead of setting attacking = false directly when an external
    /// system (death, stun, scene transition) needs to abort combat.
    /// </summary>
    public void SafeResetAttacking()
    {
        if (!attacking) return;
        attacking = false;
        enemyCombat.InterruptCombo();
    }

    float ShieldBrokenSlowDown()
    {
        return shield.health == 0 ? 0.75f : 1f;
    }

    void SetIdleState()
    {
        currentState = State.Idle;
        stateTimer   = Random.Range(idleTimeMin, idleTimeMax);
        isWalking    = Random.value > 0.5f;
    }

    void playWalk()
    {
        if (Vector2.Distance(transform.position, player.position) < audioRange)
            audioManager.getInstance().PlayWalk();
    }

    void SetCombatState()
    {
        currentState   = State.Combat;
        lostSightTimer = lostSightTimeout; // Start with a full grace period
    }

    bool IsAtEdgeOrWall()
    {
        bool noGround = !Physics2D.Raycast(groundCheck.position, Vector2.down, ledgeDetectDistance, groundLayer);
        bool hitWall  =  Physics2D.Raycast(wallCheck.position, facingRight ? Vector2.right : Vector2.left, wallDetectDistance, groundLayer);
        return noGround || hitWall;
    }

    void Flip()
    {
        facingRight = !facingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    void Attack()
    {
        if (attacking) return;
        attacking = true;
        enemyCombat.TriggerRandomAttackCombo();
    }
}