using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AmbientSmoke : MonoBehaviour
{
    public enum DriftDirection { Random, Left, Right, Forward, Backward, Up, Down }

    [Header("Fog Appearance")]
    [Tooltip("Base color and transparency of the fog.")]
    public Color fogColor = new Color(0.7f, 0.7f, 0.7f, 0.3f);

    [Range(0.1f, 3f), Tooltip("Overall scale of fog particles.")]
    public float fogSize = 1f;
    public float fogDensity = 0.5f;

    public float lifetimeMax = 8f;
    public float lifetimeMin = 4f;

    [Header("Movement Settings")]
    [Tooltip("How quickly fog drifts around.")]
    public float driftSpeed = 0.1f;

    [Tooltip("Direction of drift. 'Random' uses noise for organic movement; all others apply a directional velocity.")]
    public DriftDirection driftDirection = DriftDirection.Random;

    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        ConfigureFog();
    }

    void ConfigureFog()
    {
        var main = ps.main;
        main.loop = true;
        main.startLifetime  = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
        main.startSpeed     = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor     = fogColor;
        main.startSize      = fogSize;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = fogDensity;

        // Fade in → hold → fade out
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(fogColor, 0f),
                new GradientColorKey(fogColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f,          0f),
                new GradientAlphaKey(fogColor.a,  0.2f),
                new GradientAlphaKey(fogColor.a,  0.8f),
                new GradientAlphaKey(0f,          1f)
            }
        );
        colorOverLifetime.color = grad;

        // Breathe size over lifetime
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f,   0.7f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f,   0.6f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ConfigureDrift();
    }

    void ConfigureDrift()
    {
        var noise = ps.noise;
        var vel = ps.velocityOverLifetime;

        // Always enable noise for organic, ambient movement
        noise.enabled        = true;
        noise.strength       = driftSpeed;
        noise.frequency      = 0.1f;
        noise.scrollSpeed    = 0.05f;
        noise.separateAxes   = true; // Allows per-axis noise strength control

        if (driftDirection == DriftDirection.Random)
        {
            // Full noise on all axes, no directional velocity
            noise.strengthX  = new ParticleSystem.MinMaxCurve(driftSpeed);
            noise.strengthY  = new ParticleSystem.MinMaxCurve(driftSpeed);
            vel.enabled = false;
        }
        else
        {
            // Determine which axis is the drift axis
            // Zero out noise on that axis, keep it on the others,
            // then apply a constant velocity along it instead.
            ParticleSystem.MinMaxCurve full  = new ParticleSystem.MinMaxCurve(driftSpeed);
            ParticleSystem.MinMaxCurve zero  = new ParticleSystem.MinMaxCurve(0f);
            ParticleSystem.MinMaxCurve drift = new ParticleSystem.MinMaxCurve(driftDirection switch
            {
                DriftDirection.Left     => -driftSpeed,
                DriftDirection.Right    =>  driftSpeed,
                DriftDirection.Forward  =>  driftSpeed,
                DriftDirection.Backward => -driftSpeed,
                DriftDirection.Up       =>  driftSpeed,
                DriftDirection.Down     => -driftSpeed,
                _                       =>  0f
            });

            bool isX = driftDirection == DriftDirection.Left     || driftDirection == DriftDirection.Right;
            bool isY = driftDirection == DriftDirection.Up       || driftDirection == DriftDirection.Down;

            // Suppress noise only on the driven axis; keep it alive on the other two
            noise.strengthX = isX ? zero : full;
            noise.strengthY = isY ? zero : full;

            // Apply constant directional velocity on the driven axis only
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.World;
            vel.x       = isX ? drift : zero;
            vel.y       = isY ? drift : zero;
        }
    }

    void OnValidate()
    {
        if (ps == null) ps = GetComponent<ParticleSystem>();

    #if UNITY_EDITOR
        // Deferring ConfigureFog() prevents Unity's ParticleSystem editor UI
        // from trying to initialize GUI styles outside of OnGUI, which causes
        // the ArgumentException spam when modifying ps.noise in OnValidate().
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) ConfigureFog();
        };
    #else
        ConfigureFog();
    #endif
    }
}