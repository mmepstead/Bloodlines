using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class CircularParticleRotator : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private bool initialized = false;

    void LateUpdate()
    {
        ps = GetComponent<ParticleSystem>();

        // Get all live particles
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
        int count = ps.GetParticles(particles);
        if(count == 0 || initialized) return;
        Vector3 center = transform.position;

        for (int i = 0; i < count; i++)
        {
            // Direction from center to particle
            Vector3 dir = (particles[i].position + transform.position) - center;

            // Calculate angle in degrees so the particle faces outward (perpendicular)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Optional: offset by 90° if you want the particle to "point" tangentially instead of radially
            if(angle == 0 || angle == 180f || angle == 90 || angle == -90f)
            {
                angle += 90f;
            }
            // Apply rotation (z-axis for 2D)
            particles[i].rotation = angle;
        }
        initialized = true;
        // Apply the modified particles back to the system
        ps.SetParticles(particles, count);
    }
}
