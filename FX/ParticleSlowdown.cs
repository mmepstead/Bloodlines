using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[RequireComponent(typeof(ParticleSystem))]
public class ParticleSlowdown : MonoBehaviour
{
    [Tooltip("How long before particles start reversing (seconds).")]
    public float reverseDelay = 1.0f;

    [Tooltip("How quickly particles slow down before reversing.")]
    public float slowdownSpeed = 2.0f;

    [Tooltip("How quickly particles accelerate back to the center.")]
    public float reverseSpeed = 2.0f;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private bool reversing = false;
    private float timer = 0f;
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    void Update()
    {
        int count = ps.GetParticles(particles);
        timer += Time.deltaTime;

        // After delay, begin reversing
        if (timer > reverseDelay)
        {
            reversing = true;
        }

        for (int i = 0; i < count; i++)
        {
            if (!reversing)
            {
                // Gradually slow down outward motion
                particles[i].velocity *= Mathf.Clamp01(1f - slowdownSpeed * Time.deltaTime);

            }
            else
            {
                // Accelerate back towards the center
                Vector3 toCenter = -particles[i].position.normalized;
                particles[i].velocity += toCenter * reverseSpeed * Time.deltaTime;                
            }
        }

        ps.SetParticles(particles, count);
    }
}
