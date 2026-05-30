using UnityEngine;

/// <summary>
/// WallMossController
/// Particles arranged in small clusters that pulse in scale and drift
/// imperceptibly, like living moss or tiny wall plants breathing.
///
/// SETUP: Attach to a GameObject with a ParticleSystem.
///   - Emission: disabled (script manages particles)
///   - Start Speed: 0, Simulation Space: World
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class WallMossController : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Total number of moss particles.")]
    public int particleCount = 60;

    [Tooltip("Width of the wall area to cover.")]
    public float areaWidth = 4f;

    [Tooltip("Height of the wall area to cover.")]
    public float areaHeight = 3f;

    [Header("Particle Size")]
    public float particleSizeMin = 0.06f;
    public float particleSizeMax = 0.18f;

    [Header("Colour")]
    public Color[] colors = new Color[]
    {
        new Color(0.10f, 0.35f, 0.08f),
        new Color(0.15f, 0.45f, 0.10f),
        new Color(0.08f, 0.28f, 0.06f),
    };

    [Header("Pulse")]
    [Tooltip("How fast the size pulse oscillates.")]
    public float pulseSpeed = 0.4f;

    [Tooltip("How much the size pulses (fraction of base size).")]
    [Range(0f, 0.5f)]
    public float pulseAmount = 0.18f;

    [Header("Drift")]
    [Tooltip("Speed of the subtle Perlin drift.")]
    public float driftSpeed = 0.12f;

    [Tooltip("Max XY drift distance from home position.")]
    public float driftAmount = 0.015f;

    // ── Private ───────────────────────────────────────────────────────────────

    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particles;

    private float   _fixedZ;
    private Vector3[] _homePos;
    private float[]   _noiseOffsets;
    private float[]   _baseSizes;
    private float[]   _pulseOffsets;   // per-particle phase offset so they don't all pulse together

    void Start()
    {
        _ps = GetComponent<ParticleSystem>();
        var em = _ps.emission; em.enabled = false;
        var mn = _ps.main;
        mn.startSpeed      = 0f;
        mn.startLifetime   = float.MaxValue;
        mn.simulationSpace = ParticleSystemSimulationSpace.World;
        mn.loop            = false;
        mn.playOnAwake     = false;

        _fixedZ       = transform.position.z;
        _particles    = new ParticleSystem.Particle[particleCount];
        _homePos      = new Vector3[particleCount];
        _noiseOffsets = new float[particleCount];
        _baseSizes    = new float[particleCount];
        _pulseOffsets = new float[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            float x = transform.position.x + Random.Range(-areaWidth * 0.5f, areaWidth * 0.5f);
            float y = transform.position.y + Random.Range(-areaHeight * 0.5f, areaHeight * 0.5f);
            _homePos[i]      = new Vector3(x, y, _fixedZ);
            _noiseOffsets[i] = Random.Range(0f, 100f);
            _baseSizes[i]    = Random.Range(particleSizeMin, particleSizeMax);
            _pulseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);

            _particles[i]                   = new ParticleSystem.Particle();
            _particles[i].position          = _homePos[i];
            _particles[i].startColor        = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.green;
            _particles[i].startSize         = _baseSizes[i];
            _particles[i].remainingLifetime = float.MaxValue;
            _particles[i].startLifetime     = float.MaxValue;
            _particles[i].velocity          = Vector3.zero;
        }

        _ps.Play();
        _ps.SetParticles(_particles, particleCount);
    }

    void Update()
    {
        _ps.GetParticles(_particles, particleCount);
        float t = Time.time;

        for (int i = 0; i < particleCount; i++)
        {
            // Subtle XY drift
            float nx = (Mathf.PerlinNoise(_noiseOffsets[i],       t * driftSpeed) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(_noiseOffsets[i] + 50f, t * driftSpeed) - 0.5f) * 2f;
            _particles[i].position = new Vector3(
                _homePos[i].x + nx * driftAmount,
                _homePos[i].y + ny * driftAmount,
                _fixedZ);

            // Size pulse
            float pulse = Mathf.Sin(t * pulseSpeed * Mathf.PI * 2f + _pulseOffsets[i]);
            _particles[i].startSize         = _baseSizes[i] * (1f + pulse * pulseAmount);
            _particles[i].remainingLifetime = float.MaxValue;
        }

        _ps.SetParticles(_particles, particleCount);
    }
}
