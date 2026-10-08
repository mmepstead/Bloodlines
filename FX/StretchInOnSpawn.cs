using UnityEngine;

public class StretchInOnSpawn : MonoBehaviour
{
    [Tooltip("How long the stretch-in animation takes, in seconds.")]
    [SerializeField] private float stretchDuration = 0.3f;

    [Tooltip("Optional easing curve (0-1 in, 0-1 out). Leave default for linear.")]
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private Vector3 targetScale;
    private float timer;

    private void Awake()
    {
        // Cache the scale this object was set to in the editor/spawn code.
        targetScale = transform.localScale;

        // Start squashed flat on X.
        transform.localScale = new Vector3(0f, targetScale.y, targetScale.z);
    }

    private void Update()
    {
        if (timer >= stretchDuration) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / stretchDuration);
        float easedT = easeCurve.Evaluate(t);

        float x = Mathf.Lerp(0f, targetScale.x, easedT);
        transform.localScale = new Vector3(x, targetScale.y, targetScale.z);
    }
}