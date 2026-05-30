using UnityEngine;

public class SpawnDust : MonoBehaviour
{
    public Material SporeMaterial;
    public float scale = 0.03f;
    public float minScale = 0.02f;
    public bool useLocal = false;
    public bool foreground = false;
    public float opacity = 1f;
    public float boxScale = 1f;
    public int maxParticles = 5;
    public float rateOverTime = 6f;
    private ParticleSystem.Particle[] particles;
    private ParticleSystem ps;
    public float fluctuationSpeed = 1f;
    void Start()
    {
        GameObject spores = new GameObject("FloatingSpores");
        spores.transform.SetParent(transform);
        spores.transform.localPosition = Vector3.zero;

        this.ps = spores.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = 0.02f;
        main.startSize = new ParticleSystem.MinMaxCurve(minScale, scale);
        main.simulationSpace = useLocal ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.loop = true;
        var emission = ps.emission;
        emission.rateOverTime = rateOverTime;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(4f*boxScale, 3f*boxScale, 1f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.01f, 0.01f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.01f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.2f;
        noise.scrollSpeed = 0.1f;

        var renderer = spores.GetComponent<ParticleSystemRenderer>();
        renderer.material = SporeMaterial;
        renderer.sortingLayerID = foreground ? SortingLayer.NameToID("Foreground") : SortingLayer.NameToID("Default");
        renderer.sortingOrder = 10;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(185, 158, 248, 1f), 0.0f), new GradientColorKey(new Color(185, 158, 248, 1f), 0.5f), new GradientColorKey(new Color(185, 158, 248, 1f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(opacity, 0.5f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

    }
}