using UnityEngine;

/// <summary>
/// Attach to a GameObject that has a ParticleSystem component.
/// Call TriggerSmokePuff() from code or via the Inspector button to emit a burst of smoke.
/// All visual parameters are exposed in the Inspector for easy tuning.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SmokeParticleController : MonoBehaviour
{
    [Header("Colour")]
    [Tooltip("Primary colour of the smoke particles.")]
    public Color smokeColour = new Color(0.7f, 0.7f, 0.7f, 0.8f);

    [Tooltip("Optional secondary colour for a gradient effect (set alpha to 0 to disable gradient).")]
    public Color smokeColourSecondary = new Color(0.4f, 0.4f, 0.4f, 0.0f);

    [Header("Size")]
    [Tooltip("Starting size of each particle.")]
    [Range(0.05f, 5f)]
    public float startSize = 0.5f;

    [Tooltip("Multiplier applied to size over the particle lifetime (end size = startSize * sizeMultiplier).")]
    [Range(0.5f, 5f)]
    public float sizeMultiplier = 2.5f;

    [Header("Density")]
    [Tooltip("Number of particles emitted per burst.")]
    [Range(1, 500)]
    public int burstCount = 40;

    [Tooltip("Maximum number of particles alive at once.")]
    [Range(1, 1000)]
    public int maxParticles = 200;

    [Header("Emission Duration & Lifetime")]
    [Tooltip("How long (in seconds) the system emits particles after TriggerSmokePuff() is called.")]
    [Range(0.05f, 5f)]
    public float emitDuration = 0.4f;

    [Tooltip("How long each individual particle lives (seconds).")]
    [Range(0.2f, 10f)]
    public float particleLifetime = 1.8f;

    [Header("Motion")]
    [Tooltip("Initial upward speed of emitted particles.")]
    [Range(0f, 5f)]
    public float startSpeed = 0.6f;

    [Tooltip("How much particles spread out from the emit direction (degrees).")]
    [Range(0f, 90f)]
    public float spreadAngle = 45f;

    // ── Private ──────────────────────────────────────────────────────────────

    private ParticleSystem _ps;
    private float _emitTimer = 0f;
    private bool _emitting = false;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        ConfigureParticleSystem();
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        // TriggerSmokePuff();
    }

    /// <summary>
    /// Call this to fire a smoke puff. Safe to call repeatedly.
    /// </summary>
    public void TriggerSmokePuff()
    {
        ConfigureParticleSystem();   // re-apply any Inspector tweaks
        _ps.Emit(burstCount);
        _emitting = true;
        _emitTimer = emitDuration;
    }

    private void Update()
    {
        if (!_emitting) return;

        _emitTimer -= Time.deltaTime;

        if (_emitTimer > 0f)
        {
            // Trickle extra particles during the emit window for a denser look
            float rate = burstCount / Mathf.Max(emitDuration, 0.01f);
            int extra = Mathf.RoundToInt(rate * Time.deltaTime);
            if (extra > 0) _ps.Emit(extra);
        }
        else
        {
            _emitting = false;
        }
    }

    // ── Particle system configuration ────────────────────────────────────────

    private void ConfigureParticleSystem()
    {
        // Main module
        var main = _ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = particleLifetime;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Colour over lifetime
        var col = _ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(smokeColour, 0f),
                new GradientColorKey(smokeColourSecondary.a > 0.01f ? smokeColourSecondary : smokeColour, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(smokeColour.a, 0f),
                new GradientAlphaKey(0f, 1f)          // fade out at end of life
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Size over lifetime
        var sol = _ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = AnimationCurve.Linear(0f, 1f, 1f, sizeMultiplier);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Shape
        var shape = _ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;
        shape.angle = spreadAngle;

        // Renderer – use default particle shader
        var renderer = GetComponent<ParticleSystemRenderer>();
        if (renderer.sharedMaterial == null)
        {
            renderer.sharedMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        }
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 1;
    }

#if UNITY_EDITOR
    // Convenience button in the Inspector during Play Mode
    [ContextMenu("Trigger Smoke Puff (Play Mode)")]
    private void EditorTrigger()
    {
        if (Application.isPlaying) TriggerSmokePuff();
        else Debug.Log("Enter Play Mode to test TriggerSmokePuff.");
    }
#endif
}