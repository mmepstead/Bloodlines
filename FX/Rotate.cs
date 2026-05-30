using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Rotate : MonoBehaviour
{
    public float speed = 1;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    void Update()
    {
        // Rotate in z axis
        transform.Rotate(0, 0, speed * Time.deltaTime);
        ps = GetComponent<ParticleSystem>();
        if(ps)
        {
            particles = new ParticleSystem.Particle[ps.main.maxParticles];
            int count = ps.GetParticles(particles);
            for (int i = 0; i < count; i++)
            {
                // Apply rotation (z-axis for 2D)
                particles[i].rotation += -speed * Time.deltaTime;
              }
            ps.SetParticles(particles, count);
        }
    }
}
