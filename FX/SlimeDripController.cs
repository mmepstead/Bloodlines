using UnityEngine;
using System.Collections;

/// <summary>
/// SlimeDripController
/// Attach this to a GameObject that has a ParticleSystem component.
///
/// SETUP:
///   - Add a ParticleSystem to the same GameObject.
///   - Set the ParticleSystem's Start Speed to 0.
///   - Simulation Space should be World.
///   - Disable the Emission module — this script manages all particles manually.
///
/// All movement is strictly in the XY plane (Z is always locked to the
/// GameObject's Z position, so this works correctly in both 2D and 3D projects).
///
/// PARAMETERS:
///   particleCount        - total particles forming the horizontal grate line
///   lineWidth            - horizontal spread of the line
///   particleSizeMin/Max  - random size range per particle
///   slimeColors          - array of colours randomly assigned to particles
///   idleWobbleSpeed/Amount - speed and magnitude of idle Perlin drift
///   dripInterval         - seconds between drip events
///   dripParticleCount    - how many centre particles form the drip
///   dripSag              - how far the drip particles ease down before dropping
///   bulgeDuration        - time (seconds) of the sag phase
///   dropSpeed            - downward units per second during the drop
///   dropDistance         - how far the drop falls before resetting
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SlimeDripController : MonoBehaviour
{
    // ── Inspector Parameters ──────────────────────────────────────────────────

    [Header("Line Setup")]
    [Tooltip("Total number of slime particles.")]
    public int particleCount = 40;

    [Tooltip("Horizontal width of the sewer-grate line.")]
    public float lineWidth = 2.0f;

    [Header("Particle Size")]
    [Tooltip("Minimum particle size.")]
    public float particleSizeMin = 0.08f;

    [Tooltip("Maximum particle size.")]
    public float particleSizeMax = 0.18f;

    [Header("Colours")]
    [Tooltip("Colours randomly assigned to particles. Add as many green shades as you like.")]
    public Color[] slimeColors = new Color[]
    {
        new Color(0.10f, 0.55f, 0.10f),
        new Color(0.05f, 0.40f, 0.05f),
        new Color(0.20f, 0.70f, 0.10f),
        new Color(0.15f, 0.50f, 0.25f),
        new Color(0.30f, 0.60f, 0.05f),
    };

    [Header("Idle Wobble")]
    [Tooltip("Speed of the idle Perlin-noise drift.")]
    public float idleWobbleSpeed = 0.6f;

    [Tooltip("Maximum XY displacement during idle wobble.")]
    public float idleWobbleAmount = 0.04f;

    [Header("Drip Timing")]
    [Tooltip("Seconds between drip events.")]
    public float dripInterval = 5.0f;

    [Tooltip("Number of centre particles that detach and drip. These are always taken from the middle of the line.")]
    [Range(1, 40)]
    public int dripParticleCount = 6;

    [Tooltip("How far the drip particles ease downward before the drop begins (visual sag, in units).")]
    public float dripSag = 0.15f;

    [Tooltip("Duration of the sag phase (seconds).")]
    public float bulgeDuration = 1.2f;

    [Header("Drop Physics")]
    [Tooltip("Downward speed of the falling drop (units per second).")]
    public float dropSpeed = 3.5f;

    [Tooltip("Distance the drop falls before particles reset to the line.")]
    public float dropDistance = 3.0f;

    // ── Private State ─────────────────────────────────────────────────────────

    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particles;

    // Cached Z depth — all particles share this so movement stays in XY.
    private float _fixedZ;

    // Per-particle home X position along the grate line (world space)
    private float[] _homeX;
    // Home Y is always the GameObject's Y at Start
    private float   _homeY;

    private float[] _noiseOffsets;
    private Color[] _assignedColors;
    private bool[]  _isDripping;

    private enum Phase { Idle, Bulging, Dropping }
    private Phase _phase      = Phase.Idle;
    private float _phaseTimer = 0f;
    private float _dropTravelled = 0f;  // units fallen so far this drop

    // Per-particle positions captured at the start of the sag phase
    private Vector3[] _phaseStartPos;
    // Per-particle Y target for the bottom of the sag
    private float[]   _sagTargetY;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        _ps = GetComponent<ParticleSystem>();

        var emission = _ps.emission;
        emission.enabled = false;

        var main = _ps.main;
        main.startSpeed      = 0f;
        main.startLifetime   = float.MaxValue;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop            = false;
        main.playOnAwake     = false;

        _fixedZ = transform.position.z;
        _homeY  = transform.position.y;

        _particles     = new ParticleSystem.Particle[particleCount];
        _homeX         = new float[particleCount];
        _noiseOffsets  = new float[particleCount];
        _assignedColors= new Color[particleCount];
        _isDripping    = new bool[particleCount];
        _phaseStartPos = new Vector3[particleCount];
        _sagTargetY    = new float[particleCount];

        InitParticles();

        _ps.Play();
        _ps.SetParticles(_particles, particleCount);

        StartCoroutine(DripLoop());
    }

    void Update()
    {
        _ps.GetParticles(_particles, particleCount);

        float dt = Time.deltaTime;

        switch (_phase)
        {
            case Phase.Idle:     UpdateIdle(dt);   break;
            case Phase.Bulging:  UpdateBulge(dt);  break;
            case Phase.Dropping: UpdateDrop(dt);   break;
        }

        // Keep lifetime frozen so particles never auto-expire
        for (int i = 0; i < particleCount; i++)
            _particles[i].remainingLifetime = float.MaxValue;

        _ps.SetParticles(_particles, particleCount);
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    void InitParticles()
    {
        for (int i = 0; i < particleCount; i++)
        {
            float t  = (particleCount > 1) ? (float)i / (particleCount - 1) : 0.5f;
            float wx = transform.position.x + Mathf.Lerp(-lineWidth * 0.5f, lineWidth * 0.5f, t);

            _homeX[i]          = wx;
            _noiseOffsets[i]   = Random.Range(0f, 100f);
            _assignedColors[i] = slimeColors.Length > 0
                                    ? slimeColors[Random.Range(0, slimeColors.Length)]
                                    : Color.green;
            _isDripping[i]     = false;

            _particles[i]                  = new ParticleSystem.Particle();
            _particles[i].position         = XY(_homeX[i], _homeY);
            _particles[i].startColor       = _assignedColors[i];
            _particles[i].startSize        = Random.Range(particleSizeMin, particleSizeMax);
            _particles[i].remainingLifetime = float.MaxValue;
            _particles[i].startLifetime    = float.MaxValue;
            _particles[i].velocity         = Vector3.zero;
        }
    }

    // ── Phase: Idle ───────────────────────────────────────────────────────────

    void UpdateIdle(float dt)
    {
        float t = Time.time;
        for (int i = 0; i < particleCount; i++)
        {
            if (_isDripping[i]) continue;

            float nx = (Mathf.PerlinNoise(_noiseOffsets[i],       t * idleWobbleSpeed) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(_noiseOffsets[i] + 50f, t * idleWobbleSpeed) - 0.5f) * 2f;

            _particles[i].position = XY(
                _homeX[i] + nx * idleWobbleAmount,
                _homeY    + ny * idleWobbleAmount
            );
        }
    }

    // ── Phase: Bulge ──────────────────────────────────────────────────────────

    void BeginBulge(int[] indices)
    {
        _phase      = Phase.Bulging;
        _phaseTimer = 0f;

        // Space particles evenly along a vertical span of dripSag,
        // so they form a line rather than a cluster.
        float spacing = indices.Length > 1 ? dripSag / (indices.Length - 1) : 0f;

        for (int k = 0; k < indices.Length; k++)
        {
            int i = indices[k];
            _isDripping[i]    = true;
            _phaseStartPos[i] = _particles[i].position;

            // k=0 stays nearest the line, k=last sits furthest down.
            _sagTargetY[i] = _homeY - spacing * k;
        }
    }

    void UpdateBulge(float dt)
    {
        _phaseTimer += dt;
        float progress = Mathf.Clamp01(_phaseTimer / bulgeDuration);
        float ease     = EaseInOut(progress);

        UpdateIdle(dt);

        for (int i = 0; i < particleCount; i++)
        {
            if (!_isDripping[i]) continue;
            // X stays exactly at home position — only Y eases downward
            float y = Mathf.Lerp(_phaseStartPos[i].y, _sagTargetY[i], ease);
            _particles[i].position = XY(_homeX[i], y);
        }

        if (progress >= 1f) BeginDrop();
    }

    // ── Phase: Drop ───────────────────────────────────────────────────────────

    void BeginDrop()
    {
        _phase        = Phase.Dropping;
        _dropTravelled = 0f;

        // Lock in the bulge-end positions as the drop starting points
        for (int i = 0; i < particleCount; i++)
        {
            if (_isDripping[i])
                _phaseStartPos[i] = _particles[i].position;
        }
    }

    void UpdateDrop(float dt)
    {
        _dropTravelled += dt * dropSpeed;
        UpdateIdle(dt);

        for (int i = 0; i < particleCount; i++)
        {
            if (!_isDripping[i]) continue;
            // Only move in Y — X is frozen at the bulge-end X
            _particles[i].position = XY(
                _phaseStartPos[i].x,
                _phaseStartPos[i].y - _dropTravelled
            );
        }

        if (_dropTravelled >= dropDistance)
            ResetDripParticles();
    }

    void ResetDripParticles()
    {
        for (int i = 0; i < particleCount; i++)
        {
            if (!_isDripping[i]) continue;
            _isDripping[i]     = false;
            _particles[i].position = XY(_homeX[i], _homeY);
        }
        _phase = Phase.Idle;
    }

    // ── Drip Coroutine ────────────────────────────────────────────────────────

    IEnumerator DripLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(dripInterval);
            yield return new WaitUntil(() => _phase == Phase.Idle);

            int   count   = Mathf.Clamp(dripParticleCount, 1, particleCount);
            int[] chosen  = PickDripParticles(count);
            BeginBulge(chosen);
        }
    }

    /// <summary>
    /// Always picks the centre N particles of the line.
    /// </summary>
    int[] PickDripParticles(int count)
    {
        int mid   = particleCount / 2;
        int half  = count / 2;
        int start = Mathf.Clamp(mid - half, 0, particleCount - count);
        int[] idx = new int[count];
        for (int k = 0; k < count; k++)
            idx[k] = start + k;
        return idx;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Build a Vector3 with a fixed Z, keeping all motion in XY.</summary>
    Vector3 XY(float x, float y) => new Vector3(x, y, _fixedZ);

    static float EaseInOut(float t) => t * t * (3f - 2f * t);
}