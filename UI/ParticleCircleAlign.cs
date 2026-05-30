using UnityEngine;

public class ParticleCircleAlign : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    public float circleRadius = 2f; // Match this to your Particle System's radius

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void LateUpdate()
    {
        int count = ps.particleCount;
        if (count == 0) return;

        if (particles == null || particles.Length < count)
            particles = new ParticleSystem.Particle[count];

        ps.GetParticles(particles);


        for (int i = 0; i < count; i++)
        {
            Vector3 pos = particles[i].position.normalized;
            float angleRad = Mathf.Atan2(pos.y, pos.x);
            // Set rotation to be tangential to the circle (perpendicular to the radius)
            float angleDeg = angleRad * Mathf.Rad2Deg; // +90° to be tangent
            particles[i].rotation = angleDeg; // Works for 2D
        }
        ps.SetParticles(particles, count);
    }
}