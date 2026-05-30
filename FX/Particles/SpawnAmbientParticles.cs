using UnityEngine;

public class SpawnAmbientParticles : MonoBehaviour
{
    public Material DustMaterial;

    void Start()
    {
        GameObject dust = new GameObject("AmbientDust");
        dust.transform.SetParent(transform);
        dust.transform.localPosition = Vector3.zero;

        var ps = dust.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 10f;
        main.startSpeed = 0.01f;
        main.startSize = 0.02f;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5f, 3f, 1f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 1f, 0.1f), 0f),
                new GradientColorKey(new Color(1f, 1f, 1f, 0.15f), 0.5f),
                new GradientColorKey(new Color(1f, 1f, 1f, 0.05f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.15f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = dust.GetComponent<ParticleSystemRenderer>();
        renderer.material = DustMaterial;
        renderer.sortingOrder = 10;
    }
}