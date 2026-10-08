using System.Collections;
using UnityEngine;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
// SceneTarget
// Maps a string ID (used in CutsceneData steps) to a scene object.
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class SceneTarget
{
    public string id;
    public GameObject target;
}

// ─────────────────────────────────────────────────────────────────────────────
// CutsceneManager
//
// Place one of these in every scene that needs cutscenes.
// Call PlayCutscene(CutsceneData) from any trigger, event, or start logic.
//
// The dialogue box is a World Space canvas parented to the camera, sized in
// Unity world units using your project's pixels-per-unit value. All Inspector
// fields that accept pixels are converted to world units internally — use the
// same pixel values you'd use in your sprite importer.
// ─────────────────────────────────────────────────────────────────────────────
public class CutsceneManager : MonoBehaviour
{
    // ── Inspector – Sprites ───────────────────────────────────────────────────
    [Header("Dialogue Box Sprites")]
    [Tooltip("Background sprite for the dialogue panel. " +
             "Its native pixel size divided by Pixels Per Unit determines the box's world size. " +
             "A 9-sliced sprite scales without distortion.")]
    [SerializeField] private Sprite boxSprite;

    [Tooltip("Small icon shown in the bottom-right corner when waiting for player input " +
             "(e.g. a downward arrow). Used as the static fallback when no animation frames are set.")]
    [SerializeField] private Sprite continueSprite;

    [Tooltip("All frames of the continue indicator animation in playback order. " +
             "Slice your spritesheet in the Sprite Editor then drag every resulting sprite here. " +
             "Leave empty for a static (non-animated) indicator.")]
    [SerializeField] private Sprite[] continueAnimFrames;

    [Tooltip("Playback speed of the continue indicator animation in frames per second (default 8).")]
    [SerializeField] private float continueAnimFPS = 8f;

    // ── Inspector – Speaker Portraits ─────────────────────────────────────────
    [Header("Speakers")]
    [Tooltip("ScriptableObject mapping speaker names to portrait sprites. " +
             "Create via Assets ▸ Create ▸ Cutscene ▸ Speaker Roster.")]
    [SerializeField] private SpeakerRoster speakerRoster;

    // ── Inspector – Scale ─────────────────────────────────────────────────────
    [Header("Scale")]
    [Tooltip("Must match your project's Pixels Per Unit import setting (e.g. 64). " +
             "All pixel measurements below are divided by this value to get world units.")]
    [SerializeField] private float pixelsPerUnit = 64f;

    // ── Inspector – Layout (all values in pixels, converted to world units) ───
    [Header("Layout  (pixels — converted to world units via Pixels Per Unit)")]
    [Tooltip("Width of the dialogue box in pixels (e.g. 512 for a 512-wide sprite).")]
    [SerializeField] private float boxWidthPx = 512f;

    [Tooltip("Height of the dialogue box in pixels (e.g. 128 for a 128-tall sprite).")]
    [SerializeField] private float boxHeightPx = 128f;

    [Tooltip("Width and height of the square speaker portrait cell in pixels.")]
    [SerializeField] private float portraitSizePx = 128f;

    [Tooltip("Distance from the camera's bottom edge to the bottom of the dialogue box, in pixels.")]
    [SerializeField] private float bottomPaddingPx = 8f;

    [Tooltip("Inner padding between the content area and the box edges, in pixels.")]
    [SerializeField] private float contentPaddingPx = 4f;

    // ── Inspector – Typing ────────────────────────────────────────────────────
    [Header("Typing")]
    [Tooltip("Seconds between each revealed character. Lower = faster.")]
    [SerializeField] private float charDelay = 0.04f;

    // ── Inspector – Camera & Player ───────────────────────────────────────────
    [Header("References")]
    [Tooltip("The scene camera. The dialogue canvas is parented here so it follows camera movement. " +
             "Leave null to auto-find Camera.main at startup.")]
    [SerializeField] private Camera sceneCamera;

    [Tooltip("The Player GameObject. Must have a Movement component.")]
    [SerializeField] private GameObject playerObject;

    [Tooltip("Custom font asset for the dialogue text. Leave null to use the default font.")]
    [SerializeField] private TMP_FontAsset customFontAsset;

    [Tooltip("Optional. Your camera-follow script, if you have one. It is disabled for the " +
             "duration of a cutscene so it doesn't fight the MoveCamera action, then re-enabled at the end.")]
    [SerializeField] private MonoBehaviour cameraFollowScript;

