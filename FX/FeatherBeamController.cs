using UnityEngine;

/// <summary>
/// Controls a particle system to create a "feather beam" effect.
/// Particles start in a horizontal line, then erupt in a chosen cardinal direction,
/// emit at increasing speed for a duration, then fade out.
/// </summary>
public class FeatherBeamController : MonoBehaviour
{
    [System.Serializable]
    public enum CardinalDirection
    {
        Up,
        Down,
        Left,
        Right
    }
    public GameObject summonPortal;

    [Header("Timing")]
    [SerializeField] private float delayBeforeEruption = 1f;
    [SerializeField] private float emissionDuration = 3f;

    [Header("Direction & Speed")]
    [SerializeField] private CardinalDirection direction = CardinalDirection.Up;
    [SerializeField] private float maxParticleSpeed = 10f;
    [SerializeField] private float speedRampUpDuration = 0.5f;

    [Header("Initial Setup")]
    [SerializeField] private float horizontalSpread = 5f;

    [Header("Particle Behavior")]
    [SerializeField] private float particleLifetime = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Feather Spawning")]
    [Tooltip("Prefab spawned along the beam to look like falling feathers.")]
    [SerializeField] private GameObject featherPrefab;
    [Tooltip("Seconds between feather spawns while the beam is active.")]
    [SerializeField] private float featherSpawnInterval = 0.25f;
    [Tooltip("How many feathers to spawn each spawn tick.")]
    [SerializeField] private int feathersPerSpawn = 1;
    [Tooltip("Lowest height (relative to this transform) a feather can spawn at.")]
    [SerializeField] private float featherMinHeight = 0f;
    [Tooltip("Highest height (relative to this transform) a feather can spawn at.")]
    [SerializeField] private float featherMaxHeight = 5f;
    [Tooltip("Rate at which particles are emitted per second during active emission.")]
    [SerializeField] private float emissionRate = 100f;
    private ParticleSystem particleSystem;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityOverLifetimeModule;
    private ParticleSystem.SizeOverLifetimeModule sizeModule;
    private ParticleSystem.ColorOverLifetimeModule colorModule;

    private float elapsedTime = 0f;
    private bool hasErupted = false;
    private float emissionEndTime = 0f;
    private Vector3 emissionDirection = Vector3.up;
    private float nextFeatherSpawnTime = 0f;

    private void Start()
    {
        // Get or create particle system
        particleSystem = GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            Debug.LogError("FeatherBeamController requires a ParticleSystem component!");
            return;
        }

        // Cache all modules
        mainModule = particleSystem.main;
        emissionModule = particleSystem.emission;
        velocityOverLifetimeModule = particleSystem.velocityOverLifetime;
        sizeModule = particleSystem.sizeOverLifetime;
        colorModule = particleSystem.colorOverLifetime;

