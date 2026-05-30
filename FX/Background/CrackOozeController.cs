using UnityEngine;

/// <summary>
/// CrackOozeController
/// Particles arranged along a crack pattern that slowly seep downward,
/// pause, then reset — like moisture weeping through old stonework.
/// The crack shape is generated procedurally from a jagged random walk.
///
/// SETUP: Attach to a GameObject with a ParticleSystem.
///   - Emission: disabled, Start Speed: 0, Simulation Space: World
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class CrackOozeController : MonoBehaviour
{
    [Header("Crack Shape")]
    [Tooltip("Total particles lining the crack.")]
    public int particleCount = 30;

    [Tooltip("Total length of the crack (units).")]
    public float crackLength = 1.2f;

    [Tooltip("How jagged the crack is (max lateral deviation per step, units).")]
    public float crackJaggedness = 0.06f;

    [Header("Particle Size")]
    public float particleSizeMin = 0.04f;
    public float particleSizeMax = 0.10f;

    [Header("Colour")]
    public Color[] colors = new Color[]
    {
        new Color(0.20f, 0.28f, 0.22f),
        new Color(0.15f, 0.22f, 0.18f),
        new Color(0.25f, 0.35f, 0.25f),
    };

    [Header("Ooze Behaviour")]
    [Tooltip("How far each particle seeps below its crack position before resetting.")]
    public float oozeDistance = 0.12f;

    [Tooltip("Minimum ooze speed (units/sec).")]
    public float oozeSpeedMin = 0.015f;

    [Tooltip("Maximum ooze speed (units/sec).")]
    public float oozeSpeedMax = 0.055f;

    [Tooltip("Seconds a particle waits before starting to ooze.")]
    public float pauseMin = 0.5f;
    public float pauseMax = 5.0f;

    // ── Private ───────────────────────────────────────────────────────────────

    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particles;
    private float _fixedZ;

    private float[] _crackX;       // X position on the crack
    private float[] _homeY;        // Y position on the crack (the rest point)
    private float[] _currentY;
    private float[] _oozeSpeed;
    private float[] _pauseTimer;
    private bool[]  _oozing;

    void Start()
    {
        _ps = GetComponent<ParticleSystem>();
        var em = _ps.emission; em.enabled = false;
        var mn = _ps.main;
        mn.startSpeed = 0f; mn.startLifetime = float.MaxValue;
        mn.simulationSpace = ParticleSystemSimulationSpace.World;
        mn.loop = false; mn.playOnAwake = false;

        _fixedZ     = transform.position.z;
        _particles  = new ParticleSystem.Particle[particleCount];
        _crackX     = new float[particleCount];
        _homeY      = new float[particleCount];
        _currentY   = new float[particleCount];
        _oozeSpeed  = new float[particleCount];
        _pauseTimer = new float[particleCount];
        _oozing     = new bool[particleCount];

        // Generate crack path as a downward random walk
        float stepY = crackLength / (particleCount - 1);
        float cx    = transform.position.x;
        float cy    = transform.position.y + crackLength * 0.5f;  // start at top of crack

        for (int i = 0; i < particleCount; i++)
        {
            // Lateral jag
            cx += Random.Range(-crackJaggedness, crackJaggedness);
            // Clamp so the crack doesn't wander too far from centre
            cx = Mathf.Clamp(cx,
                transform.position.x - crackLength * 0.3f,
                transform.position.x + crackLength * 0.3f);

            _crackX[i]     = cx;
            _homeY[i]      = cy - stepY * i;
            _currentY[i]   = _homeY[i];
            _oozeSpeed[i]  = Random.Range(oozeSpeedMin, oozeSpeedMax);
            _pauseTimer[i] = Random.Range(pauseMin, pauseMax);
            _oozing[i]     = false;

            _particles[i]                   = new ParticleSystem.Particle();
            _particles[i].position          = new Vector3(_crackX[i], _homeY[i], _fixedZ);
            _particles[i].startColor        = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.gray;
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
        float dt = Time.deltaTime;

        for (int i = 0; i < particleCount; i++)
        {
            if (_oozing[i])
            {
                _currentY[i] -= _oozeSpeed[i] * dt;
                _particles[i].position = new Vector3(_crackX[i], _currentY[i], _fixedZ);

                if (_homeY[i] - _currentY[i] >= oozeDistance)
                {
                    _currentY[i]   = _homeY[i];
                    _oozing[i]     = false;
                    _pauseTimer[i] = Random.Range(pauseMin, pauseMax);
                    _oozeSpeed[i]  = Random.Range(oozeSpeedMin, oozeSpeedMax);
                }
            }
            else
            {
                _pauseTimer[i] -= dt;
                if (_pauseTimer[i] <= 0f)
                    _oozing[i] = true;
            }

            _particles[i].remainingLifetime = float.MaxValue;
        }

        _ps.SetParticles(_particles, particleCount);
    }
}
