using UnityEngine;

public class SpawnSparks : MonoBehaviour
{
    public Material EmberMaterial;

    void Start()
    {
        GameObject embers = new GameObject("FireEmbers");
        embers.transform.SetParent(transform);
        embers.transform.localPosition = Vector3.zero;

        var ps = embers.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 2f;
        main.startSpeed = 0.3f;
        main.startSize = 0.03f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.5f, 0f, 0.5f),
            new Color(1f, 0.2f, 0f, 0.8f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 4f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.1f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var renderer = embers.GetComponent<ParticleSystemRenderer>();
        renderer.material = EmberMaterial;
        renderer.sortingOrder = 10;
    }
}