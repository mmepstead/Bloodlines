using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// DialogueStep
// A single step in a cutscene: either a line of dialogue or a scene action.
// Speaker portraits are resolved by name via SpeakerRoster.
//
// NOTE: ScriptableObjects can't hold references to scene objects, so scene
// actions refer to objects by a string ID ("targetId"). CutsceneManager
// resolves that ID via its Scene Targets list (or GameObject.Find as fallback).
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class DialogueStep
{
    public enum StepType { Dialogue, SceneAction }

    public enum ActionType
    {
        Custom,      // Falls through to CutsceneManager.OnCustomAction (uses actionLabel)
        MovePlayer,  // Calls Movement.MoveDistance(moveDistance)
        MoveCamera,  // Smoothly pans the camera to targetId (use "Player" to return to player)
        OpenGate,    // Calls GateController.OpenGate() on targetId
        CloseGate    // Calls GateController.CloseGate() on targetId
    }

    [Tooltip("Dialogue = show text in the box. SceneAction = trigger a scene event (no UI shown).")]
    public StepType stepType = StepType.Dialogue;

    [Header("Dialogue Fields (StepType.Dialogue only)")]
    [Tooltip("Must match a name entry in the SpeakerRoster to get a portrait. " +
             "If no match is found the name is shown as text instead.")]
    public string speakerName;

    [Tooltip("The line of dialogue to type out.")]
    [TextArea(2, 6)]
    public string dialogueText;

    [Header("Scene Action Fields (StepType.SceneAction only)")]
    [Tooltip("Which action to run.")]
    public ActionType actionType = ActionType.Custom;

    [Tooltip("Human-readable label. For ActionType.Custom this is passed to CutsceneManager.OnCustomAction.")]
    public string actionLabel = "Scene Action";

    [Tooltip("ID of the scene object to act on (MoveCamera, OpenGate, CloseGate). " +
             "Matches an entry in CutsceneManager's Scene Targets list. " +
             "'Player' is built in. Falls back to GameObject.Find(name) if no entry matches.")]
    public string targetId;

    [Tooltip("MovePlayer: units to move along the X axis. Positive = right, negative = left.")]
    public float moveDistance = 1f;

    [Tooltip("MoveCamera: seconds the camera takes to glide to the target.")]
    public float cameraMoveDuration = 1.5f;

    [Tooltip("MovePlayer / OpenGate / CloseGate: seconds to pause before the next step, " +
             "so the action can finish playing. 0 = advance immediately. " +
             "(MoveCamera always waits for its own duration.)")]
    public float waitAfterSeconds = 0f;
}

// ─────────────────────────────────────────────────────────────────────────────
// CutsceneData  –  ScriptableObject asset (one per cutscene)
//
// Create via:  Assets ▸ Create ▸ Cutscene ▸ Cutscene Data
// ─────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscene/Cutscene Data", order = 0)]
public class CutsceneData : ScriptableObject
{
    [Tooltip("Friendly name used for debugging.")]
    public string cutsceneName = "New Cutscene";

    [Tooltip("All steps in order. Each is either Dialogue or a SceneAction.")]
    public DialogueStep[] steps;
}