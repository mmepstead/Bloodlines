using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
// DialogueBoxUI  —  World Space canvas edition
//
// All sizing is expressed in Unity world units (pixels ÷ pixelsPerUnit) so the
// dialogue box matches the scale of your sprites exactly. A 512×128 sprite at
// 64 PPU produces a box that is 8×2 world units — identical to how that sprite
// would appear if placed in the scene directly.
//
// The canvas is parented to the camera and repositioned each frame so it stays
// locked to the bottom of the viewport regardless of camera movement.
//
// Generated hierarchy:
//
//   DialogueCanvas          Canvas (World Space), child of Camera
//   └── DialogueBox         RectTransform — one world-unit = one game unit
//       ├── BoxBackground   Image (boxSprite) — full-stretch, never in layout
//       ├── ContentRow      HorizontalLayoutGroup — portrait slot + text only
//       │   ├── PortraitSlot   LayoutElement (fixed portraitUnits × portraitUnits)
//       │   │   ├── SpeakerImage      Image — stretch-fills slot, preserveAspect
//       │   │   └── SpeakerNameText   TextMeshPro — same rect, shown w/o portrait
//       │   └── DialogueText  TextMeshPro — expands to fill remaining width
//       └── ContinueIndicator  Image — anchored bottom-right, outside layout
// ─────────────────────────────────────────────────────────────────────────────
public class DialogueBoxUI : MonoBehaviour
{
    // ── Parameters stored at Create-time ──────────────────────────────────────
    private float  _charDelay;
    private float  _bottomPaddingUnits; // world-unit offset from camera bottom edge
    private Camera _camera;

    // ── Runtime-built UI references ───────────────────────────────────────────
    private Image           _speakerImage;
    private TextMeshProUGUI _speakerNameText;
    private TextMeshProUGUI _dialogueText;
    private GameObject      _continueIndicator;
    private GameObject      _canvasRoot;
    private RectTransform   _canvasRT;

    // ── Typing state ──────────────────────────────────────────────────────────
    private Coroutine _typingCoroutine;
    private bool      _isTyping;
    private bool      _skipRequested;

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Whether the typewriter coroutine is still running.</summary>
    public bool IsTyping => _isTyping;

