using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to any GameObject with a SpriteRenderer. Call TriggerPunch() whenever
/// you want the sprite to quickly pop outward and then smoothly settle back to
/// its original size - handy for highlighting an increasing combo counter.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ComboPunchScale : MonoBehaviour
{
    [Header("Size")]
    [Tooltip("Peak scale multiplier relative to the original size. 1.3 = 30% bigger at the top of the punch.")]
    [SerializeField] private float punchScale = 1.3f;

    [Header("Timing")]
    [Tooltip("How long it takes to grow out to the peak size. Keep this short for a snappy pop.")]
    [SerializeField] private float growDuration = 0.08f;

    [Tooltip("How long it takes to settle back to the original size after the peak.")]
    [SerializeField] private float shrinkDuration = 0.4f;

    [Header("Easing")]
    [Tooltip("Easing shape for the grow phase (input 0-1 time, output 0-1 blend toward peak scale).")]
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Easing shape for the shrink phase, before bounce is layered on top.")]
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Bounce")]
    [Tooltip("How much springy overshoot to add while settling. 0 = smooth ease only, no bounce.")]
    [SerializeField] private float bounceAmount = 0.15f;

    [Tooltip("How many oscillations the bounce makes while settling.")]
    [SerializeField] private float bounceCycles = 2.5f;

    [Header("Misc")]
    [Tooltip("Animate using unscaled time so the punch still plays if Time.timeScale is 0 (e.g. pause menus).")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("If a new punch is triggered mid-animation, blend smoothly from the current scale instead of snapping back to base first.")]
    [SerializeField] private bool interruptSmoothly = true;

    private Vector3 baseScale;
    private Coroutine punchRoutine;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        if (punchRoutine != null)
        {
            StopCoroutine(punchRoutine);
            punchRoutine = null;
        }
        transform.localScale = baseScale;
    }

    /// <summary>
    /// Call this whenever your combo counter increases (or any other moment you
    /// want to draw attention to this sprite).
    /// </summary>
    [ContextMenu("Trigger Punch")]
    public void TriggerPunch()
    {
        if (punchRoutine != null)
        {
            StopCoroutine(punchRoutine);
            if (!interruptSmoothly)
            {
                transform.localScale = baseScale;
            }
        }

        punchRoutine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        Vector3 startScale = transform.localScale;
        Vector3 peakScale = baseScale * punchScale;

        // --- Grow quickly outward ---
        float elapsed = 0f;
        while (elapsed < growDuration)
        {
            elapsed += GetDeltaTime();
            float n = growDuration > 0f ? Mathf.Clamp01(elapsed / growDuration) : 1f;
            float eased = growCurve.Evaluate(n);
            transform.localScale = Vector3.LerpUnclamped(startScale, peakScale, eased);
            yield return null;
        }
        transform.localScale = peakScale;

        // --- Smoothly settle back to original size, with a touch of springy bounce ---
        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += GetDeltaTime();
            float n = shrinkDuration > 0f ? Mathf.Clamp01(elapsed / shrinkDuration) : 1f;
            float eased = shrinkCurve.Evaluate(n);

            // Decaying oscillation: starts at full strength, fades to 0 as n -> 1,
            // so the object always ends up exactly at baseScale.
            float wobble = 1f + bounceAmount * Mathf.Sin(n * Mathf.PI * bounceCycles) * (1f - n);

            Vector3 settledScale = Vector3.LerpUnclamped(peakScale, baseScale, eased);
            transform.localScale = Vector3.LerpUnclamped(baseScale, settledScale, wobble);
            yield return null;
        }
        transform.localScale = baseScale;

        punchRoutine = null;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        growDuration = Mathf.Max(0f, growDuration);
        shrinkDuration = Mathf.Max(0f, shrinkDuration);
        punchScale = Mathf.Max(0f, punchScale);
        bounceCycles = Mathf.Max(0f, bounceCycles);
    }
#endif
}