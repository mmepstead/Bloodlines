using UnityEngine;

/// <summary>
/// Contained smoke for an entrance / doorway the player walks through.
///
/// Two particle populations:
///   1. "Hold" smoke  - the bulk of the particles. Spawns in a box and barely moves,
///                      with only a slight horizontal wander and a touch of vertical float.
///   2. "Drift" wisps - a small fraction that slowly slide in the chosen direction
///                      and fade out. Lives on an auto-created child particle system
///                      (named "DriftWisps") so each population can have its own behavior.
///
/// SETUP: put this on a GameObject with a ParticleSystem and assign a smoke material to
/// its ParticleSystemRenderer (the wisps copy it). Set the object's rotation to (0,0,0),
/// since the default particle-system rotation of -90 on X would lay the box on its side.
/// The wisp child is created on Awake; in the editor use the component's context menu
/// "Create / Rebuild Drift System" to preview it without entering Play mode.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class EntranceSmoke : MonoBehaviour
{
    public enum DriftDirection { Left, Right, Up, Down, Forward, Backward }

    const string DriftChildName = "DriftWisps";
    const float AreaThickness = 0.1f; // Z thickness of the emission box

    [Header("Area")]
    [Tooltip("Width and height of the square emission area (before transform scale).")]
    public Vector2 areaSize = new Vector2(3f, 3f);

    [Header("Appearance")]
    [Tooltip("Base color and peak transparency of the smoke.")]
    public Color fogColor = new Color(0.7f, 0.7f, 0.7f, 0.3f);

    [Range(0.1f, 5f), Tooltip("Overall scale of smoke particles.")]
    public float fogSize = 1.5f;

    [Range(0f, 1f), Tooltip("Drift wisps are this fraction as opaque as the main smoke.")]
    public float wispOpacity = 0.6f;

    [Header("Hold Smoke (the bulk)")]
    [Tooltip("Particles per second. Steady-state count is roughly rate x average lifetime.")]
    public float holdEmissionRate = 1.5f;
    public float holdLifetimeMin = 6f;
    public float holdLifetimeMax = 10f;

    [Tooltip("Horizontal wander strength. Keep low so smoke stays near where it spawned.")]
    public float holdWander = 0.08f;

    [Tooltip("Vertical wander strength. Just enough to feel floaty.")]
    public float holdVerticalFloat = 0.03f;

    [Header("Drift Wisps (the minority)")]
    [Tooltip("Direction the wisps slide before fading away.")]
    public DriftDirection driftDirection = DriftDirection.Right;

    [Range(0f, 0.5f), Tooltip("Wisps emitted per hold particle. 0.1 means roughly 1 wisp for every 10 hold particles.")]
    public float driftRatio = 0.1f;

    [Tooltip("Max drift speed. Each wisp picks a random speed between 60% and 100% of this.")]
    public float driftSpeed = 0.5f;
    public float driftLifetimeMin = 3f;
    public float driftLifetimeMax = 5f;

    [Tooltip("Vertical wander for wisps. Keep low.")]
    public float driftVerticalFloat = 0.03f;

    private ParticleSystem holdPs;
    private ParticleSystem driftPs;

    // ----------------------------
    // Lifecycle
    // ----------------------------

    void Awake()
    {
        holdPs = GetComponent<ParticleSystem>();
        EnsureDriftSystem();
        ConfigureAll(restart: true);
    }

    void OnValidate()
    {
        // Keep values sane
        holdLifetimeMax  = Mathf.Max(holdLifetimeMax, holdLifetimeMin);
        driftLifetimeMax = Mathf.Max(driftLifetimeMax, driftLifetimeMin);

        if (holdPs == null) holdPs = GetComponent<ParticleSystem>();

#if UNITY_EDITOR
        // Deferred so we don't touch ParticleSystem modules from inside OnValidate,
        // which triggers GUI-style ArgumentException spam in the editor.
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            FindDriftSystem();
            ConfigureAll(restart: false);
        };
#else
        FindDriftSystem();
        ConfigureAll(restart: false);
#endif
    }

    [ContextMenu("Create / Rebuild Drift System")]
    void RebuildFromMenu()
    {
        holdPs = GetComponent<ParticleSystem>();
        EnsureDriftSystem();
        ConfigureAll(restart: false);
    }

    // ----------------------------
    // Child system management
    // ----------------------------

    void FindDriftSystem()
    {
        if (driftPs != null) return;
        Transform child = transform.Find(DriftChildName);
        if (child != null) driftPs = child.GetComponent<ParticleSystem>();
    }

    void EnsureDriftSystem()
    {
        FindDriftSystem();
        if (driftPs != null) return;

        GameObject go = new GameObject(DriftChildName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        driftPs = go.AddComponent<ParticleSystem>();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Drift Wisps");
#endif
    }

    // ----------------------------
    // Configuration
    // ----------------------------

    void ConfigureAll(bool restart)
    {
        if (holdPs == null) return;

        if (restart)
        {
            holdPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (driftPs != null) driftPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ConfigureHold();
        ConfigureDrift();

        if (restart)
        {
            holdPs.Play();
            if (driftPs != null) driftPs.Play();
        }
    }

    void ConfigureHold()
    {
        var main = holdPs.main;
        main.loop            = true;
        if (!holdPs.isPlaying) main.duration = holdLifetimeMax; // prewarm simulates one full duration
        main.prewarm         = true; // Entrance is already smoky when the scene loads
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(holdLifetimeMin, holdLifetimeMax);
        main.startSpeed      = 0f;
        main.startSize       = new ParticleSystem.MinMaxCurve(fogSize * 0.8f, fogSize * 1.2f);
        main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor      = new Color(fogColor.r, fogColor.g, fogColor.b, 1f);
        main.gravityModifier = 0f;
        main.maxParticles    = Mathf.CeilToInt(holdEmissionRate * holdLifetimeMax) + 10;

        var emission = holdPs.emission;
        emission.enabled      = true;
        emission.rateOverTime = holdEmissionRate;

        ApplyBoxShape(holdPs);
        ApplyFade(holdPs, new Color(fogColor.r, fogColor.g, fogColor.b, fogColor.a));
        ApplySizeCurve(holdPs, BreatheCurve());

        // No directional velocity, just a gentle wobble that keeps smoke near its spawn point
        var vel = holdPs.velocityOverLifetime;
        vel.enabled = false;

        var noise = holdPs.noise;
        noise.enabled      = true;
        noise.separateAxes = true;
        noise.frequency    = 0.15f;
        noise.scrollSpeed  = 0.05f;
        noise.strengthX    = new ParticleSystem.MinMaxCurve(holdWander);
        noise.strengthY    = new ParticleSystem.MinMaxCurve(holdVerticalFloat);
        noise.strengthZ    = new ParticleSystem.MinMaxCurve(0f);
    }

    void ConfigureDrift()
    {
        if (driftPs == null) return;

        float wispRate = holdEmissionRate * driftRatio;

        var main = driftPs.main;
        main.loop            = true;
        if (!driftPs.isPlaying) main.duration = driftLifetimeMax;
        main.prewarm         = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Wisps leave the area instead of following the object
        main.startLifetime   = new ParticleSystem.MinMaxCurve(driftLifetimeMin, driftLifetimeMax);
        main.startSpeed      = 0f; // All motion comes from velocity-over-lifetime below
        main.startSize       = new ParticleSystem.MinMaxCurve(fogSize * 0.8f, fogSize * 1.2f);
        main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor      = new Color(fogColor.r, fogColor.g, fogColor.b, 1f);
        main.gravityModifier = 0f;
        main.maxParticles    = Mathf.CeilToInt(wispRate * driftLifetimeMax) + 10;

        var emission = driftPs.emission;
        emission.enabled      = wispRate > 0f;
        emission.rateOverTime = wispRate;

        ApplyBoxShape(driftPs);
        ApplyFade(driftPs, new Color(fogColor.r, fogColor.g, fogColor.b, fogColor.a * wispOpacity));
        ApplySizeCurve(driftPs, GrowCurve());

        // Constant slide along the chosen direction. Unity requires x/y/z curves to share
        // the same mode, so every axis is a TwoConstants curve (non-driven axes are 0..0).
        Vector3 dir = DirectionVector();
        var vel = driftPs.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x = AxisCurve(dir.x);
        vel.y = AxisCurve(dir.y);
        vel.z = AxisCurve(dir.z);

        // Light wobble on top, vertical kept minimal. The driven axis is suppressed
        // so the noise doesn't fight the slide.
        var noise = driftPs.noise;
        noise.enabled      = true;
        noise.separateAxes = true;
        noise.frequency    = 0.15f;
        noise.scrollSpeed  = 0.05f;
        noise.strengthX    = new ParticleSystem.MinMaxCurve(Mathf.Approximately(dir.x, 0f) ? holdWander : 0f);
        noise.strengthY    = new ParticleSystem.MinMaxCurve(Mathf.Approximately(dir.y, 0f) ? driftVerticalFloat : 0f);
        noise.strengthZ    = new ParticleSystem.MinMaxCurve(0f);

        // Match the parent's rendering so the wisps look like the same smoke
        var parentRenderer = holdPs.GetComponent<ParticleSystemRenderer>();
        var driftRenderer  = driftPs.GetComponent<ParticleSystemRenderer>();
        if (parentRenderer != null && driftRenderer != null)
        {
            driftRenderer.sharedMaterial   = parentRenderer.sharedMaterial;
            driftRenderer.sortingLayerID   = parentRenderer.sortingLayerID;
            driftRenderer.sortingOrder     = parentRenderer.sortingOrder;
            driftRenderer.renderMode       = parentRenderer.renderMode;
        }
    }

    // ----------------------------
    // Helpers
    // ----------------------------

    void ApplyBoxShape(ParticleSystem target)
    {
        var shape = target.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position  = Vector3.zero;
        shape.rotation  = Vector3.zero;
        shape.scale     = new Vector3(areaSize.x, areaSize.y, AreaThickness);
    }

    // Fade in -> hold -> fade out
    static void ApplyFade(ParticleSystem target, Color color)
    {
        var col = target.colorOverLifetime;
        col.enabled = true;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f,      0f),
                new GradientAlphaKey(color.a, 0.25f),
                new GradientAlphaKey(color.a, 0.75f),
                new GradientAlphaKey(0f,      1f)
            });
        col.color = grad;
    }

    static void ApplySizeCurve(ParticleSystem target, AnimationCurve curve)
    {
        var sol = target.sizeOverLifetime;
        sol.enabled = true;
        sol.size    = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    // Hold smoke slowly "breathes"
    static AnimationCurve BreatheCurve()
    {
        AnimationCurve c = new AnimationCurve();
        c.AddKey(0f,   0.7f);
        c.AddKey(0.5f, 1f);
        c.AddKey(1f,   0.6f);
        return c;
    }

    // Wisps thin out and spread as they drift away
    static AnimationCurve GrowCurve()
    {
        AnimationCurve c = new AnimationCurve();
        c.AddKey(0f, 0.6f);
        c.AddKey(1f, 1.2f);
        return c;
    }

    // Per-particle random speed between 60% and 100% of driftSpeed on the driven axis
    ParticleSystem.MinMaxCurve AxisCurve(float axisDirection)
    {
        if (Mathf.Approximately(axisDirection, 0f))
            return new ParticleSystem.MinMaxCurve(0f, 0f);

        return new ParticleSystem.MinMaxCurve(
            axisDirection * driftSpeed * 0.6f,
            axisDirection * driftSpeed);
    }

    Vector3 DirectionVector()
    {
        switch (driftDirection)
        {
            case DriftDirection.Left:     return Vector3.left;
            case DriftDirection.Right:    return Vector3.right;
            case DriftDirection.Up:       return Vector3.up;
            case DriftDirection.Down:     return Vector3.down;
            case DriftDirection.Forward:  return Vector3.forward;
            case DriftDirection.Backward: return Vector3.back;
            default:                      return Vector3.zero;
        }
    }

    // Shows the emission area and roughly how far wisps travel before fading out.
    void OnDrawGizmosSelected()
    {
        Gizmos.color  = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, areaSize.y, AreaThickness));

        Gizmos.matrix = Matrix4x4.identity;
        if (driftRatio > 0f)
        {
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position;
            Gizmos.DrawLine(start, start + DirectionVector() * driftSpeed * driftLifetimeMax);
        }
    }
}