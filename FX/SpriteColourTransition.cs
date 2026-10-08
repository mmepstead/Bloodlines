using System.Collections;
using UnityEngine;

/// <summary>
/// Transitions a SpriteRenderer's colour from white to a target colour
/// over a set duration, after an initial delay following Awake.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteColourTransition : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("The colour to transition to.")]
    public Color targetColour = Color.red;

    [Tooltip("How long (in seconds) the colour transition takes.")]
    public float transitionDuration = 2f;

    [Tooltip("Delay (in seconds) after Awake before the transition begins.")]
    public float startDelay = 1f;

    [Header("Easing")]
    [Tooltip("Curve controlling transition speed over time. Leave as linear for a uniform fade.")]
    public AnimationCurve easingCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    // ---------------------------------------------------------------

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Ensure we start at full white regardless of what the sprite's
        // colour is set to in the Inspector.
        _spriteRenderer.color = Color.white;

        StartCoroutine(DelayedTransition());
    }

    private IEnumerator DelayedTransition()
    {
        // Wait for the chosen delay.
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        yield return StartCoroutine(TransitionColour(Color.white, targetColour, transitionDuration));
    }

    private IEnumerator TransitionColour(Color from, Color to, float duration)
    {
        if (duration <= 0f)
        {
            // Instant snap if duration is zero or negative.
            _spriteRenderer.color = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Apply easing curve so the speed profile can be customised.
            float easedT = easingCurve.Evaluate(t);

            _spriteRenderer.color = Color.Lerp(from, to, easedT);
            yield return null; // Wait for the next frame.
        }

        // Guarantee we end exactly on the target colour.
        _spriteRenderer.color = to;
    }
}