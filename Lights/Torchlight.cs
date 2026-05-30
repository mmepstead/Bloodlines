using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TorchLight : MonoBehaviour
{
    [Header("Flicker Settings")]
    public float minIntensity = 0.8f;     // Minimum brightness
    public float maxIntensity = 1.2f;     // Maximum brightness
    public float flickerSpeed = 15f;      // Higher = faster flickering
    public float scaleVariance = 0.05f;   // Light blob scaling variation

    private SpriteRenderer sr;
    private Vector3 originalScale;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

        // Flicker brightness (via color alpha)
        sr.color = new Color(1f, 1f, 1f, intensity);

        // Optional: flicker scale slightly for added effect
        float scale = 1f + (intensity - 1f) * scaleVariance;
        transform.localScale = originalScale * scale;
    }
}