    // ── Inspector – Scene Targets ─────────────────────────────────────────────
    [Header("Scene Targets")]
    [Tooltip("Maps the 'Target Id' strings used in cutscene steps to scene objects " +
             "(gates, camera focus points, etc). 'Player' is built in and doesn't need an entry.")]
    [SerializeField] private SceneTarget[] sceneTargets;

    // ── Inspector – Rendering ─────────────────────────────────────────────────
    [Header("Rendering")]
    [Tooltip("Canvas sorting order. Set higher than your world sprites so the box draws on top.")]
    [SerializeField] private int sortingOrder = 100;
    public static bool cutsceneActive = false;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private DialogueBoxUI _dialogueBoxUI;
    private CutsceneData  _activeCutscene;
    private int           _currentStepIndex;
    private bool          _cutsceneActive;
    private bool          _waitingForAdvance;
    private Movement      _playerMovement;
    private Coroutine     _actionRoutine;

    private const string PlayerId = "Player";

    // ─────────────────────────────────────────────────────────────────────────
    // Unity Messages
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Fall back to Camera.main if no camera was assigned in the Inspector.
        if (sceneCamera == null)
            sceneCamera = Camera.main;

        if (sceneCamera == null)
            Debug.LogError("[CutsceneManager] No camera found. Assign one in the Inspector " +
                           "or tag your camera as MainCamera.");

        // Cache the player movement script so we don't search every frame.
        if (playerObject != null)
            _playerMovement = playerObject.GetComponent<Movement>();

