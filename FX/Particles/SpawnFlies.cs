using UnityEngine;

public class SpawnFlies : MonoBehaviour
{
    [Header("Assign a Material with a Fly Sprite")]
    public Material FlyMaterial;

    void Start()
    {
        GameObject flies = new GameObject("Flies");
        flies.transform.SetParent(transform);
        flies.transform.localPosition = Vector3.zero;

        var ps = flies.AddComponent<ParticleSystem>();

        // === Main Settings ===
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f); // Flies last 5–7 sec
        main.startSpeed = 0.01f;
        main.startSize = 0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 6; // Cap number of flies
        main.loop = true;

        // === Emission ===
        var emission = ps.emission;
        emission.rateOverTime = 2f; // One fly per second

        // === Shape ===
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // === No directional drift ===
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // === Noise (buzz motion) ===
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 1.5f;
        noise.scrollSpeed = 0.5f;

        // === Renderer ===
        var renderer = flies.GetComponent<ParticleSystemRenderer>();
        renderer.material = FlyMaterial;
        renderer.sortingOrder = 10;
    }
}