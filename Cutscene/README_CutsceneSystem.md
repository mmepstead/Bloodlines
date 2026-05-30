# Cutscene / Dialogue System — Setup Guide

## Files

| File | Purpose |
|---|---|
| `CutsceneData.cs` | ScriptableObject — one asset per cutscene, holds all steps |
| `SpeakerRoster.cs` | ScriptableObject — maps speaker names to portrait sprites (one shared asset) |
| `DialogueBoxUI.cs` | Builds the full UI hierarchy at runtime; no prefab needed |
| `CutsceneManager.cs` | Scene singleton — drives playback, handles input, controls player |
| `CutsceneTrigger.cs` | Helper — fires a cutscene on collision or from code |

---

## 1. Scene Setup (no UI to build by hand)

1. Create an empty GameObject called **CutsceneManager**.
2. Attach the **CutsceneManager** component.
3. Fill in the Inspector fields:

| Field | What to assign |
|---|---|
| **Box Sprite** | The dialogue panel background sprite (9-sliced works great) |
| **Continue Sprite** | Small arrow/icon shown when waiting for Q (or leave null for a white square) |
| **Speaker Roster** | Your SpeakerRoster asset (see step 2 below) |
| **Bottom Padding** | Pixels from screen bottom to box (default 30) |
| **Box Width / Height** | Dialogue panel size in pixels (default 450 × 75) |
| **Portrait Size** | Square portrait area width & height (default 75) |
| **Char Delay** | Seconds per character (default 0.04 ≈ 25 chars/sec) |
| **Player Object** | Your Player GameObject (must have a PlayerMovement component) |

> **Player movement class name**: `CutsceneManager.cs` references `PlayerMovement` in `Awake`.
> Change this to your actual movement script class name.

The dialogue box Canvas is created automatically when the scene starts — there is no prefab to set up or hierarchy to build.

---

## 2. Speaker Roster

Create **one** roster asset for your whole project:

1. **Right-click in the Project window** → `Create ▸ Cutscene ▸ Speaker Roster`.
2. Name it `SpeakerRoster` (or anything you like).
3. Add entries — each entry is a **Speaker Name** + **Portrait** sprite (75×75 recommended).
4. Assign the asset to the **Speaker Roster** field on CutsceneManager.

Speaker names in the roster must match the `speakerName` field in your dialogue steps **exactly** (case-sensitive). If no match is found, the name appears as text in the portrait area.

---

## 3. Creating a Cutscene

1. **Right-click in the Project window** → `Create ▸ Cutscene ▸ Cutscene Data`.
2. Name it (e.g. `Intro_Cutscene`).
3. Add steps in the Inspector:

### Dialogue step
- `Step Type` → `Dialogue`
- `Speaker Name` → e.g. `Guard` (must match an entry in your SpeakerRoster to get a portrait)
- `Dialogue Text` → your line of text

### Scene Action step
- `Step Type` → `Scene Action`
- `Action Label` → a readable tag, e.g. `OpenGate`
- Then in `CutsceneManager.cs` inside the `SceneAction` case, add:
  ```csharp
  if (step.actionLabel == "OpenGate")
  {
      gateObject.SetActive(false);
      AdvanceStep(); // always call this when the action is done
  }
  ```

---

## 4. Triggering a Cutscene

### Via Collision
1. Add a `BoxCollider2D` (set **Is Trigger = true**) to a GameObject.
2. Attach **CutsceneTrigger**.
3. Assign the `Cutscene Data` asset.

### Via Code
```csharp
cutsceneManager.PlayCutscene(myCutsceneData);
```

### On Scene Start
Enable **Play On Start** on a `CutsceneTrigger`.

---

## 5. Controls

| Key | While typing | Text fully shown |
|---|---|---|
| **Q** | Skip to full text | Advance to next step |

---

## 6. Extension Points

- **Async scene actions** — don't call `AdvanceStep()` immediately; call it at the end of a coroutine after the animation/tween finishes.
- **Skip entire cutscene** — call `cutsceneManager.SkipCutscene()` from a UI button or input check.
- **Cutscene-end callback** — override `OnCutsceneEnded()` in a subclass, or add a `UnityEvent` field.
- **Voice acting** — add an `AudioClip` field to `DialogueStep` and play it at the start of `ShowDialogue`.
- **Typewriter sound** — add an `AudioSource` to the canvas root and play a tick each character in `TypeText`.
- **Box sizing at runtime** — all size/padding parameters are on `CutsceneManager`; change them in the Inspector or set them from code before the first cutscene plays.
