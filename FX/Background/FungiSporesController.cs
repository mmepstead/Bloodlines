using UnityEngine;

/// <summary>
/// FungiSporesController
/// Stationary fungi cluster particles that periodically release a spore
/// particle drifting slowly outward before fading and resetting.
/// The "fungi" particles themselves pulse gently to feel alive.
///
/// SETUP: Attach to a GameObject with a ParticleSystem.
///   - Emission: disabled, Start Speed: 0, Simulation Space: World
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class FungiSporesController : MonoBehaviour
{
    [Header("Fungi Cluster")]
    [Tooltip("Number of fungi body particles (static cluster).")]
    public int fungiCount = 8;

    [Tooltip("Radius of the fungi cluster spread.")]
    public float clusterRadius = 0.25f;

    [Header("Spore Particles")]
    [Tooltip("Number of spore particles (drift outward periodically).")]
    public int sporeCount = 12;

    [Tooltip("How far a spore drifts before resetting (units).")]
    public float sporeDriftDistance = 0.5f;

    [Tooltip("Spore drift speed (units/sec).")]
    public float sporeDriftSpeed = 0.06f;

    [Tooltip("Seconds between a spore releasing (per spore, staggered).")]
    public float sporeInterval = 1.2f;

    [Header("Particle Sizes")]
    [Tooltip("Size range for fungi body particles.")]
    public float fungiSizeMin = 0.10f;
    public float fungiSizeMax = 0.22f;

    [Tooltip("Size range for spore particles.")]
    public float sporeSizeMin = 0.03f;
    public float sporeSizeMax = 0.07f;

    [Header("Colours")]
    public Color[] fungiColors = new Color[]
    {
        new Color(0.55f, 0.40f, 0.10f),
        new Color(0.65f, 0.50f, 0.15f),
        new Color(0.45f, 0.30f, 0.08f),
    };

    public Color[] sporeColors = new Color[]
    {
        new Color(0.80f, 0.75f, 0.50f, 0.8f),
        new Color(0.90f, 0.85f, 0.60f, 0.6f),
    };

    [Header("Pulse")]
    public float pulseSpeed  = 0.5f;
    [Range(0f, 0.4f)]
    public float pulseAmount = 0.12f;

    // ── Private ───────────────────────────────────────────────────────────────

    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particles;   // fungi first, then spores
    private int   _totalCount;
    private float _fixedZ;

    // Fungi
    private Vector3[] _fungiHome;
    private float[]   _fungiBaseSize;
    private float[]   _fungiPulseOffset;

    // Spores
    private Vector3[] _sporeOrigin;     // where the spore spawns (random point in cluster)
    private Vector3[] _sporeDir;        // drift direction (random unit vector)
    private float[]   _sporeTravelled;  // distance drifted so far
    private float[]   _sporeDelay;      // countdown before this spore activates
    private bool[]    _sporeActive;
    private float[]   _sporeBaseSize;

    void Start()
    {
        _totalCount = fungiCount + sporeCount;
        _ps = GetComponent<ParticleSystem>();
        var em = _ps.emission; em.enabled = false;
        var mn = _ps.main;
        mn.startSpeed = 0f; mn.startLifetime = float.MaxValue;
        mn.simulationSpace = ParticleSystemSimulationSpace.World;
        mn.loop = false; mn.playOnAwake = false;

        _fixedZ    = transform.position.z;
        _particles = new ParticleSystem.Particle[_totalCount];

        _fungiHome        = new Vector3[fungiCount];
        _fungiBaseSize    = new float[fungiCount];
        _fungiPulseOffset = new float[fungiCount];

        _sporeOrigin    = new Vector3[sporeCount];
        _sporeDir       = new Vector3[sporeCount];
        _sporeTravelled = new float[sporeCount];
        _sporeDelay     = new float[sporeCount];
        _sporeActive    = new bool[sporeCount];
        _sporeBaseSize  = new float[sporeCount];

        // Initialise fungi
        for (int i = 0; i < fungiCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist  = Random.Range(0f, clusterRadius);
            float wx    = transform.position.x + Mathf.Cos(angle) * dist;
            float wy    = transform.position.y + Mathf.Sin(angle) * dist * 0.5f; // flatten vertically
            _fungiHome[i]        = new Vector3(wx, wy, _fixedZ);
            _fungiBaseSize[i]    = Random.Range(fungiSizeMin, fungiSizeMax);
            _fungiPulseOffset[i] = Random.Range(0f, Mathf.PI * 2f);

            _particles[i].position          = _fungiHome[i];
            _particles[i].startColor        = fungiColors.Length > 0 ? fungiColors[Random.Range(0, fungiColors.Length)] : Color.yellow;
            _particles[i].startSize         = _fungiBaseSize[i];
            _particles[i].remainingLifetime = float.MaxValue;
            _particles[i].startLifetime     = float.MaxValue;
        }

        // Initialise spores (staggered start times so they don't all fire at once)
        for (int j = 0; j < sporeCount; j++)
        {
            ResetSpore(j, j * (sporeInterval / sporeCount));
            _particles[fungiCount + j].startColor = sporeColors.Length > 0
                ? sporeColors[Random.Range(0, sporeColors.Length)] : Color.white;
            _sporeBaseSize[j] = Random.Range(sporeSizeMin, sporeSizeMax);
            _particles[fungiCount + j].startSize         = 0f;  // hidden until active
            _particles[fungiCount + j].remainingLifetime = float.MaxValue;
            _particles[fungiCount + j].startLifetime     = float.MaxValue;
        }

        _ps.Play();
        _ps.SetParticles(_particles, _totalCount);
    }

    void Update()
    {
        _ps.GetParticles(_particles, _totalCount);
        float t  = Time.time;
        float dt = Time.deltaTime;

        // Update fungi pulse
        for (int i = 0; i < fungiCount; i++)
        {
            float pulse = Mathf.Sin(t * pulseSpeed * Mathf.PI * 2f + _fungiPulseOffset[i]);
            _particles[i].startSize         = _fungiBaseSize[i] * (1f + pulse * pulseAmount);
            _particles[i].remainingLifetime = float.MaxValue;
        }

        // Update spores
        for (int j = 0; j < sporeCount; j++)
        {
            int pi = fungiCount + j;

            if (!_sporeActive[j])
            {
                _sporeDelay[j] -= dt;
                if (_sporeDelay[j] <= 0f)
                {
                    _sporeActive[j] = true;
                    _sporeTravelled[j] = 0f;
                }
                _particles[pi].startSize = 0f;  // hidden
            }
            else
            {
                _sporeTravelled[j] += sporeDriftSpeed * dt;
                float progress = _sporeTravelled[j] / sporeDriftDistance;

                // Fade out as the spore reaches its limit
                Color c = _particles[pi].startColor;
                c.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(progress * 1.5f - 0.3f));
                _particles[pi].startColor = c;
                _particles[pi].startSize  = _sporeBaseSize[j] * Mathf.Clamp01(1f - progress);

                _particles[pi].position = _sporeOrigin[j] + _sporeDir[j] * _sporeTravelled[j];

                if (_sporeTravelled[j] >= sporeDriftDistance)
                    ResetSpore(j, Random.Range(0.2f, sporeInterval));
            }

            _particles[pi].remainingLifetime = float.MaxValue;
        }

        _ps.SetParticles(_particles, _totalCount);
    }

    void ResetSpore(int j, float delay)
    {
        // Pick a random spawn point inside the cluster
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist  = Random.Range(0f, clusterRadius * 0.5f);
        _sporeOrigin[j] = new Vector3(
            transform.position.x + Mathf.Cos(angle) * dist,
            transform.position.y + Mathf.Sin(angle) * dist * 0.5f,
            _fixedZ);

        // Random outward drift direction (bias upward slightly)
        float da = Random.Range(0f, Mathf.PI * 2f);
        _sporeDir[j] = new Vector3(Mathf.Cos(da), Mathf.Abs(Mathf.Sin(da)) * 0.6f + 0.2f, 0f).normalized;

        _sporeTravelled[j] = 0f;
        _sporeDelay[j]     = delay;
        _sporeActive[j]    = false;
    }
}
