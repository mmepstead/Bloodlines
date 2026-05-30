using UnityEngine;

public class ShadowController : MonoBehaviour
{
    [Header("References")]
    public Transform player;          // The player to follow
    public LayerMask groundLayer;     // What counts as ground

    [Header("Settings")]
    public float maxScale = 1f;       // Full size of shadow
    public float minScale = 0f;       // Scale when max height
    public float maxHeight = 5f;      // Height at which shadow disappears

    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (player == null) return;

        // Raycast straight down from the player in 2D
        RaycastHit2D hit = Physics2D.Raycast(player.position, Vector2.down, Mathf.Infinity, groundLayer);

        if (hit.collider != null)
        {
            // Move shadow to ground point
            transform.position = new Vector3(hit.point.x, hit.point.y - 0.01f, transform.position.z);

            // Get height above ground
            float height = Vector2.Distance(player.position, hit.point);

            // Scale based on height
            float t = Mathf.Clamp01(1f - (height / maxHeight));
            float scaleValue = Mathf.Lerp(minScale, maxScale, t);

            transform.localScale = baseScale * scaleValue;
        }
    }
}