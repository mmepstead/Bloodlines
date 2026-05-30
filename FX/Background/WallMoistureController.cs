using UnityEngine;

/// <summary>
/// WallMoistureController
/// Particles cling to the wall surface, slide slowly downward a short
/// distance, then reset — like moisture seeping through old stonework.
///
/// SETUP: Attach to a GameObject with a ParticleSystem.
///   - Emission: disabled, Start Speed: 0, Simulation Space: World
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class WallMoistureController : MonoBehaviour
{
    [Header("Layout")]
    public int   particleCount = 35;
    public float areaWidth     = 4f;
    public float areaHeight    = 3f;

    [Header("Particle Size")]
    public float particleSizeMin = 0.04f;
    public float particleSizeMax = 0.12f;

    [Header("Colour")]
    public Color[] colors = new Color[]
    {
        new Color(0.30f, 0.40f, 0.45f),
        new Color(0.25f, 0.35f, 0.40f),
        new Color(0.20f, 0.30f, 0.35f),
    };

    [Header("Seep Behaviour")]
    [Tooltip("How far each particle slides down before resetting (units).")]
    public float seepDistance = 0.25f;

    [Tooltip("Minimum downward slide speed (units/sec).")]
    public float seepSpeedMin = 0.02f;

    [Tooltip("Maximum downward slide speed (units/sec).")]
    public float seepSpeedMax = 0.08f;

    [Tooltip("Seconds a particle waits at its home position before seeping.")]
    public float pauseMin = 0.5f;
    public float pauseMax = 4.0f;

    // ── Private ───────────────────────────────────────────────────────────────

    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particles;
    private float _fixedZ;

    private float[] _homeX;
    private float[] _homeY;
    private float[] _currentY;
    private float[] _seepSpeed;
    private float[] _pauseTimer;
    private bool[]  _seeping;

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
        _homeX      = new float[particleCount];
        _homeY      = new float[particleCount];
        _currentY   = new float[particleCount];
        _seepSpeed  = new float[particleCount];
        _pauseTimer = new float[particleCount];
        _seeping    = new bool[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            _homeX[i]      = transform.position.x + Random.Range(-areaWidth * 0.5f, areaWidth * 0.5f);
            _homeY[i]      = transform.position.y + Random.Range(-areaHeight * 0.5f, areaHeight * 0.5f);
            _currentY[i]   = _homeY[i];
            _seepSpeed[i]  = Random.Range(seepSpeedMin, seepSpeedMax);
            _pauseTimer[i] = Random.Range(pauseMin, pauseMax);
            _seeping[i]    = false;

            _particles[i]                   = new ParticleSystem.Particle();
            _particles[i].position          = new Vector3(_homeX[i], _homeY[i], _fixedZ);
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
            if (_seeping[i])
            {
                _currentY[i] -= _seepSpeed[i] * dt;
                _particles[i].position = new Vector3(_homeX[i], _currentY[i], _fixedZ);

                if (_homeY[i] - _currentY[i] >= seepDistance)
                {
                    // Reset to home and start pause
                    _currentY[i]   = _homeY[i];
                    _seeping[i]    = false;
                    _pauseTimer[i] = Random.Range(pauseMin, pauseMax);
                    _seepSpeed[i]  = Random.Range(seepSpeedMin, seepSpeedMax);
                }
            }
            else
            {
                _pauseTimer[i] -= dt;
                if (_pauseTimer[i] <= 0f)
                    _seeping[i] = true;
            }

            _particles[i].remainingLifetime = float.MaxValue;
        }

        _ps.SetParticles(_particles, particleCount);
    }
}
