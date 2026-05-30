using UnityEngine;

public class FlashLight : MonoBehaviour
{
    public float lifetime = 0.8f;     // Minimum brightness
    float start = 0;
    void Update()
    {
        start += Time.deltaTime;
        if (start >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}