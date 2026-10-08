using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to a parent GameObject whose children are the individual gate prongs.
/// Each prong should have a SmokeParticleController on it (or on a child), or
/// assign the smokeControllerPrefab to spawn one at the prong's position.
///
/// Position the prongs in their CLOSED positions in the scene.
///
/// Call OpenGate()  to raise the prongs in sequence.
/// Call CloseGate() to lower the prongs in sequence (same order, timing and effects).
/// </summary>
public class GateController : MonoBehaviour
{
    // ── Initial state ────────────────────────────────────────────────────────

    [Header("Initial State")]
    [Tooltip("If true, the gate starts open: every prong is snapped to its raised position " +
             "on Awake, with no animation or smoke. Place the prongs in their CLOSED " +
             "positions in the scene either way.")]
    public bool startOpen = false;

    // ── Colliders ────────────────────────────────────────────────────────────

    [Header("Colliders")]
    [Tooltip("Disable the gate's colliders while it is open, and re-enable them when it starts closing.")]
    public bool disableCollidersWhenOpen = true;

    [Tooltip("Colliders that block the player. Leave empty to auto-collect every Collider2D " +
             "on this object and its children. Assign manually if your blocking collider " +
             "lives on a different object.")]
    public Collider2D[] gateColliders;

    // ── Prong order ──────────────────────────────────────────────────────────

    public enum ProngOrder
    {
        LeftToRight,        // child index 0 → N
        RightToLeft,        // child index N → 0
        OutsideIn,          // first & last, then converge toward centre
        InsideOut,          // centre first, then radiate outward
        Random,
        Custom              // use the customOrder list
    }

    [Header("Prong Order")]
    [Tooltip("Determines which gate prong activates first (used for both opening and closing).")]
    public ProngOrder prongOrder = ProngOrder.LeftToRight;

    [Tooltip("Only used when ProngOrder is Custom. Fill with child indices in the order you want.")]
    public List<int> customOrder = new List<int>();

    // ── Timing ───────────────────────────────────────────────────────────────

    [Header("Timing")]
    [Tooltip("Seconds between each successive prong starting its move.")]
    [Range(0f, 2f)]
    public float prongStartDelay = 0.15f;

    [Tooltip("Duration of each prong's up/down movement (seconds).")]
    [Range(0.1f, 5f)]
    public float riseDuration = 0.8f;

    // ── Rise distance ────────────────────────────────────────────────────────

    [Header("Rise")]
    [Tooltip("How far (in world units) each prong rises when the gate opens.")]
    [Range(0.1f, 20f)]
    public float riseDistance = 2f;

    [Tooltip("Easing curve for the movement. Leave as EaseInOut for a smooth feel.")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ── Shake ────────────────────────────────────────────────────────────────

    [Header("Shake")]
    [Tooltip("Maximum displacement of the prong during shake (world units).")]
    [Range(0f, 0.5f)]
    public float shakeStrength = 0.04f;

    [Tooltip("How long the prong shakes before moving (seconds).")]
    [Range(0f, 2f)]
    public float shakeDuration = 0.35f;

    // ── Smoke ────────────────────────────────────────────────────────────────

    [Header("Smoke")]
    [Tooltip("Optional: a prefab with SmokeParticleController to instantiate at each prong. " +
             "If null the script looks for a SmokeParticleController on each prong or its children.")]
    public GameObject smokeControllerPrefab;

    // ── State ────────────────────────────────────────────────────────────────

    private Transform[] _prongs;
    private Vector3[] _prongOrigins;       // CLOSED positions, captured in Awake
    private bool _isOpen = false;          // target state: true once opening has been requested
    private Coroutine _sequenceRoutine;

    /// <summary>True while the gate is mid open/close animation.</summary>
    public bool IsAnimating => _sequenceRoutine != null;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        CacheProng();
        ApplyInitialState();
    }

    private void CacheProng()
    {
        int count = transform.childCount;
        _prongs = new Transform[count];
        _prongOrigins = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            _prongs[i] = transform.GetChild(i);
            _prongOrigins[i] = _prongs[i].position;
        }

        if (gateColliders == null || gateColliders.Length == 0)
            gateColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void SetCollidersEnabled(bool value)
    {
        if (!disableCollidersWhenOpen || gateColliders == null) return;

        foreach (Collider2D col in gateColliders)
            if (col != null) col.enabled = value;
    }