        // Configure particle system
        ConfigureParticleSystem();
    }

    private void ConfigureParticleSystem()
    {
        // Set particle lifetime
        mainModule.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime);

        // Configure size over lifetime (shrink to zero)
        var sizeOverLifetimeCurve = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, -2f),
            new Keyframe(1f, 0f, -2f, 0f)
        );
        sizeModule.enabled = true;
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeOverLifetimeCurve);

        // Configure color over lifetime (fade out)
        var alphaFadeCurve = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, 0f),
            new Keyframe(1f - (fadeOutDuration / particleLifetime), 1f, 0f, 0f),
            new Keyframe(1f, 0f, 0f, 0f)
        );
        colorModule.enabled = true;
        var gradientAlpha = new ParticleSystem.MinMaxGradient(Color.white);
        colorModule.color = gradientAlpha;

        // We'll use the alpha curve differently since MinMaxGradient doesn't directly support it
        // Instead, we'll manage fade in script if needed

        // Set emission rate to 0 initially
        emissionModule.rateOverTime = 0f;
        emissionModule.enabled = true;
        summonPortal.SetActive(true); // Optionally enable the portal if needed
    }

    private void Update()
    {
        if (particleSystem == null) return;

        elapsedTime += Time.deltaTime;

        // Handle eruption trigger
        if (!hasErupted && elapsedTime >= delayBeforeEruption)
        {
            TriggerEruption();
        }

        // Update emission based on current state
        if (hasErupted)
        {
            float timeSinceEruption = elapsedTime - delayBeforeEruption;

            // Stop emission after duration
            if (timeSinceEruption >= emissionDuration)
            {
                if (emissionModule.enabled)
                {
                    emissionModule.rateOverTime = 0f;
                    BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
                    if(boxCollider != null)
                    {
                        boxCollider.enabled = false; // Enable collider if needed for interactions
                    }
                    summonPortal.SetActive(false); // Optionally enable the portal if needed
                    emissionEndTime = elapsedTime;
                }
            }
            else
            {
                // Ramp up particle speed during emission
                float speedFraction = Mathf.Clamp01(timeSinceEruption / speedRampUpDuration);
                float currentSpeed = maxParticleSpeed * speedFraction;

                // Update velocity based on direction and current speed
                UpdateParticleVelocity(currentSpeed);

                // Spawn falling feathers along the beam while it's active
                UpdateFeatherSpawning();
            }
        }

        // Check if all particles have died (optional: destroy controller after completion)
        if (emissionEndTime > 0 && elapsedTime > emissionEndTime + particleLifetime + 0.5f)
        {
            // All particles have faded, can disable or destroy if desired
            // For now, just let it sit idle
        }
    }

    private void TriggerEruption()
    {
        hasErupted = true;
        // Enable emission with a reasonable emission rate
        emissionModule.rateOverTime = emissionRate; // Adjust for density of particles

        // Set initial direction
        emissionDirection = GetDirectionVector();

        // Start feather spawning immediately on eruption
        nextFeatherSpawnTime = elapsedTime;
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if(boxCollider != null)
        {
            boxCollider.enabled = true; // Enable collider if needed for interactions
        }
    }

    private void UpdateParticleVelocity(float speed)
    {
        // Enable velocity over lifetime module
        velocityOverLifetimeModule.enabled = true;

        // Set velocity in the emission direction with current speed
        velocityOverLifetimeModule.x = new ParticleSystem.MinMaxCurve(emissionDirection.x * speed);
        velocityOverLifetimeModule.y = new ParticleSystem.MinMaxCurve(emissionDirection.y * speed);
        velocityOverLifetimeModule.z = new ParticleSystem.MinMaxCurve(emissionDirection.z * speed);
    }

    private Vector3 GetDirectionVector()
    {
        return direction switch
        {
            CardinalDirection.Up => Vector3.up,
            CardinalDirection.Down => Vector3.down,
            CardinalDirection.Left => Vector3.left,
            CardinalDirection.Right => Vector3.right,
            _ => Vector3.up
        };
    }

    /// <summary>
    /// Spawns feather(s) at a random height along the beam, on a fixed interval,
    /// while the beam is actively emitting.
    /// </summary>
    private void UpdateFeatherSpawning()
    {
        if (featherPrefab == null) return;

        if (elapsedTime >= nextFeatherSpawnTime)
        {
            for (int i = 0; i < feathersPerSpawn; i++)
            {
                SpawnFeather();
            }
            nextFeatherSpawnTime = elapsedTime + featherSpawnInterval;
        }
    }

    /// <summary>
    /// Spawns a single feather sharing the beam's x position, at a random
    /// height within [featherMinHeight, featherMaxHeight] above this transform.
    /// The feather prefab is responsible for its own falling/movement behavior.
    /// </summary>
    private void SpawnFeather()
    {
        float randomHeight = Random.Range(featherMinHeight, featherMaxHeight);
        Vector3 spawnPosition = new Vector3(
            transform.position.x,
            transform.position.y + randomHeight,
            transform.position.z
        );

        Instantiate(featherPrefab, spawnPosition, Quaternion.identity);
    }

    /// <summary>
    /// Manually start the beam emission (alternative to waiting for delay)
    /// </summary>
    public void StartBeam()
    {
        elapsedTime = delayBeforeEruption; // Skip the delay
    }

    /// <summary>
    /// Stop emission immediately
    /// </summary>
    public void StopBeam()
    {
        hasErupted = true;
        emissionModule.rateOverTime = 0f;
        summonPortal.SetActive(false); // Optionally disable the portal if needed
        emissionEndTime = elapsedTime;
    }

    /// <summary>
    /// Reset the controller to initial state
    /// </summary>
    public void Reset()
    {
        elapsedTime = 0f;
        hasErupted = false;
        emissionEndTime = 0f;
        nextFeatherSpawnTime = 0f;
        emissionModule.rateOverTime = 0f;
        particleSystem.Clear();
    }
}