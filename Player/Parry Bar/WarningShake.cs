using UnityEngine;

/// <summary>
/// Attach to each warning line indicator. Shakes the object by a maximum amount,
/// decreasing toward a minimum as the tracked marker approaches the goal.
/// </summary>
public class WarningShake : MonoBehaviour
{
    [Tooltip("Reference to the moving Marker object.")]
    public Transform marker;

    [Tooltip("Reference to the Goal object.")]
    public Transform goal;

    [Tooltip("Maximum shake magnitude (world units) when the marker is far from the goal.")]
    public float maxShake = 0.1f;

    [Tooltip("Minimum shake magnitude (world units) when the marker is right on top of the goal.")]
    public float minShake = 0.005f;

    [Tooltip("How quickly the shake position updates (oscillations per second).")]
    public float shakeFrequency = 30f;

    Vector3 basePosition;

    void Start()
    {
        basePosition = transform.position;
    }

    void Update()
    {
        if (marker == null || goal == null) return;

        float distanceToGoal = Mathf.Abs(marker.position.x - goal.position.x);

        // Normalise distance: use the full bar width (1.3 units) as the reference max distance
        float barWidth = 1.3f;
        float t = Mathf.Clamp01(distanceToGoal / barWidth);

        float shakeMagnitude = Mathf.Lerp(minShake, maxShake, t);

        // Pseudo-random but smooth shake using Perlin noise
        float offsetX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f * shakeMagnitude;
        float offsetY = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f * shakeMagnitude;

        transform.position = basePosition + new Vector3(offsetX, offsetY, 0f);
    }

    /// <summary>
    /// Call this if the base position needs to be updated after spawning
    /// (e.g. if you reposition the object after instantiation).
    /// </summary>
    public void SetBasePosition(Vector3 pos)
    {
        basePosition = pos;
        transform.position = pos;
    }
}