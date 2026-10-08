using UnityEngine;

public class LookAtTarget2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float rotationSpeed = 5f; // 0 = instant snap

    [Tooltip("Subtract 90 if your sprite's 'forward' is drawn facing up instead of right.")]
    [SerializeField] private float angleOffset = -90f;
    public bool continuousUpdate = true;
    public void Start() {
        // If no target is assigned, try to find the player in the scene
        if (target == null) {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) {
                target = player.transform;
            }
        }
        if (target == null) return;

        Vector2 direction = target.position - transform.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
        Quaternion desiredRotation = Quaternion.Euler(0f, 0f, targetAngle);

        transform.rotation = desiredRotation;
    }

    private void Update()
    {
        if (target == null || !continuousUpdate) return;

        Vector2 direction = target.position - transform.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
        Quaternion desiredRotation = Quaternion.Euler(0f, 0f, targetAngle);

        transform.rotation = rotationSpeed > 0f
            ? Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime)
            : desiredRotation;
    }
}