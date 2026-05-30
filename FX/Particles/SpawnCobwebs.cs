using UnityEngine;

public class SpawnCobwebs : MonoBehaviour
{
    public Material ThreadMaterial;

    void Start()
    {
        GameObject threads = new GameObject("CobwebThreads");
        threads.transform.SetParent(transform);
        threads.transform.localPosition = Vector3.zero;

        var ps = threads.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 4f;
        main.startSpeed = 0.05f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.gravityModifier = 0.01f;
        main.startRotation = Mathf.PI / 2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 2f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3f, 1f, 1f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

        var renderer = threads.GetComponent<ParticleSystemRenderer>();
        renderer.material = ThreadMaterial;
        renderer.sortingOrder = 10;
    }
}