using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// CutsceneTrigger
//
// Drop this on any collider (set Is Trigger = true) to start a cutscene
// when the player walks into it.  Also exposes a public method so you can
// call it from a UnityEvent, Timeline, or any other script.
//
// Each CutsceneTrigger holds ONE CutsceneData asset; swap the asset in the
// Inspector for a different cutscene at the same trigger point.
// ─────────────────────────────────────────────────────────────────────────────
public class CutsceneTrigger : MonoBehaviour
{
    [Header("Cutscene")]
    [Tooltip("The CutsceneData ScriptableObject asset to play.")]
    [SerializeField] private CutsceneData cutsceneData;

    [Header("Settings")]
    [Tooltip("Tag that is allowed to trigger this cutscene (usually 'Player').")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("If true, the trigger destroys itself after firing once.")]
    [SerializeField] private bool oneShot = true;

    [Tooltip("If true, the cutscene plays automatically when the scene starts (ignores collider).")]
    [SerializeField] private bool playOnStart = false;

    private CutsceneManager _manager;
    private bool _hasPlayed;

    private void Start()
    {
        // Find the manager in the scene. Consider caching this in a static singleton
        // if your project grows and you call FindObjectOfType frequently.
        _manager = FindFirstObjectByType<CutsceneManager>();

        if (_manager == null)
            Debug.LogWarning("[CutsceneTrigger] No CutsceneManager found in scene.");

        if (playOnStart)
            TriggerCutscene();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasPlayed) return;
        if (!other.CompareTag(playerTag)) return;

        TriggerCutscene();
    }

    /// <summary>Manually start the cutscene from code or a UnityEvent.</summary>
    public void TriggerCutscene()
    {
        if (_hasPlayed && oneShot) return;
        if (_manager == null || cutsceneData == null) return;

        _hasPlayed = true;
        _manager.PlayCutscene(cutsceneData);

        if (oneShot)
            Destroy(gameObject);   // Remove the trigger so it can't fire again
    }
}