    /// <summary>
    /// Build the dialogue canvas and return the ready-to-use DialogueBoxUI.
    /// All pixel parameters are divided by <paramref name="pixelsPerUnit"/> so
    /// the result matches your sprite's native scale.
    /// </summary>
    /// <param name="camera">
    ///     The scene camera. The World Space canvas is parented here so the box
    ///     follows the camera automatically.
    /// </param>
    /// <param name="boxSprite">
    ///     Background sprite for the dialogue panel. The canvas ReferencePixelsPerUnit
    ///     is set to match your sprite's PPU so the image renders at native size.
    ///     Pass null for a transparent background.
    /// </param>
    /// <param name="continueSprite">
    ///     Icon shown when waiting for player input. Pass null for a white square.
    /// </param>
    /// <param name="pixelsPerUnit">
    ///     Your project's PPU setting (e.g. 64). All pixel measurements are divided
    ///     by this value to get world units.
    /// </param>
    /// <param name="bottomPaddingPx">Pixels from the camera's bottom edge to the box.</param>
    /// <param name="charDelay">Seconds between revealed characters (default 0.04).</param>
    /// <param name="boxWidthPx">Box width in pixels (e.g. 512).</param>
    /// <param name="boxHeightPx">Box height in pixels (e.g. 128).</param>
    /// <param name="portraitSizePx">Portrait cell width and height in pixels (e.g. 128).</param>
    /// <param name="contentPaddingPx">Inner padding between content and box edges in pixels.</param>
    /// <param name="sortingOrder">
    ///     Sorting order of the canvas. Set this higher than your world sprites to
    ///     ensure the box draws on top.
    /// </param>
    public static DialogueBoxUI Create(
        Camera camera,
        Sprite boxSprite,
        Sprite continueSprite    = null,
        RuntimeAnimatorController continueAnimatorController = null,
        float  pixelsPerUnit     = 64f,
        float  bottomPaddingPx   = 8f,
        float  charDelay         = 0.04f,
        float  boxWidthPx        = 512f,
        float  boxHeightPx       = 128f,
        float  portraitSizePx    = 128f,
        float  contentPaddingPx  = 4f,
        int    sortingOrder      = 100,
        TMP_FontAsset customFontAsset = null)
    {
        // Convert every pixel measurement to world units once here.
        // Everything below uses units; nothing below divides again.
        float ppu              = pixelsPerUnit;
        float boxW             = boxWidthPx        / ppu;
        float boxH             = boxHeightPx       / ppu;
        float portraitU        = portraitSizePx    / ppu;
        float padU             = contentPaddingPx  / ppu;
        float bottomPadU       = bottomPaddingPx   / ppu;

        // ── World Space Canvas ────────────────────────────────────────────────
        // Parented to the camera so it moves with it.
        // pixelsPerUnit on the CanvasScaler tells Unity how many canvas pixels
        // equal one world unit — must match your sprite PPU exactly.
        var canvasGO = new GameObject("DialogueCanvas");
        canvasGO.transform.SetParent(camera.transform, false);

        var canvas              = canvasGO.AddComponent<Canvas>();
        canvas.renderMode       = RenderMode.WorldSpace;
        canvas.sortingOrder     = sortingOrder;
        canvas.sortingLayerName   = "UI"; // Make sure you have a "UI" layer in your project
        // ReferencePixelsPerUnit makes TMP font sizes and Image pixel sizes
        // correspond to the same PPU scale as your sprites.
        var scaler                        = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode                = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.referencePixelsPerUnit     = ppu;
        scaler.scaleFactor                = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // The canvas RectTransform needs a size so World Space rendering knows
        // its bounds. We give it the box size; it doesn't need to be larger
        // because we only render one box.
        var canvasRT        = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta  = new Vector2(boxW, boxH);
        canvasRT.localScale = Vector3.one / ppu; // 1 canvas pixel = 1/ppu world units

        // ── DialogueBox — single source of truth for position & size ──────────
        var boxGO = new GameObject("DialogueBox");
        boxGO.transform.SetParent(canvasGO.transform, false);

        var boxRT              = boxGO.AddComponent<RectTransform>();
        boxRT.anchorMin        = new Vector2(0.5f, 0f);
        boxRT.anchorMax        = new Vector2(0.5f, 0f);
        boxRT.pivot            = new Vector2(0.5f, 0f);
        // sizeDelta is in canvas pixels; canvas pixels = world units * ppu
        boxRT.sizeDelta        = new Vector2(boxWidthPx, boxHeightPx);
        // anchoredPosition is also in canvas pixels
        boxRT.anchoredPosition = new Vector2(0f, bottomPaddingPx);

        // ── BoxBackground — Image only, never participates in layout ──────────
        var bgGO  = new GameObject("BoxBackground");
        bgGO.transform.SetParent(boxGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();

        if (boxSprite != null)
        {
            bgImg.sprite = boxSprite;
            bgImg.type   = Image.Type.Sliced;
        }

        var bgRT       = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.pivot     = new Vector2(0.5f, 0.5f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // ── ContentRow — HorizontalLayoutGroup, invisible, layout only ─────────
        // offsetMin/Max are in canvas pixels.
        var rowGO = new GameObject("ContentRow");
        rowGO.transform.SetParent(boxGO.transform, false);

        var rowRT       = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin = Vector2.zero;
        rowRT.anchorMax = Vector2.one;
        rowRT.pivot     = new Vector2(0.5f, 0.5f);
        rowRT.offsetMin = new Vector2(contentPaddingPx,  contentPaddingPx);
        rowRT.offsetMax = new Vector2(-contentPaddingPx, -contentPaddingPx);

        var hlg                    = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.spacing                = 10f;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.padding                = new RectOffset(0, 40, 0, 0);

        // ── PortraitSlot — fixed size via LayoutElement (canvas pixels) ────────
        var slotGO = new GameObject("PortraitSlot");
        slotGO.transform.SetParent(rowGO.transform, false);
        slotGO.AddComponent<RectTransform>();

        var slotLE             = slotGO.AddComponent<LayoutElement>();
        slotLE.minWidth        = portraitSizePx;
        slotLE.preferredWidth  = portraitSizePx;
        slotLE.minHeight       = portraitSizePx;
        slotLE.preferredHeight = portraitSizePx;
        slotLE.flexibleWidth   = 0f;
        slotLE.flexibleHeight  = 0f;

        // SpeakerImage — stretch-fills slot
        var portraitGO          = new GameObject("SpeakerImage");
        portraitGO.transform.SetParent(slotGO.transform, false);
        var portraitImg         = portraitGO.AddComponent<Image>();
        portraitImg.preserveAspect = true;
        portraitImg.enabled        = false;

        var portraitRT       = portraitGO.GetComponent<RectTransform>();
        portraitRT.anchorMin = Vector2.zero;
        portraitRT.anchorMax = Vector2.one;
        portraitRT.offsetMin = Vector2.zero;
        portraitRT.offsetMax = Vector2.zero;

        // SpeakerNameText — same rect, visible when no portrait
        var nameGO  = new GameObject("SpeakerNameText");
        nameGO.transform.SetParent(slotGO.transform, false);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.alignment = TextAlignmentOptions.Center;
        // Font size in canvas pixels; at 64 PPU a size of 14 canvas-px ≈ 14/64 ≈ 0.22 world units
        nameTMP.fontSize  = 14f;
        nameTMP.text      = string.Empty;

        var nameRT       = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = Vector2.zero;
        nameRT.anchorMax = Vector2.one;
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;

        // ── DialogueText — fills remaining width ──────────────────────────────
        var textGO  = new GameObject("DialogueText");
        textGO.transform.SetParent(rowGO.transform, false);
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.alignment = TextAlignmentOptions.MidlineLeft;
        textTMP.fontSize  = 12f;
        textTMP.text      = string.Empty;
        if(customFontAsset != null)
        {
            textTMP.font = customFontAsset;
        }
        var textLE           = textGO.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1f;

        // ── ContinueIndicator — anchored corner, outside ContentRow ───────────
        var indicatorGO  = new GameObject("ContinueIndicator");
        indicatorGO.transform.SetParent(boxGO.transform, false);
        var indicatorImg         = indicatorGO.AddComponent<Image>();
        indicatorImg.sprite      = continueSprite;
        indicatorImg.SetNativeSize();
        var animator = indicatorGO.AddComponent<Animator>();
        animator.runtimeAnimatorController = continueAnimatorController;
        indicatorGO.SetActive(false);


        var indicatorRT              = indicatorGO.GetComponent<RectTransform>();
        indicatorRT.localScale        = new Vector3(0.2f,0.2f,1); // counteract canvas scale so sprite pixels = world units
        indicatorRT.anchorMin        = new Vector2(1f, 0f);
        indicatorRT.anchorMax        = new Vector2(1f, 0f);
        indicatorRT.pivot            = new Vector2(1f, 0f);
        indicatorRT.anchoredPosition = new Vector2(-40, 20);

        // ── Wire DialogueBoxUI ────────────────────────────────────────────────
        var ui                  = boxGO.AddComponent<DialogueBoxUI>();
        ui._speakerImage        = portraitImg;
        ui._speakerNameText     = nameTMP;
        ui._dialogueText        = textTMP;
        ui._continueIndicator   = indicatorGO;
        ui._charDelay           = charDelay;
        ui._canvasRoot          = canvasGO;
        ui._canvasRT            = canvasRT;
        ui._camera              = camera;
        ui._bottomPaddingUnits  = bottomPadU;

        canvasGO.SetActive(false);
        return ui;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Unity messages
    // ─────────────────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        // Keep the canvas snapped to the bottom-centre of the camera's viewport
        // every frame so it follows camera movement and zoom correctly.
        if (_camera == null || !_canvasRoot.activeSelf) return;
        SnapToCamera();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public runtime methods
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reveal the dialogue box and begin typing the step's text.
    /// </summary>
    public void ShowDialogue(DialogueStep step, SpeakerRoster roster, System.Action onTypingComplete)
    {
        _canvasRoot.SetActive(true);
        SnapToCamera();
        _continueIndicator.SetActive(false);

        Sprite portrait = roster != null ? roster.GetPortrait(step.speakerName) : null;

        if (portrait != null)
        {
            _speakerImage.sprite  = portrait;
            _speakerImage.enabled = true;
            _speakerNameText.text = string.Empty;
        }
        else
        {
            _speakerImage.enabled = false;
            _speakerNameText.text = step.speakerName;
        }

        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _typingCoroutine = StartCoroutine(TypeText(step.dialogueText, onTypingComplete));
    }

    /// <summary>Instantly reveal the full line while typing is in progress.</summary>
    public void RequestSkipTyping() => _skipRequested = true;

    /// <summary>Hide the canvas and abort any in-progress typing.</summary>
    public void Hide()
    {
        if (_canvasRoot != null)
            _canvasRoot.SetActive(false);

        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _isTyping = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Position the canvas so its bottom edge sits <see cref="_bottomPaddingUnits"/>
    /// above the camera's bottom edge in world space.
    ///
    /// ViewportToWorldPoint(0,0,0) gives the bottom-left of the camera frustum
    /// at depth 0 (the camera's near plane, which is where world-space UI sits
    /// when z = camera.nearClipPlane). Horizontally we centre it on the camera.
    /// </summary>
    private void SnapToCamera()
    {
        // Bottom-centre of the viewport in world space
        Vector3 bottomCentre = _camera.ViewportToWorldPoint(
            new Vector3(0.5f, 0f, _camera.nearClipPlane));
        float worldZ = _camera.transform.position.z + 1;
        // Move up by padding. The canvas localScale converts world units to
        // canvas pixels, so we work in world units here and let the transform do it.
        _canvasRoot.transform.position = new Vector3(
            bottomCentre.x,
            bottomCentre.y + _bottomPaddingUnits,
            worldZ);
    }

    private IEnumerator TypeText(string fullText, System.Action onComplete)
    {
        _isTyping          = true;
        _skipRequested     = false;
        _dialogueText.text = string.Empty;

        for (int i = 0; i < fullText.Length; i++)
        {
            if (_skipRequested)
            {
                _dialogueText.text = fullText;
                break;
            }

            _dialogueText.text += fullText[i];
            yield return new WaitForSeconds(_charDelay);
        }

        _isTyping      = false;
        _skipRequested = false;
        _continueIndicator.SetActive(true);
        onComplete?.Invoke();
    }
}