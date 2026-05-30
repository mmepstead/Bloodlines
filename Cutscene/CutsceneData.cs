using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// DialogueStep
// A single step in a cutscene: either a line of dialogue or a scene-action
// hook. Speaker portraits are resolved by name via SpeakerRoster — there is
// no per-step portrait field.
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class DialogueStep
{
    public enum StepType { Dialogue, SceneAction }

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
    [Tooltip("Human-readable label used to identify this action in CutsceneManager.")]
    public string actionLabel = "Scene Action";
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
