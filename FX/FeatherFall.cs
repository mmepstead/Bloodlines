using UnityEngine;

/// <summary>
/// Makes a spawned feather drift downward with a gentle natural "float," while
/// swaying left/right in sync with its sprite animation.
///
/// HOW THE HORIZONTAL SWAY IS DRIVEN:
/// Rather than guessing frame timing in code, this listens for Animation Events
/// fired directly from the feather's animation clip. In the Animation window:
///   - Add an event on frame 1 that calls MoveLeft()
///   - Add an event on frame 5 that calls MoveRight()
/// The feather will then move left for frames 1-4 and right from frame 5 onward
/// (looping naturally back to left when the clip restarts and frame 1 fires again).
/// This keeps the sway perfectly locked to the animation even if you retime it later.
/// </summary>
[DisallowMultipleComponent]
public class FeatherFall : MonoBehaviour
{
    private enum SwayDirection
    {
        Left,
        Right
    }

    [Header("Falling")]
    [Tooltip("How fast the feather drifts downward, in units/second.")]
    [SerializeField] private float fallSpeed = 1.5f;

    [Header("Natural Floating (vertical bob)")]
    [Tooltip("How far up/down the feather bobs on top of its steady fall, for a natural floaty feel.")]
    [SerializeField] private float floatAmplitude = 0.15f;
    [Tooltip("How quickly the feather bobs up and down. Higher = faster flutter.")]
    [SerializeField] private float floatFrequency = 1.5f;

    [Header("Horizontal Sway (driven by Animation Events)")]
    [Tooltip("Sideways speed while swaying left or right, in units/second.")]
    [SerializeField] private float horizontalSpeed = 0.5f;
    [Tooltip("Direction the feather sways before its first animation event fires (i.e. during frame 0).")]
    [SerializeField] private SwayDirection initialDirection = SwayDirection.Right;

    [Header("Lifecycle (optional cleanup)")]
    [Tooltip("If true, the feather destroys itself once it falls below destroyYThreshold.")]
    [SerializeField] private bool destroyWhenBelowY = false;
    [SerializeField] private float destroyYThreshold = -10f;
    [Tooltip("If greater than 0, the feather destroys itself after this many seconds regardless of position.")]
    [SerializeField] private float maxLifetime = 0f;

    // The "true" underlying position, accumulated each frame from fall + sway.
    // The visual bob is layered on top of this so it never distorts the drift itself.
    private Vector3 driftPosition;
    private SwayDirection currentDirection;
    private float bobPhaseOffset;
    private float age;

    private void Start()
    {
        driftPosition = transform.position;
        currentDirection = initialDirection;

        // Randomize each feather's bob phase so a batch of them doesn't bob in unison.
        bobPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        age += Time.deltaTime;

        // Steady downward drift plus sideways sway, accumulated into driftPosition.
        float horizontalDir = currentDirection == SwayDirection.Left ? -1f : 1f;
        driftPosition += new Vector3(horizontalDir * horizontalSpeed, -fallSpeed, 0f) * Time.deltaTime;

        // Layer a gentle vertical bob on top for a natural floating feel.
        float bob = Mathf.Sin((age * floatFrequency) + bobPhaseOffset) * floatAmplitude;
        transform.position = driftPosition + new Vector3(0f, bob, 0f);

        HandleLifecycle();
    }

    private void HandleLifecycle()
    {
        if (maxLifetime > 0f && age >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (destroyWhenBelowY && transform.position.y <= destroyYThreshold)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Animation Event target - call this from the frame where the sprite starts moving left.
    /// </summary>
    public void MoveLeft()
    {
        currentDirection = SwayDirection.Left;
    }

    /// <summary>
    /// Animation Event target - call this from the frame where the sprite starts moving right.
    /// </summary>
    public void MoveRight()
    {
        currentDirection = SwayDirection.Right;
    }
}