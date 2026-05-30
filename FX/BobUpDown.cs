using UnityEngine;

public class BobUpDown : MonoBehaviour
{
    [Tooltip("How far the object moves up and down from its starting position.")]
    public float range = 0.5f;

    [Tooltip("How quickly the object moves up and down.")]
    public float speed = 1f;

    [Tooltip("Offset to desynchronize multiple objects.")]
    public float phaseOffset = 0f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Calculate new Y position using sine wave
        float newY = startPos.y + Mathf.Sin((Time.time + phaseOffset) * speed) * range;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}