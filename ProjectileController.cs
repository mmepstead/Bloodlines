using UnityEngine;
using System.Collections;

/// <summary>
/// Controls a spawned 2D projectile: which of 8 cardinal directions (or the Player) it fires toward,
/// how fast/heavy it flies, and what movement pattern it follows (straight, wave, spiral, or homing).
///
/// SETUP:
///  1. Attach this script to your projectile prefab (it will add a Rigidbody2D automatically if missing).
///  2. Set "Direction" in the Inspector, or set it from code right after Instantiate() -
///     e.g. proj.GetComponent<ProjectileController>().direction = FireDirection.NorthEast;
///  3. Pick a "Pattern" and tweak its settings below. Everything is exposed to the Inspector
///     so you can flip between behaviours per-prefab without touching code.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ProjectileController : MonoBehaviour
{
    #region Enums

    /// <summary>The 8 cardinal/intercardinal directions, plus a special option to aim at the Player.</summary>
    public enum FireDirection
    {
        North, South, East, West,
        NorthEast, NorthWest, SouthEast, SouthWest,
        TargetPlayer
    }

    /// <summary>The trajectory shape layered on top of the base direction.</summary>
    public enum MovementPattern
    {
        /// <summary>Flies in a straight line (default).</summary>
        Straight,
        /// <summary>Oscillates side-to-side around its straight-line path, like a sine wave.</summary>
        Wave,
        /// <summary>Spirals around its straight-line path. Radius can grow or shrink over time.</summary>
        Spiral,
        /// <summary>Continuously curves to track the Player as it flies.</summary>
        Homing
    }

    #endregion

    // -----------------------------------------------------------------------------------------
    #region Inspector Fields
    // -----------------------------------------------------------------------------------------

    [Header("=== Direction ===")]
    [Tooltip("Which way the projectile fires. TargetPlayer aims at 'player' (or an object tagged 'Player').")]
    public FireDirection direction = FireDirection.North;

    [Tooltip("Optional. Only needed for TargetPlayer or Homing. If left empty, auto-finds an object tagged 'Player'.")]
    public Transform player;

    [Header("=== Base Physics ===")]
    [Tooltip("Top speed in units/second.")]
    public float maxSpeed = 10f;

    [Tooltip("Units/second^2 the projectile speeds up by until it reaches maxSpeed. Set to 0 to start at full speed instantly.")]
    public float acceleration = 0f;

    [Tooltip("Constant downward pull, units/sec^2. 0 = no gravity (pure pattern-based flight). Try ~2-9 for an arcing lob.")]
    public float gravity = 0f;

    [Tooltip("Seconds before this projectile self-destroys. Set to 0 or less to live forever (until it hits something).")]
    public float lifetime = 5f;

    [Header("=== Movement Pattern ===")]
    [Tooltip("Straight = simple line. Wave = sine oscillation. Spiral = corkscrew. Homing = curves toward the Player.")]
    public MovementPattern pattern = MovementPattern.Straight;

    [Header("--- Wave Settings (used when Pattern = Wave) ---")]
    [Tooltip("How far the projectile swings left/right of its straight path, in units.")]
    public float waveAmplitude = 1f;

    [Tooltip("Distance the projectile must travel to complete one full wave cycle (peak-to-peak-to-start).")]
    public float waveLength = 2f;

    [Header("--- Spiral Settings (used when Pattern = Spiral) ---")]
    [Tooltip("Rotation speed of the spiral, in degrees/second. Higher = tighter, faster corkscrew.")]
    public float spiralAngularSpeed = 240f;

    [Tooltip("How fast the spiral's radius changes, units/second. POSITIVE = growing spiral. NEGATIVE = shrinking spiral. 0 = constant-radius circle around the flight path.")]
    public float spiralRadiusChangeRate = 0.5f;

    [Tooltip("Starting radius of the spiral, in units. Use a small value (e.g. 0.1) for a spiral that starts tight and grows.")]
    public float spiralStartRadius = 0.1f;

    [Header("--- Homing Settings (used when Pattern = Homing) ---")]
    [Tooltip("Maximum turn rate in degrees/second while tracking the Player. Lower = lazier curve, higher = sharper tracking.")]
    public float homingTurnSpeed = 180f;

    [Header("=== Impact (Ground Collision) ===")]
    [Tooltip("Layer(s) treated as a wall/ground impact.")]
    public LayerMask groundLayer;

    [Tooltip("False = destroy immediately on impact. True = stick into the surface, then fade out.")]
    public bool stickToWall = false;

    [Tooltip("Seconds to remain stuck in the wall before fading (only used when stickToWall = true).")]
    public float stickDuration = 2f;

    [Tooltip("Seconds the fade-out takes (only used when stickToWall = true).")]
    public float fadeDuration = 0.5f;

    [Tooltip("How far (in units) the projectile pushes forward along its travel direction to look embedded, once stuck.")]
    public float embedDistance = 0.15f;

    [Tooltip("Optional explosion/impact VFX prefab spawned at the point of impact, in either mode.")]
    public GameObject impactEffectPrefab;

    #endregion

    // -----------------------------------------------------------------------------------------
    #region Private State
    // -----------------------------------------------------------------------------------------

    private Rigidbody2D rb;
    private Vector2 baseDirection;   // normalized straight-line travel direction
    private float currentSpeed;
    private float distanceTraveled;  // used by Wave pattern
    private float spiralRadius;      // used by Spiral pattern
    private float spiralAngle;       // used by Spiral pattern (degrees)
    private float airTime;           // used by gravity
    private bool hasHit;             // true once we've collided with the ground layer
    private Vector2 lastMoveDirection = Vector2.up; // actual direction of travel, updated each frame

    #endregion

    // -----------------------------------------------------------------------------------------
    #region Unity Lifecycle
    // -----------------------------------------------------------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Dynamic (not Kinematic) so collisions are reliably detected against the Ground's
        // Rigidbody2D - required for CompositeCollider2D/Tilemap setups - no matter what body
        // type that Rigidbody2D ends up using. We still drive all movement manually via
        // MovePosition() each frame, so gravityScale is zeroed out (script-level "gravity"
        // field handles arcs instead) and rotation is frozen so impacts don't spin it.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        spiralRadius = spiralStartRadius;
        currentSpeed = acceleration > 0f ? 0f : maxSpeed;

        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        baseDirection = GetDirectionVector();

        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Once we've hit the ground, movement/patterns no longer matter - bail out early.
        if (hasHit) return;

        airTime += Time.deltaTime;

        // Accelerate toward max speed if configured.
        if (acceleration > 0f && currentSpeed < maxSpeed)
            currentSpeed = Mathf.Min(maxSpeed, currentSpeed + acceleration * Time.deltaTime);

        // Homing re-aims the base direction toward the player every frame, within the turn-speed limit.
        if (pattern == MovementPattern.Homing && player != null)
        {
            Vector2 toPlayer = ((Vector2)player.position - rb.position).normalized;
            baseDirection = RotateVectorTowards(
                baseDirection,
                toPlayer,
                homingTurnSpeed * Mathf.Deg2Rad * Time.deltaTime
            );
        }

        Vector2 patternMovement = CalculatePatternMovement();

        // Gravity is applied as a simple accumulating downward drift, independent of pattern.
        Vector2 gravityMovement = Vector2.down * gravity * airTime * Time.deltaTime;

        Vector2 totalMovement = patternMovement + gravityMovement;

        if (totalMovement.sqrMagnitude > 0.0000001f)
            lastMoveDirection = totalMovement.normalized;

        rb.MovePosition(rb.position + totalMovement);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;

        // Only react to collisions with objects on the configured ground/wall layer(s).
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            HandleImpact(collision);
        }
    }

    #endregion

    // -----------------------------------------------------------------------------------------
    #region Impact Handling
    // -----------------------------------------------------------------------------------------

    private void HandleImpact(Collision2D collision)
    {
        hasHit = true;

        // Pulling the body out of the simulation entirely (rather than just zeroing velocity)
        // stops Box2D's own contact resolution from continuing to nudge it along the surface -
        // this is what was causing the "sliding along the ground" look at shallow hit angles.
        rb.simulated = false;

        ContactPoint2D contact = collision.GetContact(0);

        // Optional explosion/impact VFX, spawned regardless of which impact mode is used.
        if (impactEffectPrefab != null)
        {
            Quaternion effectRotation = Quaternion.FromToRotation(Vector3.up, contact.normal);
            Instantiate(impactEffectPrefab, contact.point, effectRotation);
        }

        if (stickToWall)
        {
            StickAndFade();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void StickAndFade()
    {
        // Push forward along the direction it was actually travelling (not the surface normal) so
        // it looks like it kept going a little on impact and is now embedded - this reads correctly
        // at any hit angle, including shallow/grazing hits where the normal component is tiny.
        transform.position += (Vector3)(lastMoveDirection * embedDistance);

        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(stickDuration);
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float elapsed = 0f;
            Color startColor = sr.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeDuration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    #endregion

    // -----------------------------------------------------------------------------------------
    #region Direction Handling
    // -----------------------------------------------------------------------------------------

    /// <summary>Converts the chosen FireDirection enum into a normalized world-space vector.</summary>
    private Vector2 GetDirectionVector()
    {
        switch (direction)
        {
            case FireDirection.North: return Vector2.up;
            case FireDirection.South: return Vector2.down;
            case FireDirection.East: return Vector2.right;
            case FireDirection.West: return Vector2.left;
            case FireDirection.NorthEast: return new Vector2(1f, 1f).normalized;
            case FireDirection.NorthWest: return new Vector2(-1f, 1f).normalized;
            case FireDirection.SouthEast: return new Vector2(1f, -1f).normalized;
            case FireDirection.SouthWest: return new Vector2(-1f, -1f).normalized;
            case FireDirection.TargetPlayer:
                if (player != null)
                    return ((Vector2)player.position - (Vector2)transform.position).normalized;
                Debug.LogWarning($"{name}: FireDirection is TargetPlayer but no Player was found. Defaulting to North.");
                return Vector2.up;
            default:
                return Vector2.up;
        }
    }

    /// <summary>
    /// Rotates 'current' toward 'target' by at most 'maxRadiansDelta', staying normalized.
    /// Unity provides Vector3.RotateTowards but no Vector2 equivalent, so this fills that gap.
    /// </summary>
    private static Vector2 RotateVectorTowards(Vector2 current, Vector2 target, float maxRadiansDelta)
    {
        float currentAngle = Mathf.Atan2(current.y, current.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;

        float maxDegreesDelta = maxRadiansDelta * Mathf.Rad2Deg;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, maxDegreesDelta);

        float rad = newAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    #endregion

    // -----------------------------------------------------------------------------------------
    #region Movement Pattern Calculations
    // -----------------------------------------------------------------------------------------

    /// <summary>Returns this frame's movement delta (in world units) based on the active pattern.</summary>
    private Vector2 CalculatePatternMovement()
    {
        switch (pattern)
        {
            case MovementPattern.Wave:
                return CalculateWaveMovement();
            case MovementPattern.Spiral:
                return CalculateSpiralMovement();
            case MovementPattern.Homing:
            case MovementPattern.Straight:
            default:
                return baseDirection * currentSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Moves forward along baseDirection while oscillating side-to-side (perpendicular to travel)
    /// in a sine wave. waveAmplitude controls swing distance, waveLength controls how "stretched" the wave is.
    /// </summary>
    private Vector2 CalculateWaveMovement()
    {
        Vector2 forwardStep = baseDirection * currentSpeed * Time.deltaTime;
        float prevDistance = distanceTraveled;
        distanceTraveled += forwardStep.magnitude;

        Vector2 perpendicular = new Vector2(-baseDirection.y, baseDirection.x); // 90 degrees to travel direction
        float safeWaveLength = Mathf.Max(waveLength, 0.001f);

        float prevOffset = Mathf.Sin(prevDistance / safeWaveLength * Mathf.PI * 2f) * waveAmplitude;
        float newOffset = Mathf.Sin(distanceTraveled / safeWaveLength * Mathf.PI * 2f) * waveAmplitude;

        // Only apply the CHANGE in lateral offset this frame, so the forward speed stays accurate.
        return forwardStep + perpendicular * (newOffset - prevOffset);
    }

    /// <summary>
    /// Moves the projectile's centerline forward along baseDirection while it corkscrews around that
    /// centerline. spiralRadiusChangeRate > 0 makes it grow outward, < 0 makes it shrink inward, 0 keeps
    /// a constant-radius circular orbit.
    /// </summary>
    private Vector2 CalculateSpiralMovement()
    {
        float prevAngle = spiralAngle;
        float prevRadius = spiralRadius;

        spiralAngle += spiralAngularSpeed * Time.deltaTime;
        spiralRadius = Mathf.Max(0f, spiralRadius + spiralRadiusChangeRate * Time.deltaTime);

        Vector2 centerStep = baseDirection * currentSpeed * Time.deltaTime;

        Vector2 prevOffset = new Vector2(Mathf.Cos(prevAngle * Mathf.Deg2Rad), Mathf.Sin(prevAngle * Mathf.Deg2Rad)) * prevRadius;
        Vector2 newOffset = new Vector2(Mathf.Cos(spiralAngle * Mathf.Deg2Rad), Mathf.Sin(spiralAngle * Mathf.Deg2Rad)) * spiralRadius;

        // Only apply the CHANGE in orbital offset this frame, so the forward speed stays accurate.
        return centerStep + (newOffset - prevOffset);
    }

    #endregion
}