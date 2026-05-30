using UnityEngine;
using TMPro;
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
             "(e.g. a downward arrow). Leave null for a plain white square fallback.")]
    [SerializeField] private Sprite continueSprite;
    [SerializeField] private RuntimeAnimatorController continueAnimatorController;

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

    [Tooltip("The Player GameObject. Must have a PlayerMovement component.")]
    [SerializeField] private GameObject playerObject;

    [Tooltip("Custom font asset for the dialogue text. Leave null to use the default font.")]
    [SerializeField] private TMP_FontAsset customFontAsset;

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
    private MonoBehaviour _playerMovementScript;

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
        // IMPORTANT: Replace "PlayerMovement" with your actual movement script class name.
        if (playerObject != null)
            _playerMovementScript = playerObject.GetComponent<Movement>();

        // Build the dialogue box UI.
        _dialogueBoxUI = DialogueBoxUI.Create(
            camera:           sceneCamera,
            boxSprite:        boxSprite,
            continueSprite:   continueSprite,
            continueAnimatorController: continueAnimatorController,
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
                // ── SCENE ACTION HOOK ─────────────────────────────────────────
                // Add your own GameObject/scene manipulation calls here.
                // Call AdvanceStep() when the action is finished:
                //   • immediately for instant changes
                //   • at the end of a coroutine for animated/timed changes
                //
                // Branch on step.actionLabel to dispatch different actions:
                //   if (step.actionLabel == "OpenGate") { ... AdvanceStep(); }
                // ─────────────────────────────────────────────────────────────

                Debug.Log($"[CutsceneManager] Scene action: {step.actionLabel}");

                // TODO: Insert your scene action logic here, then call AdvanceStep().
                AdvanceStep();
                break;
        }
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
        _cutsceneActive    = false;
        _waitingForAdvance = false;
        _activeCutscene    = null;

        _dialogueBoxUI.Hide();
        SetPlayerMovement(true);
        CutsceneManager.cutsceneActive = false;
        OnCutsceneEnded();
    }

    /// <summary>
    /// Called when a cutscene finishes. Override in a subclass or add a
    /// UnityEvent field here for Inspector-driven callbacks.
    /// </summary>
    protected virtual void OnCutsceneEnded()
    {
        Debug.Log("[CutsceneManager] Cutscene ended.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Player Movement
    // ─────────────────────────────────────────────────────────────────────────

    private void SetPlayerMovement(bool enabled)
    {
        if (_playerMovementScript != null)
            _playerMovementScript.enabled = enabled;
            Debug.Log(_playerMovementScript.enabled);
    }
}