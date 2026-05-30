using UnityEngine;

/// <summary>
/// WallBugsController
/// Particles sit still for a random duration, then skitter a short distance
/// in a random direction and freeze again — like cockroaches or wall beetles.
///
/// SETUP: Attach to a GameObject with a ParticleSystem.
///   - Emission: disabled, Start Speed: 0, Simulation Space: World
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class WallBugsController : MonoBehaviour
{
    [Header("Layout")]
    public int   particleCount = 20;
    public float areaWidth     = 4f;
    public float areaHeight    = 3f;

    [Header("Particle Size")]
    public float particleSizeMin = 0.04f;
    public float particleSizeMax = 0.10f;

    [Header("Colour")]
    public Color[] colors = new Color[]
    {
        new Color(0.15f, 0.10f, 0.05f),
        new Color(0.20f, 0.15f, 0.08f),
        new Color(0.08f, 0.08f, 0.05f),
    };

    [Header("Behaviour")]
    [Tooltip("Minimum seconds a bug stays still before skittering.")]
    public float stillTimeMin = 1.5f;

    [Tooltip("Maximum seconds a bug stays still before skittering.")]
    public float stillTimeMax = 6.0f;

    [Tooltip("How far a bug moves in one skitter (units).")]
    public float skitterDistance = 0.15f;

    [Tooltip("How fast the bug moves during a skitter (units/sec).")]
    public float skitterSpeed = 0.8f;

    // ── Private ───────────────────────────────────────────────────────────────

    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particles;
    private float _fixedZ;

    private Vector3[] _currentPos;
    private Vector3[] _targetPos;
    private float[]   _stillTimer;    // countdown until next skitter
    private bool[]    _skittering;

    void Start()
    {
        _ps = GetComponent<ParticleSystem>();
        var em = _ps.emission; em.enabled = false;
        var mn = _ps.main;
        mn.startSpeed      = 0f;
        mn.startLifetime   = float.MaxValue;
        mn.simulationSpace = ParticleSystemSimulationSpace.World;
        mn.loop = false; mn.playOnAwake = false;

        _fixedZ      = transform.position.z;
        _particles   = new ParticleSystem.Particle[particleCount];
        _currentPos  = new Vector3[particleCount];
        _targetPos   = new Vector3[particleCount];
        _stillTimer  = new float[particleCount];
        _skittering  = new bool[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            float x = transform.position.x + Random.Range(-areaWidth * 0.5f, areaWidth * 0.5f);
            float y = transform.position.y + Random.Range(-areaHeight * 0.5f, areaHeight * 0.5f);
            _currentPos[i] = new Vector3(x, y, _fixedZ);
            _targetPos[i]  = _currentPos[i];
            _stillTimer[i] = Random.Range(stillTimeMin, stillTimeMax);
            _skittering[i] = false;

            _particles[i]                   = new ParticleSystem.Particle();
            _particles[i].position          = _currentPos[i];
            _particles[i].startColor        = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.black;
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
            if (_skittering[i])
            {
                // Move toward target
                _currentPos[i] = Vector3.MoveTowards(_currentPos[i], _targetPos[i], skitterSpeed * dt);
                _particles[i].position = _currentPos[i];

                if (Vector3.Distance(_currentPos[i], _targetPos[i]) < 0.001f)
                {
                    _skittering[i] = false;
                    _stillTimer[i] = Random.Range(stillTimeMin, stillTimeMax);
                }
            }
            else
            {
                _stillTimer[i] -= dt;
                if (_stillTimer[i] <= 0f)
                {
                    // Pick a new random nearby target, clamped to area bounds
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float dist  = Random.Range(skitterDistance * 0.5f, skitterDistance);
                    float tx    = Mathf.Clamp(_currentPos[i].x + Mathf.Cos(angle) * dist,
                                    transform.position.x - areaWidth  * 0.5f,
                                    transform.position.x + areaWidth  * 0.5f);
                    float ty    = Mathf.Clamp(_currentPos[i].y + Mathf.Sin(angle) * dist,
                                    transform.position.y - areaHeight * 0.5f,
                                    transform.position.y + areaHeight * 0.5f);
                    _targetPos[i]  = new Vector3(tx, ty, _fixedZ);
                    _skittering[i] = true;
                }
            }

            _particles[i].remainingLifetime = float.MaxValue;
        }

        _ps.SetParticles(_particles, particleCount);
    }
}
