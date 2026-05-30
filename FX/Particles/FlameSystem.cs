using UnityEngine;

/// <summary>
/// Creates a stylized 2D flame with flickering orange/yellow particles
/// and optional drifting sparks above it.
/// Attach to a GameObject with a ParticleSystem using a white square pixel sprite.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class FlameSystem : MonoBehaviour
{
    [Header("🔥 Main Flame Settings")]
    public Gradient flameColors = new Gradient
    {
        colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0f),
            new GradientColorKey(new Color(1f, 0.9f, 0.4f), 1f)
        },
        alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(0f, 1f)
        }
    };

    [Header("📏 System Dimensions")]
    public float width = 0.4f;
    public float height = 1.0f;
    public int maxParticles = 200;

    [Header("🌬 Flicker & Motion")]
    public float flickerSpeed = 2.0f;
    public float horizontalShift = 0.2f;

    [Header("✨ Sparks (optional)")]
    public bool enableSparks = true;
    public float sparkRate = 1.2f;
    public int sparkMaxParticles = 40;
    public float sparkSpeed = 1.2f;
    public Color sparkColor = new Color(1f, 0.8f, 0.3f, 1f);
    public float sparkDrift = 0.4f;
    public bool local = false;
    private ParticleSystem mainFlame;
    private ParticleSystem sparks;

    void Awake()
    {
        SetupMainFlame();

        if (enableSparks)
            SetupSparks();
    }

    void SetupMainFlame()
    {
        mainFlame = GetComponent<ParticleSystem>();
        var main = mainFlame.main;
        var emission = mainFlame.emission;
        var shape = mainFlame.shape;
        var colorOverLifetime = mainFlame.colorOverLifetime;
        var noise = mainFlame.noise;
        var velocity = mainFlame.velocityOverLifetime;
        var sizeOverLifetime = mainFlame.sizeOverLifetime;

        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startRotation = 0f;
        main.maxParticles = maxParticles;
        main.simulationSpace = local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;

        emission.rateOverTime = 100f;

        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width, height, 0f);
        shape.position = new Vector3(0f, 0f, 0f);

        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(flameColors);

        noise.enabled = true;
        noise.strength = horizontalShift;
        noise.frequency = flickerSpeed;
        noise.scrollSpeed = 0.5f;

        velocity.enabled = true;
        // velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);

        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = mainFlame.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    void SetupSparks()
    {
        GameObject sparkObj = new GameObject("Sparks");
        sparkObj.transform.SetParent(transform);
        sparkObj.transform.localPosition = Vector3.zero;

        sparks = sparkObj.AddComponent<ParticleSystem>();
        var main = sparks.main;
        var emission = sparks.emission;
        var shape = sparks.shape;
        var colorOverLifetime = sparks.colorOverLifetime;
        var velocity = sparks.velocityOverLifetime;

        // --- BASIC SETTINGS ---
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(sparkSpeed * 0.8f, sparkSpeed * 1.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
        main.startColor = sparkColor;
        main.maxParticles = sparkMaxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // --- EMISSION ---
        emission.rateOverTime = sparkRate;

        // --- SHAPE ---
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 0.3f, 0.05f, 0f);
        shape.position = new Vector3(0f, height * 0.1f, 0f);

        // --- VELOCITY ---
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // Ensure all axes use the same mode (constant min/max)
        velocity.x = new ParticleSystem.MinMaxCurve(-sparkDrift, sparkDrift);
        velocity.y = new ParticleSystem.MinMaxCurve(sparkSpeed * 0.8f, sparkSpeed * 1.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // --- COLOR FADE ---
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(sparkColor, 0f),
                new GradientColorKey(new Color(sparkColor.r, sparkColor.g, sparkColor.b, 0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

        // --- RENDERING ---
        var renderer = sparks.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetComponent<ParticleSystemRenderer>().sharedMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 1;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireCube(transform.position + new Vector3(0f, -height * 0.3f, 0f), new Vector3(width, height, 0.1f));
    }
#endif
}
