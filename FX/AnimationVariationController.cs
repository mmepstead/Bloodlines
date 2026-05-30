using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AnimationVariationController — Applies timed, frame-triggered variations to a
/// Unity Animator without replacing your existing Animator Controller or state machine.
///
/// Every effect is a one-shot call. It arms itself, waits until the Animator reaches
/// a specified frame, applies the variation for a given duration, then cleanly
/// restores normal playback. Multiple effects can be queued; they are executed in
/// the order they become active.
///
/// QUICK START
/// -----------
/// 1. Attach to any GameObject that has an Animator component.
/// 2. Call any of the public effect methods from code (see examples below).
/// 3. Cancel all pending effects at any time with CancelAll().
///
/// REQUIREMENTS
/// ------------
/// • The Animator must use an AnimatorController with at least one state.
/// • Clip frame-rate is read automatically via GetCurrentClipFrameRate().
///   If your clips use non-uniform frame rates, pass an explicit framesPerSecond
///   override where indicated.
///
/// EXAMPLE CALLS
/// -------------
///   // Bounce between frames 4–8 for 1.2 s, starting when frame 4 is reached
///   avc.PingPongFrameRange(triggerFrame: 4, endFrame: 8, duration: 1.2f);
///
///   // Slow to 40 % speed for 0.8 s, starting at frame 6
///   avc.SlowDown(triggerFrame: 6, duration: 0.8f, targetSpeedMultiplier: 0.4f);
///
///   // Speed up to 200 % for 0.5 s, starting at frame 2
///   avc.SpeedUp(triggerFrame: 2, duration: 0.5f, targetSpeedMultiplier: 2f);
///
///   // Freeze on frame 5 for 0.3 s (hit-stop / impact pause)
///   avc.Freeze(triggerFrame: 5, duration: 0.3f);
///
///   // Rewind 3 frames then resume, starting at frame 10, over 0.4 s
///   avc.Rewind(triggerFrame: 10, duration: 0.4f, rewindFrames: 3);
///
///   // Shake speed rapidly between 0.6 and 1.4 for 0.6 s, starting at frame 1
///   avc.SpeedShake(triggerFrame: 1, duration: 0.6f, intensity: 0.4f, frequency: 12f);
///
///   // Ease speed from current to targetMultiplier over the full duration
///   avc.EaseSpeed(triggerFrame: 0, duration: 1f, targetSpeedMultiplier: 0f, easeCurve: AnimationCurve.EaseInOut(0,1,1,0));
/// </summary>
[AddComponentMenu("2D/Animation Variation Controller")]
[RequireComponent(typeof(Animator))]
public class AnimationVariationController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Frame Detection")]

    [Tooltip("Layer index of the Animator state to watch. 0 = base layer.")]
    public int animatorLayer = 0;

    [Tooltip("How many times per second frame-trigger checks run. " +
             "Higher = more precise trigger but slightly more CPU. 60 is plenty for most games.")]
    [Min(10)]
    public float checkRate = 60f;

    [Tooltip("Optional: override the frames-per-second used to convert frame numbers to " +
             "normalised time. Leave at 0 to auto-detect from the current clip.")]
    [Min(0)]
    public float framesPerSecondOverride = 0f;

    [Header("Debug")]

    [Tooltip("Log effect start / end events to the console.")]
    public bool debugLog = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────────────────────────────────

    private Animator   _animator;
    private float      _baseSpeed = 1f;          // animator.speed before any effect
    private bool       _effectRunning = false;

    // Pending effects waiting for their trigger frame
    private readonly List<PendingEffect> _pending = new List<PendingEffect>();

    // Re-used WaitForSeconds to reduce allocations in the check loop
    private WaitForSeconds _checkWait;

    // ─────────────────────────────────────────────────────────────────────────
    //  Nested types
    // ─────────────────────────────────────────────────────────────────────────

    private enum EffectType { PingPong, SlowDown, SpeedUp, Freeze, Rewind, SpeedShake, EaseSpeed }

    private class PendingEffect
    {
        public EffectType Type;
        public int   TriggerFrame;
        public float Duration;

        // PingPong
        public int   EndFrame;

        // SpeedUp / SlowDown / EaseSpeed
        public float TargetSpeedMultiplier;
        public AnimationCurve EaseCurve;

        // Rewind
        public int RewindFrames;

        // SpeedShake
        public float Intensity;
        public float Frequency;

        // Internal tracking
        public bool Armed;   // true once we have passed or reached TriggerFrame
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _animator  = GetComponent<Animator>();
        _baseSpeed = _animator.speed;
        _checkWait = new WaitForSeconds(1f / checkRate);
    }

    private void OnEnable()  => StartCoroutine(FrameWatchLoop());
    private void OnDisable() => StopAllCoroutines();

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Effects
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bounce the animation back and forth between <paramref name="triggerFrame"/>
    /// and <paramref name="endFrame"/> for <paramref name="duration"/> seconds,
    /// then resume normal playback. Great for "hover" or "idle emphasis" moments.
    /// </summary>
    /// <param name="triggerFrame">Frame at which the effect activates.</param>
    /// <param name="endFrame">Inclusive last frame of the ping-pong range.</param>
    /// <param name="duration">How long the ping-pong lasts in seconds.</param>
    public void PingPongFrameRange(int triggerFrame, int endFrame, float duration)
    {
        Queue(new PendingEffect
        {
            Type         = EffectType.PingPong,
            TriggerFrame = triggerFrame,
            EndFrame     = endFrame,
            Duration     = duration,
        });
    }

    /// <summary>
    /// Ramp the Animator speed down to <paramref name="targetSpeedMultiplier"/> instantly
    /// at <paramref name="triggerFrame"/>, hold for <paramref name="duration"/> seconds,
    /// then restore the original speed.
    /// </summary>
    /// <param name="triggerFrame">Frame that arms the slow-down.</param>
    /// <param name="duration">How long the slow-down lasts.</param>
    /// <param name="targetSpeedMultiplier">Speed multiplier to apply (e.g. 0.3 = 30 % speed).</param>
    public void SlowDown(int triggerFrame, float duration, float targetSpeedMultiplier = 0.3f)
    {
        Queue(new PendingEffect
        {
            Type                  = EffectType.SlowDown,
            TriggerFrame          = triggerFrame,
            Duration              = duration,
            TargetSpeedMultiplier = Mathf.Clamp(targetSpeedMultiplier, 0f, 1f),
        });
    }

    /// <summary>
    /// Ramp the Animator speed up to <paramref name="targetSpeedMultiplier"/> instantly
    /// at <paramref name="triggerFrame"/>, hold for <paramref name="duration"/> seconds,
    /// then restore the original speed.
    /// </summary>
    /// <param name="triggerFrame">Frame that arms the speed-up.</param>
    /// <param name="duration">How long the speed-up lasts.</param>
    /// <param name="targetSpeedMultiplier">Speed multiplier (e.g. 2.5 = 250 % speed).</param>
    public void SpeedUp(int triggerFrame, float duration, float targetSpeedMultiplier = 2f)
    {
        Queue(new PendingEffect
        {
            Type                  = EffectType.SpeedUp,
            TriggerFrame          = triggerFrame,
            Duration              = duration,
            TargetSpeedMultiplier = Mathf.Max(1f, targetSpeedMultiplier),
        });
    }

    /// <summary>
    /// Completely freeze the animation at <paramref name="triggerFrame"/> for
    /// <paramref name="duration"/> seconds (hit-stop / impact pause).
    /// Restores the original speed afterwards.
    /// </summary>
    /// <param name="triggerFrame">Frame on which playback halts.</param>
    /// <param name="duration">How long the freeze lasts.</param>
    public void Freeze(int triggerFrame, float duration)
    {
        Queue(new PendingEffect
        {
            Type         = EffectType.Freeze,
            TriggerFrame = triggerFrame,
            Duration     = duration,
        });
    }

    /// <summary>
    /// Briefly scrub the animation backwards by <paramref name="rewindFrames"/> frames
    /// at <paramref name="triggerFrame"/>, then resume forward. Useful for an
    /// "anticipation" stutter or glitch effect.
    /// </summary>
    /// <param name="triggerFrame">Frame that triggers the rewind.</param>
    /// <param name="duration">Total time spent rewinding (controls rewind speed).</param>
    /// <param name="rewindFrames">How many frames to scrub back.</param>
    public void Rewind(int triggerFrame, float duration, int rewindFrames = 3)
    {
        Queue(new PendingEffect
        {
            Type         = EffectType.Rewind,
            TriggerFrame = triggerFrame,
            Duration     = duration,
            RewindFrames = Mathf.Max(1, rewindFrames),
        });
    }

    /// <summary>
    /// Rapidly oscillate the playback speed around the base speed, creating a
    /// "jitter" or "electric" feel. Good for charge-up effects or impacts.
    /// </summary>
    /// <param name="triggerFrame">Frame that starts the shake.</param>
    /// <param name="duration">How long the shake lasts.</param>
    /// <param name="intensity">Max deviation from base speed (e.g. 0.4 → speed swings ±0.4).</param>
    /// <param name="frequency">Oscillations per second (higher = faster jitter).</param>
    public void SpeedShake(int triggerFrame, float duration, float intensity = 0.3f, float frequency = 10f)
    {
        Queue(new PendingEffect
        {
            Type         = EffectType.SpeedShake,
            TriggerFrame = triggerFrame,
            Duration     = duration,
            Intensity    = Mathf.Max(0f, intensity),
            Frequency    = Mathf.Max(1f, frequency),
        });
    }

    /// <summary>
    /// Smoothly ease the playback speed from the current base speed toward
    /// <paramref name="targetSpeedMultiplier"/> over <paramref name="duration"/> seconds
    /// using an <see cref="AnimationCurve"/>, then restore the original speed.
    /// Great for slow-mo ramp-ins, dramatic pauses, or elastic recoveries.
    /// </summary>
    /// <param name="triggerFrame">Frame that starts the ease.</param>
    /// <param name="duration">Duration of the ease in seconds.</param>
    /// <param name="targetSpeedMultiplier">Speed to ease toward (multiplier on base speed).</param>
    /// <param name="easeCurve">
    ///   Curve mapping normalised time (0–1) to speed multiplier (0–1).
    ///   Null defaults to a smooth ease-in-out that reaches the target at t=0.5
    ///   and returns to base at t=1.
    /// </param>
    public void EaseSpeed(int triggerFrame, float duration, float targetSpeedMultiplier,
                          AnimationCurve easeCurve = null)
    {
        if (easeCurve == null)
            easeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f); // caller can supply their own

        Queue(new PendingEffect
        {
            Type                  = EffectType.EaseSpeed,
            TriggerFrame          = triggerFrame,
            Duration              = duration,
            TargetSpeedMultiplier = targetSpeedMultiplier,
            EaseCurve             = easeCurve,
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Control
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cancel all pending and active effects and restore the Animator to its
    /// base speed immediately.
    /// </summary>
    public void CancelAll()
    {
        _pending.Clear();
        StopAllCoroutines();
        _effectRunning = false;
        _animator.speed = _baseSpeed;
        StartCoroutine(FrameWatchLoop());
        Log("CancelAll — all effects cancelled, speed restored.");
    }

    /// <summary>
    /// Returns the number of effects currently queued and waiting to trigger.
    /// </summary>
    public int PendingCount => _pending.Count;

    /// <summary>
    /// Returns true while any variation effect is actively running.
    /// </summary>
    public bool IsEffectActive => _effectRunning;

    // ─────────────────────────────────────────────────────────────────────────
    //  Internal — frame watch loop
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FrameWatchLoop()
    {
        while (true)
        {
            yield return _checkWait;

            if (_pending.Count == 0 || _effectRunning) continue;

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(animatorLayer);
            float fps       = GetCurrentClipFrameRate();
            float clipLen   = state.length;
            int   totalFrames = Mathf.RoundToInt(clipLen * fps);
            int   currentFrame = Mathf.FloorToInt(state.normalizedTime % 1f * totalFrames);

            for (int i = 0; i < _pending.Count; i++)
            {
                PendingEffect e = _pending[i];
                if (currentFrame >= e.TriggerFrame && !e.Armed)
                {
                    e.Armed = true;
                    _pending.RemoveAt(i);
                    StartCoroutine(RunEffect(e, fps, clipLen));
                    break; // one effect at a time
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Internal — effect runners
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator RunEffect(PendingEffect e, float fps, float clipLength)
    {
        _effectRunning = true;
        Log($"Effect '{e.Type}' starting (trigger frame {e.TriggerFrame}, duration {e.Duration}s).");

        switch (e.Type)
        {
            case EffectType.PingPong:  yield return RunPingPong(e, fps, clipLength); break;
            case EffectType.SlowDown:  yield return RunSpeedSet(e); break;
            case EffectType.SpeedUp:   yield return RunSpeedSet(e); break;
            case EffectType.Freeze:    yield return RunFreeze(e); break;
            case EffectType.Rewind:    yield return RunRewind(e, fps, clipLength); break;
            case EffectType.SpeedShake:yield return RunSpeedShake(e); break;
            case EffectType.EaseSpeed: yield return RunEaseSpeed(e); break;
        }

        _animator.speed = _baseSpeed;
        _effectRunning  = false;
        Log($"Effect '{e.Type}' ended — speed restored to {_baseSpeed}.");
    }

    // ── PingPong ──────────────────────────────────────────────────────────────
    private IEnumerator RunPingPong(PendingEffect e, float fps, float clipLength)
    {
        float frameTime   = 1f / fps;
        int   startFrame  = e.TriggerFrame;
        int   endFrame    = e.EndFrame;
        int   totalFrames = Mathf.RoundToInt(clipLength * fps);
        int   currentFrame = startFrame;
        bool  goingForward = true;

        // Pause the Animator; we drive normalised time manually
        _animator.speed = 0f;

        float elapsed = 0f;
        float nextFrameTime = 0f;

        while (elapsed < e.Duration)
        {
            // Clamp to clip length
            int clampedFrame = Mathf.Clamp(currentFrame, 0, totalFrames - 1);
            _animator.Play(0, animatorLayer, (float)clampedFrame / totalFrames);

            yield return null;
            elapsed += Time.deltaTime;

            nextFrameTime -= Time.deltaTime;
            if (nextFrameTime <= 0f)
            {
                nextFrameTime = frameTime;
                currentFrame += goingForward ? 1 : -1;

                if (currentFrame >= endFrame)   { currentFrame = endFrame;   goingForward = false; }
                if (currentFrame <= startFrame) { currentFrame = startFrame; goingForward = true;  }
            }
        }
    }

    // ── Slow / SpeedUp (instant set) ──────────────────────────────────────────
    private IEnumerator RunSpeedSet(PendingEffect e)
    {
        _animator.speed = _baseSpeed * e.TargetSpeedMultiplier;
        yield return new WaitForSeconds(e.Duration);
    }

    // ── Freeze ────────────────────────────────────────────────────────────────
    private IEnumerator RunFreeze(PendingEffect e)
    {
        _animator.speed = 0f;
        yield return new WaitForSeconds(e.Duration);
    }

    // ── Rewind ────────────────────────────────────────────────────────────────
    private IEnumerator RunRewind(PendingEffect e, float fps, float clipLength)
    {
        int   totalFrames  = Mathf.RoundToInt(clipLength * fps);
        int   startFrame   = Mathf.Clamp(e.TriggerFrame, 0, totalFrames - 1);
        int   targetFrame  = Mathf.Clamp(startFrame - e.RewindFrames, 0, totalFrames - 1);
        float framesToTravel = startFrame - targetFrame;

        if (framesToTravel <= 0f)
        {
            yield break;
        }

        // Play backwards at a speed that covers the requested frames in Duration seconds
        float rewindSpeed = (framesToTravel / totalFrames) / (e.Duration / clipLength);
        _animator.speed   = -rewindSpeed;

        float elapsed = 0f;
        while (elapsed < e.Duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    // ── SpeedShake ────────────────────────────────────────────────────────────
    private IEnumerator RunSpeedShake(PendingEffect e)
    {
        float elapsed = 0f;
        while (elapsed < e.Duration)
        {
            float noise   = Mathf.Sin(elapsed * e.Frequency * Mathf.PI * 2f) * e.Intensity;
            _animator.speed = Mathf.Max(0f, _baseSpeed + noise);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    // ── EaseSpeed ─────────────────────────────────────────────────────────────
    private IEnumerator RunEaseSpeed(PendingEffect e)
    {
        float startMultiplier  = 1f;                      // always starts from base (× 1)
        float targetMultiplier = e.TargetSpeedMultiplier;
        float elapsed          = 0f;

        while (elapsed < e.Duration)
        {
            float t = Mathf.Clamp01(elapsed / e.Duration);
            // The curve maps t → blended multiplier between start and target
            float curveValue   = e.EaseCurve.Evaluate(t);
            float multiplier   = Mathf.Lerp(startMultiplier, targetMultiplier, 1f - curveValue);
            _animator.speed    = _baseSpeed * Mathf.Max(0f, multiplier);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Internal — helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void Queue(PendingEffect e)
    {
        _pending.Add(e);
        Log($"Effect '{e.Type}' queued (trigger frame {e.TriggerFrame}).");
    }

    /// <summary>
    /// Returns the frames-per-second of the currently playing clip.
    /// Falls back to framesPerSecondOverride, then to 12 fps as a safe default.
    /// </summary>
    private float GetCurrentClipFrameRate()
    {
        if (framesPerSecondOverride > 0f) return framesPerSecondOverride;

        AnimatorClipInfo[] clips = _animator.GetCurrentAnimatorClipInfo(animatorLayer);
        if (clips != null && clips.Length > 0 && clips[0].clip != null)
            return clips[0].clip.frameRate;

        return 12f; // safe fallback for pixel-art
    }

    private void Log(string msg)
    {
        if (debugLog)
            Debug.Log($"[AnimationVariationController] {msg}");
    }
}
