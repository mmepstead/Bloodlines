using UnityEngine;

/// <summary>
/// SpiderwebController
/// Particles spread in a loose web-like pattern that sways gently as if
/// disturbed by air currents, then springs back to rest.
///
/// SETUP: Attach to a GameObject with a ParticleSystem.
///   - Emission: disabled, Start Speed: 0, Simulation Space: World
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SpiderwebController : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Number of web node particles.")]
    public int   particleCount = 30;

    [Tooltip("Radius of the web spread.")]
    public float webRadius = 0.8f;

    [Tooltip("How many rings of particles to distribute across the radius.")]
    [Range(1, 6)]
    public int rings = 3;

    [Header("Particle Size")]
    public float particleSizeMin = 0.03f;
    public float particleSizeMax = 0.08f;

    [Header("Colour")]
    public Color[] colors = new Color[]
    {
        new Color(0.80f, 0.80f, 0.75f, 0.7f),
        new Color(0.90f, 0.90f, 0.85f, 0.5f),
    };

    [Header("Sway")]
    [Tooltip("Speed of the sway oscillation.")]
    public float swaySpeed = 0.3f;

    [Tooltip("Maximum sway displacement at the web's outer edge (units).")]
    public float swayAmount = 0.04f;

    [Tooltip("How quickly particles spring back after displacement (spring stiffness).")]
    public float springStiffness = 3.5f;

    [Tooltip("Damping on the spring return (higher = less bounce).")]
    [Range(0f, 1f)]
    public float damping = 0.85f;

    // ── Private ───────────────────────────────────────────────────────────────

    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particles;
    private float _fixedZ;

    private Vector3[] _homePos;
    private Vector3[] _currentPos;
    private Vector3[] _velocity;     // spring velocity
    private float[]   _noiseOffsets;
    private float[]   _radialFraction; // 0 at centre, 1 at outer ring — scales sway

    void Start()
    {
        _ps = GetComponent<ParticleSystem>();
        var em = _ps.emission; em.enabled = false;
        var mn = _ps.main;
        mn.startSpeed = 0f; mn.startLifetime = float.MaxValue;
        mn.simulationSpace = ParticleSystemSimulationSpace.World;
        mn.loop = false; mn.playOnAwake = false;

        _fixedZ        = transform.position.z;
        _particles     = new ParticleSystem.Particle[particleCount];
        _homePos       = new Vector3[particleCount];
        _currentPos    = new Vector3[particleCount];
        _velocity      = new Vector3[particleCount];
        _noiseOffsets  = new float[particleCount];
        _radialFraction= new float[particleCount];

        // Distribute particles across concentric rings
        int idx = 0;
        // Centre particle
        if (idx < particleCount)
        {
            _homePos[idx]        = new Vector3(transform.position.x, transform.position.y, _fixedZ);
            _radialFraction[idx] = 0f;
            idx++;
        }

        for (int r = 1; r <= rings && idx < particleCount; r++)
        {
            float fraction    = (float)r / rings;
            float ringRadius  = webRadius * fraction;
            int   spokeCount  = Mathf.Min(Mathf.RoundToInt(6 * r), particleCount - idx);
            for (int s = 0; s < spokeCount && idx < particleCount; s++)
            {
                float angle = (float)s / spokeCount * Mathf.PI * 2f;
                float wx = transform.position.x + Mathf.Cos(angle) * ringRadius;
                float wy = transform.position.y + Mathf.Sin(angle) * ringRadius;
                _homePos[idx]        = new Vector3(wx, wy, _fixedZ);
                _radialFraction[idx] = fraction;
                idx++;
            }
        }

        // Fill any remaining slots with random positions within radius
        while (idx < particleCount)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist  = Random.Range(0f, webRadius);
            _homePos[idx]        = new Vector3(
                transform.position.x + Mathf.Cos(angle) * dist,
                transform.position.y + Mathf.Sin(angle) * dist,
                _fixedZ);
            _radialFraction[idx] = dist / webRadius;
            idx++;
        }

        for (int i = 0; i < particleCount; i++)
        {
            _currentPos[i]   = _homePos[i];
            _velocity[i]     = Vector3.zero;
            _noiseOffsets[i] = Random.Range(0f, 100f);

            _particles[i]                   = new ParticleSystem.Particle();
            _particles[i].position          = _homePos[i];
            _particles[i].startColor        = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;
            _particles[i].startSize         = Random.Range(particleSizeMin, particleSizeMax);
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
        float t  = Time.time;
        float dt = Time.deltaTime;

        // Global sway target — a slow sinusoidal nudge across the whole web
        float swayX = Mathf.Sin(t * swaySpeed * Mathf.PI * 2f) * swayAmount;
        float swayY = Mathf.Sin(t * swaySpeed * Mathf.PI * 2f * 0.7f + 1.3f) * swayAmount * 0.4f;

        for (int i = 0; i < particleCount; i++)
        {
            // Scale sway by radial fraction — outer nodes move more than inner
            Vector3 target = _homePos[i] + new Vector3(
                swayX * _radialFraction[i],
                swayY * _radialFraction[i],
                0f);

            // Spring toward target
            Vector3 force = (target - _currentPos[i]) * springStiffness;
            _velocity[i]  = (_velocity[i] + force * dt) * Mathf.Pow(damping, dt * 60f);
            _currentPos[i]+= _velocity[i] * dt;
            _currentPos[i].z = _fixedZ;

            _particles[i].position          = _currentPos[i];
            _particles[i].remainingLifetime = float.MaxValue;
        }

        _ps.SetParticles(_particles, particleCount);
    }
}
