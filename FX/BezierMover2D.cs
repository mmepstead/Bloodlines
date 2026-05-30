using UnityEngine;

public class BezierMover2D : MonoBehaviour
{
    [Header("Bezier Points")]
    public Transform startPoint;
    public Transform controlPoint;
    public Transform endPoint;

    [Header("Movement Settings")]
    [Tooltip("Speed multiplier for curve movement")]
    public float speed = 1f;

    [Tooltip("Whether to loop back and forth along the curve")]
    public bool loop = false;

    private float t = 0f;
    private bool reversing = false;

    void Update()
    {
        if (startPoint == null || controlPoint == null || endPoint == null)
            return;

        // Move 't' forward or backward based on loop setting
        float delta = Time.deltaTime * speed;
        if (loop)
        {
            if (reversing)
            {
                t -= delta;
                if (t <= 0f)
                {
                    t = 0f;
                    reversing = false;
                }
            }
            else
            {
                t += delta;
                if (t >= 1f)
                {
                    t = 1f;
                    reversing = true;
                }
            }
        }
        else
        {
            t = Mathf.Clamp01(t + delta);
        }

        // Calculate position along the Bezier curve
        Vector2 position = CalculateQuadraticBezierPoint(t, startPoint.position, controlPoint.position, endPoint.position);
        transform.position = position;
    }

    /// <summary>
    /// Calculates a point on a quadratic Bezier curve given t, start, control, and end points.
    /// </summary>
    Vector2 CalculateQuadraticBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float u = 1 - t;
        return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
    }
}