        // Build the dialogue box UI.
        _dialogueBoxUI = DialogueBoxUI.Create(
            camera:           sceneCamera,
            boxSprite:        boxSprite,
            continueSprite:            continueSprite,
            continueAnimatorController: null,
            continueAnimFrames:         continueAnimFrames,
            continueAnimFPS:            continueAnimFPS,
            pixelsPerUnit:    pixelsPerUnit,
            bottomPaddingPx:  bottomPaddingPx,
            charDelay:        charDelay,
            boxWidthPx:       boxWidthPx,
            boxHeightPx:      boxHeightPx,
            portraitSizePx:   portraitSizePx,
            contentPaddingPx: contentPaddingPx,
            sortingOrder:     sortingOrder,
            customFontAsset:  customFontAsset);
    }

    private void Update()
    {
        if (!_cutsceneActive) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (_dialogueBoxUI.IsTyping)
            {
                // First Q while typing → reveal full text immediately
                _dialogueBoxUI.RequestSkipTyping();
            }
            else if (_waitingForAdvance)
            {
                // Q after text is fully shown → advance to next step
                _waitingForAdvance = false;
                AdvanceStep();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Begin playing a cutscene from its first step.</summary>
    public void PlayCutscene(CutsceneData cutscene)
    {
        if (cutscene == null || cutscene.steps == null || cutscene.steps.Length == 0)
        {
            Debug.LogWarning("[CutsceneManager] Tried to play a null or empty cutscene.");
            return;
        }

        _activeCutscene    = cutscene;
        _currentStepIndex  = 0;
        _cutsceneActive    = true;
        _waitingForAdvance = false;
        CutsceneManager.cutsceneActive = true;

        if (cameraFollowScript != null)
            cameraFollowScript.enabled = false;

        SetPlayerMovement(false);
        ExecuteStep(_currentStepIndex);
    }

    /// <summary>Immediately end the active cutscene (e.g. from a skip-all button).</summary>
    public void SkipCutscene() => EndCutscene();

    // ─────────────────────────────────────────────────────────────────────────
    // Step Execution
    // ─────────────────────────────────────────────────────────────────────────

    private void AdvanceStep()
    {
        _currentStepIndex++;

        if (_currentStepIndex >= _activeCutscene.steps.Length)
        {
            EndCutscene();
            return;
        }

        ExecuteStep(_currentStepIndex);
    }

    private void ExecuteStep(int index)
    {
        DialogueStep step = _activeCutscene.steps[index];

        switch (step.stepType)
        {
            case DialogueStep.StepType.Dialogue:
                _dialogueBoxUI.ShowDialogue(step, speakerRoster, OnTypingComplete);
                break;

            case DialogueStep.StepType.SceneAction:
                _actionRoutine = StartCoroutine(RunSceneAction(step));
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scene Actions
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator RunSceneAction(DialogueStep step)
    {
        switch (step.actionType)
        {
            case DialogueStep.ActionType.MovePlayer:
                yield return DoMovePlayer(step);
                break;

            case DialogueStep.ActionType.MoveCamera:
                yield return MoveCameraTo(step);
                break;

            case DialogueStep.ActionType.OpenGate:
                DoGate(step, open: true);
                break;

            case DialogueStep.ActionType.CloseGate:
                DoGate(step, open: false);
                break;

            case DialogueStep.ActionType.Custom:
            default:
                Debug.Log($"[CutsceneManager] Custom scene action: {step.actionLabel}");
                yield return OnCustomAction(step);
                break;
        }

        // MoveCamera already waited for its own duration.
        if (step.waitAfterSeconds > 0f)
            yield return new WaitForSeconds(step.waitAfterSeconds);

        _actionRoutine = null;
        AdvanceStep();
    }

    private IEnumerator DoMovePlayer(DialogueStep step)
    {
        if (_playerMovement == null)
        {
            Debug.LogWarning("[CutsceneManager] MovePlayer: no Movement component found on playerObject.");
            yield break;
        }

        yield return _playerMovement.MoveDistance(step.moveDistance);
    }

    // ── Smooth camera move ────────────────────────────────────────────────────
    private IEnumerator MoveCameraTo(DialogueStep step)
    {
        if (sceneCamera == null) yield break;

        GameObject target = ResolveTarget(string.IsNullOrEmpty(step.targetId) ? PlayerId : step.targetId);
        if (target == null) yield break;

        Transform cam   = sceneCamera.transform;
        Vector3   start = cam.position;
        Vector3   end   = new Vector3(target.transform.position.x,
                                      target.transform.position.y,
                                      start.z); // keep camera depth unchanged

        float duration = Mathf.Max(0.01f, step.cameraMoveDuration);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            cam.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        cam.position = end;
    }

    // ── Gates ─────────────────────────────────────────────────────────────────
    private void DoGate(DialogueStep step, bool open)
    {
        GameObject go = ResolveTarget(step.targetId);
        if (go == null) return;

        GateController gate = go.GetComponent<GateController>();
        if (gate == null)
        {
            Debug.LogWarning($"[CutsceneManager] '{go.name}' has no GateController component.");
            return;
        }

        if (open) gate.OpenGate();
        else      gate.CloseGate();
    }

    // ── Custom hook ───────────────────────────────────────────────────────────
    /// <summary>
    /// Override in a subclass to handle ActionType.Custom steps. Branch on
    /// step.actionLabel. Yield inside the coroutine to wait before the cutscene continues.
    /// </summary>
    protected virtual IEnumerator OnCustomAction(DialogueStep step)
    {
        yield break;
    }

    // ── Target lookup ─────────────────────────────────────────────────────────
    private GameObject ResolveTarget(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[CutsceneManager] Scene action needs a Target Id but none was set.");
            return null;
        }

        if (id == PlayerId && playerObject != null)
            return playerObject;

        if (sceneTargets != null)
        {
            foreach (SceneTarget st in sceneTargets)
                if (st != null && st.id == id && st.target != null)
                    return st.target;
        }

        GameObject found = GameObject.Find(id);
        if (found == null)
            Debug.LogWarning($"[CutsceneManager] No scene target found for id '{id}'. " +
                             "Add it to Scene Targets or check the spelling.");
        return found;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Callbacks
    // ─────────────────────────────────────────────────────────────────────────

    private void OnTypingComplete() => _waitingForAdvance = true;

    // ─────────────────────────────────────────────────────────────────────────
    // Cleanup
    // ─────────────────────────────────────────────────────────────────────────

    private void EndCutscene()
    {
        if (_actionRoutine != null)
        {
            StopCoroutine(_actionRoutine);
            _actionRoutine = null;
        }

        _cutsceneActive    = false;
        _waitingForAdvance = false;
        _activeCutscene    = null;

        _dialogueBoxUI.Hide();
        SetPlayerMovement(true);

        if (cameraFollowScript != null)
            cameraFollowScript.enabled = true;

        CutsceneManager.cutsceneActive = false;
        OnCutsceneEnded();
    }

    /// <summary>
    /// Called when a cutscene finishes. Override in a subclass or add a
    /// UnityEvent field here for Inspector-driven callbacks.
    /// </summary>
    protected virtual void OnCutsceneEnded()
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if(currentSceneName == "Opening Cutscene")
        {
            // Load the next scene after the opening cutscene ends
            UnityEngine.SceneManagement.SceneManager.LoadScene("Room 1");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Player Movement
    // ─────────────────────────────────────────────────────────────────────────

    private void SetPlayerMovement(bool enabled)
    {
        if (_playerMovement != null)
            _playerMovement.enabled = enabled;
    }
}