using UnityEngine;

public class BurstParticles : MonoBehaviour
{
    void Start()
    {
        float startVelocity1 = -2f;
        float startVelocity2 = -3f;
        float endVelocity1 = -0.2f;
        float endVelocity2 = -0.3f;
        if(transform.parent.localScale.x < 0) {
            startVelocity1 = 2f;
            startVelocity2 = 3f;
            endVelocity1 = 0.2f;
            endVelocity2 = 0.3f;
        }
        var ps = GetComponent<ParticleSystem>();
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        // Create a new curve with values beyond -1 to 1
        AnimationCurve curveMin = new AnimationCurve(
            new Keyframe(0f, startVelocity1),
            new Keyframe(0.2f, startVelocity1),
            new Keyframe(0.5f, endVelocity1)
        );
        AnimationCurve curveMax = new AnimationCurve(
            new Keyframe(0f, startVelocity2),
            new Keyframe(0.2f, startVelocity2),
            new Keyframe(0.5f, endVelocity2)
        );

        // Create a new curve with values beyond -1 to 1
        AnimationCurve curveYMin = new AnimationCurve(
            new Keyframe(0f, -0),
            new Keyframe(0.2f, -0f),
            new Keyframe(0.5f, 0f)
        );
        // Create a new curve with values beyond -1 to 1
        AnimationCurve curveYMax = new AnimationCurve(
            new Keyframe(0f, -0),
            new Keyframe(0.2f, -0f),
            new Keyframe(0.5f, 0f)
        );


        // Set random range between the two curves
        ParticleSystem.MinMaxCurve xCurve = new ParticleSystem.MinMaxCurve(1f, curveMin, curveMax);
        xCurve.mode = ParticleSystemCurveMode.TwoCurves;
        vel.x = xCurve;

        // You can do the same for y and z axes if needed
                // Apply to x-axis (for example)
        ParticleSystem.MinMaxCurve yCurve = new ParticleSystem.MinMaxCurve(1f, curveYMin, curveYMax);
        yCurve.mode = ParticleSystemCurveMode.TwoCurves;
        vel.y = yCurve;

        
        // Z axis: no movement but same mode
        AnimationCurve zCurveMin = new AnimationCurve(new Keyframe(0f, 0f));
        AnimationCurve zCurveMax = new AnimationCurve(new Keyframe(0f, 0f));

        ParticleSystem.MinMaxCurve zCurve = new ParticleSystem.MinMaxCurve(1f, zCurveMin, zCurveMax);
        zCurve.mode = ParticleSystemCurveMode.TwoCurves;
        vel.z = zCurve;

    }
}