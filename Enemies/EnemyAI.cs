using System;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private enum State { Idle, Combat }
    private State currentState = State.Idle;

    // Fired the moment this enemy enters combat (natural detection OR AggroFromDamage).
    // Subscribe to this to show a health bar, kick off boss music, etc.
    public event Action OnAggro;

    // Fired when this enemy returns to Idle from Combat (only reachable if canDeaggro is true).
    public event Action OnDeaggro;
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
    public bool facingRight = true;
    public Vector3 hardParryPositionOffset;
    private Move move;

    [Header("Bounds Settings")]
    [Tooltip("If true, the enemy never moves further than maxDistanceFromStart (x-axis) from its spawn position.")]
    public bool useBounds = true;
    public float maxDistanceFromStart = 5f;
    private float startX;

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
    public bool attacking = false;
    public bool hardParrying = false;

    [Header("Combat Settings")]
    public float chaseSpeed = 2.5f;
    // Minimum distance in the x direction — enemy is guaranteed to attack
    // when within this distance regardless of aggression
    public float minimumAttackDistance = 1f;
    // How long the enemy will keep chasing after losing line-of-sight before
    // giving up and returning to idle. Prevents instant de-aggro.
    public float lostSightTimeout = 3f;
    private float lostSightTimer = 0f;
    // Controls whether the enemy can de-aggro and return to idle state.
    // If false, the enemy remains in combat once entered.
    public bool canDeaggro = true;
    public bool noWalk = false;
    // aggression scale from 0 to 1. 0 = always chase between attacks (aggressive),
    // 1 = never chase between attacks (passive). Values between create probabilistic behavior.
    [Range(0f, 10f)]
    public int aggression = 1;

    // Cached component references — avoids repeated GetComponent calls every frame
    private Rigidbody2D rb;
    private Enemy enemy;
    private EnemyCombat enemyCombat;
    private Shield shield;

    private void Start()
    {
        startX      = transform.position.x;
        rb          = GetComponent<Rigidbody2D>();
        enemy       = GetComponent<Enemy>();
        enemyCombat = GetComponent<EnemyCombat>();
        shield      = GetComponent<Shield>();
        move = GetComponent<Move>();
        SetIdleState();
    }

    private void FixedUpdate()
    {
        // The state machine is skipped while Move is busy, but the bounds clamp below
        // still runs so animation-driven movement is also contained.
        // Only use idle animation while a cutscene is playing
        if(CutsceneManager.cutsceneActive)
        {
            animator.SetBool(walking, false);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        if (!(move != null && move.IsBusy) && !enemy.dying && !PauseMenu.IsPaused && !hardParrying)
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

        // Skipped during hard parry because that teleports the enemy next to the player.
        if (!hardParrying)
            ClampToBounds();
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

        if (isWalking && !noWalk)
        {
            animator.SetBool(walking, true);
            rb.linearVelocity = new Vector2(
                (facingRight ? 1 : -1) * walkSpeed * ShieldBrokenSlowDown(),
                rb.linearVelocity.y);

            if (IsAtEdgeOrWall() || IsBlockedByBounds(facingRight ? 1f : -1f))
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
        // Chase — no attacks in range
        Vector2 direction = (player.position - transform.position).normalized;
        if ((direction.x > 0 && !facingRight) || (direction.x < 0 && facingRight))
            Flip();
        // Always stop movement while attacking — only animation keyframes can trigger movement
        if (attacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetBool(walking, false);
            return;
        }

        bool canSeePlayer = DetectPlayer();

        // Lost-sight grace period: don't immediately de-aggro if LOS is briefly broken
        if (!canSeePlayer)
        {
            lostSightTimer -= Time.deltaTime;
            if (lostSightTimer <= 0f)
            {
                // Check if this enemy can actually de-aggro
                if (!canDeaggro)
                {
                    // Cannot de-aggro: stay in combat state and reset timer
                    lostSightTimer = lostSightTimeout;
                    rb.linearVelocity = Vector2.zero;
                    animator.SetBool(walking, false);
                    return;
                }

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
        float xDistanceToPlayer = Mathf.Abs(player.position.x - transform.position.x);
        bool isStaggering = animator.GetCurrentAnimatorStateInfo(0).IsName("Stagger");

        // Check if within minimum attack distance — force attack if so
        if (xDistanceToPlayer <= minimumAttackDistance && !isStaggering)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool(walking, false);
            Attack();
            return;
        }

        // Check if there are any available attacks in range (not just simple distance check)
        bool canAttack = enemyCombat.CanAttackPlayer(distanceToPlayer);
        // Not in attack range — decide whether to chase based on aggression
        // aggression 0 = always chase, aggression 1 = never chase
        float value = UnityEngine.Random.Range(1, 1000);
        bool shouldChase = value > aggression;
        if (shouldChase && !isStaggering)
        {

            if (!IsAtEdgeOrWall() && !IsBlockedByBounds(direction.x))
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
        else if (!shouldChase && canAttack && !isStaggering)
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
        if (currentState == State.Combat)
            OnDeaggro?.Invoke();
        currentState = State.Idle;
        stateTimer   = UnityEngine.Random.Range(idleTimeMin, idleTimeMax);
        isWalking    = UnityEngine.Random.value > 0.5f;
    }

    void playWalk()
    {
        if (Vector2.Distance(transform.position, player.position) < audioRange)
            audioManager.getInstance().PlayWalk();
    }

    void SetCombatState()
    {
        bool wasAlreadyInCombat = currentState == State.Combat;
        currentState   = State.Combat;
        lostSightTimer = lostSightTimeout; // Start with a full grace period
        if (!wasAlreadyInCombat)
            OnAggro?.Invoke();
    }

    bool IsAtEdgeOrWall()
    {
        bool noGround = !Physics2D.Raycast(groundCheck.position, Vector2.down, ledgeDetectDistance, groundLayer);
        bool hitWall  =  Physics2D.Raycast(wallCheck.position, facingRight ? Vector2.right : Vector2.left, wallDetectDistance, groundLayer);
        return noGround || hitWall;
    }

    /// <summary>
    /// True if moving in dirX (sign only) would push the enemy past its bounds.
    /// Moving back toward the center is always allowed.
    /// </summary>
    bool IsBlockedByBounds(float dirX)
    {
        if (!useBounds) return false;
        float offset = transform.position.x - startX;
        return (dirX > 0f && offset >= maxDistanceFromStart) ||
               (dirX < 0f && offset <= -maxDistanceFromStart);
    }

    /// <summary>
    /// Hard safety net: catches overshoot, knockback, and animation-driven movement.
    /// </summary>
    void ClampToBounds()
    {
        if (!useBounds) return;

        Vector3 pos = transform.position;
        float clampedX = Mathf.Clamp(pos.x, startX - maxDistanceFromStart, startX + maxDistanceFromStart);
        if (Mathf.Approximately(clampedX, pos.x)) return;

        pos.x = clampedX;
        transform.position = pos;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // Draws the patrol bounds in the Scene view when the enemy is selected.
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        float cx = Application.isPlaying ? startX : transform.position.x;
        Gizmos.color = Color.yellow;
        Vector3 left  = new Vector3(cx - maxDistanceFromStart, transform.position.y, 0);
        Vector3 right = new Vector3(cx + maxDistanceFromStart, transform.position.y, 0);
        Gizmos.DrawLine(left + Vector3.down, left + Vector3.up);
        Gizmos.DrawLine(right + Vector3.down, right + Vector3.up);
        Gizmos.DrawLine(left, right);
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