    private void ApplyInitialState()
    {
        if (!startOpen)
        {
            _isOpen = false;
            SetCollidersEnabled(true);
            return;
        }

        // Start open: snap every prong to its raised position.
        for (int i = 0; i < _prongs.Length; i++)
            _prongs[i].position = _prongOrigins[i] + Vector3.up * riseDistance;

        // Make sure physics sees the moved prongs straight away.
        Physics2D.SyncTransforms();

        SetCollidersEnabled(false);
        _isOpen = true;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Raises the prongs in sequence.</summary>
    public void OpenGate()
    {
        if (_isOpen)
        {
            Debug.Log("[GateController] Gate is already open.");
            return;
        }

        _isOpen = true;
        StartSequence(opening: true);
    }

    /// <summary>Lowers the prongs in sequence (same order, timing and effects as opening).</summary>
    public void CloseGate()
    {
        if (!_isOpen)
        {
            Debug.Log("[GateController] Gate is already closed.");
            return;
        }

        _isOpen = false;
        StartSequence(opening: false);
    }

    private void StartSequence(bool opening)
    {
        // Cancel any in-progress open/close (and its per-prong coroutines).
        // Prongs animate from wherever they currently are, so reversing mid-way is smooth.
        StopAllCoroutines();
        _sequenceRoutine = StartCoroutine(MoveSequence(opening));
    }

    // ── Sequences ─────────────────────────────────────────────────────────────

    private IEnumerator MoveSequence(bool opening)
    {
        // Closing: block the way again as soon as the gate starts to close.
        if (!opening) SetCollidersEnabled(true);

        int[] order = BuildOrder();
        List<Coroutine> active = new List<Coroutine>();

        for (int step = 0; step < order.Length; step++)
        {
            int index = order[step];
            if (index < 0 || index >= _prongs.Length) continue;

            Vector3 target = opening
                ? _prongOrigins[index] + Vector3.up * riseDistance
                : _prongOrigins[index];

            active.Add(StartCoroutine(AnimateProng(_prongs[index], target)));

            if (step < order.Length - 1)
                yield return new WaitForSeconds(prongStartDelay);
        }

        // Wait for all prongs to finish
        foreach (var c in active)
            yield return c;

        // Opening finished: let the player through.
        if (opening) SetCollidersEnabled(false);

        _sequenceRoutine = null;
    }

    private IEnumerator AnimateProng(Transform prong, Vector3 target)
    {
        Vector3 start = prong.position;

        // 1. Initial smoke puff
        TriggerSmoke(prong);

        // 2. Shake in place
        if (shakeDuration > 0f && shakeStrength > 0f)
            yield return StartCoroutine(ShakeProng(prong, start));

        // 3. Move to target (up when opening, down when closing)
        yield return StartCoroutine(MoveProng(prong, start, target));

        // 4. Final smoke puff at resting position
        TriggerSmoke(prong);
    }

    private IEnumerator ShakeProng(Transform prong, Vector3 origin)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / shakeDuration;

            // Shake fades in quickly then fades out at the end
            float envelope = Mathf.Sin(progress * Mathf.PI);
            Vector3 offset = Random.insideUnitSphere * shakeStrength * envelope;
            offset.z = 0f; // keep 2-D; remove this line for 3-D gates

            prong.position = origin + offset;
            yield return null;
        }

        prong.position = origin;
    }

    private IEnumerator MoveProng(Transform prong, Vector3 from, Vector3 to)
    {
        float elapsed = 0f;

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            prong.position = Vector3.LerpUnclamped(from, to, riseCurve.Evaluate(t));
            yield return null;
        }

        prong.position = to;
    }

    // ── Smoke helper ──────────────────────────────────────────────────────────

    private void TriggerSmoke(Transform prong)
    {
        // First look for a SmokeParticleController already on the prong or its children
        SmokeParticleController smoke = prong.GetComponentInChildren<SmokeParticleController>();

        if (smoke == null && smokeControllerPrefab != null)
        {
            // Spawn a prefab at the prong's current position
            Vector3 offset = new Vector3(0f, -1f, 0f);
            GameObject spawned = Instantiate(smokeControllerPrefab, prong.position + offset, Quaternion.identity);
            smoke = spawned.GetComponent<SmokeParticleController>();

            // Auto-destroy after smoke finishes (lifetime + a small buffer)
            if (smoke != null)
                Destroy(spawned, smoke.particleLifetime + 1f);
        }

        if (smoke != null)
            smoke.TriggerSmokePuff();
        else
            Debug.LogWarning($"[GateController] No SmokeParticleController found for prong '{prong.name}'. " +
                             "Assign a smokeControllerPrefab or add SmokeParticleController to each prong.");
    }

    // ── Order builder ─────────────────────────────────────────────────────────

    private int[] BuildOrder()
    {
        int n = _prongs.Length;

        switch (prongOrder)
        {
            case ProngOrder.LeftToRight:
                return BuildSequential(n, false);

            case ProngOrder.RightToLeft:
                return BuildSequential(n, true);

            case ProngOrder.OutsideIn:
                return BuildOutsideIn(n);

            case ProngOrder.InsideOut:
                return Reverse(BuildOutsideIn(n));

            case ProngOrder.Random:
                return BuildRandom(n);

            case ProngOrder.Custom:
                return customOrder.Count > 0
                    ? customOrder.ToArray()
                    : BuildSequential(n, false);

            default:
                return BuildSequential(n, false);
        }
    }

    private static int[] BuildSequential(int n, bool reverse)
    {
        int[] arr = new int[n];
        for (int i = 0; i < n; i++) arr[i] = reverse ? (n - 1 - i) : i;
        return arr;
    }

    private static int[] BuildOutsideIn(int n)
    {
        int[] arr = new int[n];
        int left = 0, right = n - 1, idx = 0;

        while (left <= right)
        {
            if (left == right)
            {
                arr[idx++] = left;
            }
            else
            {
                arr[idx++] = left;
                arr[idx++] = right;
            }
            left++;
            right--;
        }
        return arr;
    }

    private static int[] BuildRandom(int n)
    {
        int[] arr = BuildSequential(n, false);
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr;
    }

    private static int[] Reverse(int[] arr)
    {
        int[] r = (int[])arr.Clone();
        System.Array.Reverse(r);
        return r;
    }

#if UNITY_EDITOR
    [ContextMenu("Open Gate (Play Mode)")]
    private void EditorOpen()
    {
        if (Application.isPlaying) OpenGate();
        else Debug.Log("Enter Play Mode to test OpenGate.");
    }

    [ContextMenu("Close Gate (Play Mode)")]
    private void EditorClose()
    {
        if (Application.isPlaying) CloseGate();
        else Debug.Log("Enter Play Mode to test CloseGate.");
    }
#endif
}