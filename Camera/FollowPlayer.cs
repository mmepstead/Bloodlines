using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public int maxLeft;
    public int maxRight;
    public int maxTop;
    public int maxBottom;
    public Transform player;

    private Vector3 shakeOffset; // Camera shake offset
    private Vector3 targetPos;   // Position from player tracking

    void LateUpdate()
    {
        // First, figure out the "follow" position without shake
        float xPos = transform.position.x;
        float yPos = transform.position.y;

        if (player.position.x > maxLeft && player.position.x < maxRight)
            xPos = player.position.x + 0.2f;

        if (player.position.y < maxTop && player.position.y > maxBottom)
            yPos = player.position.y;

        targetPos = new Vector3(xPos, yPos, -10);

        // Apply the shake offset last
        transform.position = targetPos + shakeOffset;
    }

    // This will be called by ImpactManager (or any shake trigger)
    public void ApplyShake(Vector3 offset)
    {
        shakeOffset = offset;
    }
}