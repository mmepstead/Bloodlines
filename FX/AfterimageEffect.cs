using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AfterimageEffect — Spawns fading ghost copies of a 2D pixel-art sprite to create
/// a motion trail / afterimage effect.
///
/// QUICK START
/// -----------
/// 1. Attach this component to any GameObject that has a SpriteRenderer.
/// 2. Call  StartAfterimage()  to begin the effect.
/// 3. Call  StopAfterimage()   to stop spawning new ghosts (existing ones fade out naturally).
/// 4. Call  StopAfterimageImmediate()  to instantly clear all ghosts.
///
/// All parameters are tunable in the Inspector or at runtime before / after starting.
/// </summary>
[AddComponentMenu("2D/Afterimage Effect")]
public class AfterimageEffect : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector — Spawning
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Spawning")]

    [Tooltip("Seconds between each afterimage snapshot. Lower = denser trail.")]
    [Min(0.01f)]
    public float spawnInterval = 0.05f;

    [Tooltip("How many afterimage ghosts can exist at the same time. " +
             "Set to 0 for unlimited (capped only by lifetime / interval).")]
    [Min(0)]
    public int maxGhosts = 20;

    [Tooltip("Start the effect automatically when the component wakes up.")]
    public bool playOnAwake = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector — Lifetime & Fade
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Lifetime & Fade")]

    [Tooltip("How long (seconds) each ghost lingers before fully disappearing.")]
    [Min(0.05f)]
    public float ghostLifetime = 0.3f;

    [Tooltip("Alpha of a ghost at the moment it is spawned (0 = invisible, 1 = opaque).")]
    [Range(0f, 1f)]
    public float initialAlpha = 0.7f;

    [Tooltip("Fade curve over the ghost's lifetime. " +
             "X = normalised time (0 = spawn, 1 = death), Y = alpha multiplier. " +
             "Leave as a linear-down curve for a simple fade.")]
    public AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector — Appearance
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Appearance")]

    [Tooltip("Tint applied to every ghost. Use white to keep the original sprite colours.")]
    public Color ghostColor = Color.white;

    [Tooltip("Sorting-layer offset applied to ghost renderers relative to the source sprite. " +
             "Negative values render behind the character.")]
    public int sortingOrderOffset = -1;

    [Tooltip("Optional material override for ghosts (e.g. an unlit / silhouette shader). " +
             "Leave null to share the source sprite's material.")]
    public Material ghostMaterial = null;

    [Tooltip("Scale multiplier applied to each ghost. 1 = same size as the source.")]
    public float ghostScale = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector — Pooling
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Object Pooling")]

    [Tooltip("Pre-warm the pool with this many ghost objects on Start. " +
             "Prevents small allocation spikes when the effect first fires.")]
    [Min(0)]
    public int initialPoolSize = 10;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────────────────────────────────

    private SpriteRenderer _source;
    private Transform       _sourceTransform;
    private Coroutine       _spawnCoroutine;
    private bool            _isPlaying;

    // Pool
    private readonly Queue<SpriteRenderer>  _pool   = new Queue<SpriteRenderer>();
    private readonly List<GhostInstance>    _active = new List<GhostInstance>();

    // Container keeps the hierarchy tidy
    private Transform _ghostContainer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Nested helper
    // ─────────────────────────────────────────────────────────────────────────

    private struct GhostInstance
    {
        public SpriteRenderer Renderer;
        public float          BornAt;      // Time.time when spawned
        public float          Lifetime;    // copy of ghostLifetime at spawn
        public float          InitAlpha;   // copy of initialAlpha at spawn
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _source          = GetComponent<SpriteRenderer>();
        _sourceTransform = transform;

        if (_source == null)
            Debug.LogWarning($"[AfterimageEffect] No SpriteRenderer found on '{name}'. " +
                             "Attach this component to a GameObject that has a SpriteRenderer.");

        // Create a container so ghosts don't clutter the scene root
        _ghostContainer = new GameObject($"[Afterimages] {name}").transform;
        _ghostContainer.SetParent(null); // world space — ghosts should not move with the parent

        // Pre-warm pool
        for (int i = 0; i < initialPoolSize; i++)
            _pool.Enqueue(CreatePooledRenderer());
    }

    private void Start()
    {
        if (playOnAwake)
            StartAfterimage();
    }

    private void Update()
    {
        // Tick active ghosts; recycle any that have expired
        float now = Time.time;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            GhostInstance g  = _active[i];
            float         t  = (now - g.BornAt) / g.Lifetime;   // 0 → 1

            if (t >= 1f)
            {
                ReturnToPool(g.Renderer);
                _active.RemoveAt(i);
                continue;
            }

            // Apply fade curve
            float alpha = g.InitAlpha * fadeCurve.Evaluate(t);
            Color c     = g.Renderer.color;
            c.a         = alpha;
            g.Renderer.color = c;
        }
    }

    private void OnDestroy()
    {
        StopAfterimageImmediate();

        if (_ghostContainer != null)
            Destroy(_ghostContainer.gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Begin spawning afterimage ghosts at the configured <see cref="spawnInterval"/>.
    /// Safe to call while already playing — does nothing if already active.
    /// </summary>
    public void StartAfterimage()
    {
        if (_isPlaying) return;
        if (_source == null)
        {
            Debug.LogError("[AfterimageEffect] Cannot start: no SpriteRenderer found.");
            return;
        }

        _isPlaying      = true;
        _spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    /// <summary>
    /// Stop spawning new ghosts. Existing ghosts continue to fade out naturally.
    /// </summary>
    public void StopAfterimage()
    {
        if (!_isPlaying) return;

        _isPlaying = false;
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    /// <summary>
    /// Stop spawning and immediately destroy all active ghost instances.
    /// </summary>
    public void StopAfterimageImmediate()
    {
        StopAfterimage();

        for (int i = _active.Count - 1; i >= 0; i--)
            ReturnToPool(_active[i].Renderer);

        _active.Clear();
    }

    /// <summary>
    /// Returns true if the effect is currently spawning new ghosts.
    /// </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// Manually snapshot a single ghost at the current position/sprite.
    /// Useful when you want one-off bursts (e.g. on a dash ability impact).
    /// </summary>
    public void SnapshotOnce()
    {
        if (_source == null) return;
        SpawnGhost();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Internal — spawn loop
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        while (_isPlaying)
        {
            SpawnGhost();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnGhost()
    {
        // Respect max-ghost cap
        if (maxGhosts > 0 && _active.Count >= maxGhosts)
        {
            // Recycle the oldest ghost
            GhostInstance oldest = _active[0];
            _active.RemoveAt(0);
            ReturnToPool(oldest.Renderer);
        }

        SpriteRenderer ghost = GetFromPool();

        // ── Mirror the source renderer ──────────────────────────────────────
        ghost.sprite         = _source.sprite;
        ghost.flipX          = _source.flipX;
        ghost.flipY          = _source.flipY;
        ghost.sortingLayerID = _source.sortingLayerID;
        ghost.sortingOrder   = _source.sortingOrder + sortingOrderOffset;
        ghost.material       = ghostMaterial != null ? ghostMaterial : _source.material;

        // Apply tint with initial alpha
        Color tint = ghostColor;
        tint.a     = initialAlpha;
        ghost.color = tint;

        // ── Position / rotation / scale ─────────────────────────────────────
        Transform t = ghost.transform;
        t.position   = _sourceTransform.position;
        t.rotation   = _sourceTransform.rotation;
        t.localScale  = _sourceTransform.lossyScale * ghostScale;

        ghost.gameObject.SetActive(true);

        _active.Add(new GhostInstance
        {
            Renderer  = ghost,
            BornAt    = Time.time,
            Lifetime  = ghostLifetime,
            InitAlpha = initialAlpha,
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Internal — object pool
    // ─────────────────────────────────────────────────────────────────────────

    private SpriteRenderer GetFromPool()
    {
        if (_pool.Count > 0)
        {
            SpriteRenderer sr = _pool.Dequeue();
            sr.gameObject.SetActive(true);
            return sr;
        }

        return CreatePooledRenderer();
    }

    private void ReturnToPool(SpriteRenderer sr)
    {
        sr.gameObject.SetActive(false);
        _pool.Enqueue(sr);
    }

    private SpriteRenderer CreatePooledRenderer()
    {
        var go = new GameObject("Ghost");
        go.transform.SetParent(_ghostContainer);
        go.SetActive(false);

        var sr = go.AddComponent<SpriteRenderer>();
        return sr;
    }
}
