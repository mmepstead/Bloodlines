using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// SpeakerEntry
// One row in the roster: a name and the portrait sprite for that speaker.
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class SpeakerEntry
{
    [Tooltip("Must match the speakerName field used in DialogueStep exactly (case-sensitive).")]
    public string speakerName;

    [Tooltip("75×75 portrait sprite shown in the dialogue box image area.")]
    public Sprite portrait;
}

// ─────────────────────────────────────────────────────────────────────────────
// SpeakerRoster  –  ScriptableObject asset (one shared across the whole project)
//
// Create via:  Assets ▸ Create ▸ Cutscene ▸ Speaker Roster
//
// Assign this asset to CutsceneManager. All cutscenes in the scene share it.
// If a speaker has no entry here their name appears as text in the portrait
// area instead.
// ─────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "SpeakerRoster", menuName = "Cutscene/Speaker Roster", order = 1)]
public class SpeakerRoster : ScriptableObject
{
    [SerializeField] private SpeakerEntry[] speakers;

    // Built on first lookup so repeated queries are O(1).
    private Dictionary<string, Sprite> _lookup;

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, Sprite>(System.StringComparer.Ordinal);
        if (speakers == null) return;
        foreach (var entry in speakers)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.speakerName))
                _lookup[entry.speakerName] = entry.portrait;
        }
    }

    /// <summary>
    /// Returns the portrait sprite for <paramref name="speakerName"/>, or
    /// <c>null</c> if the name has no entry in the roster.
    /// </summary>
    public Sprite GetPortrait(string speakerName)
    {
        if (_lookup == null) BuildLookup();
        _lookup.TryGetValue(speakerName, out Sprite portrait);
        return portrait;
    }

    // Re-build the lookup whenever the asset is modified in the Editor so
    // play-mode picks up changes without a domain reload.
    private void OnValidate() => _lookup = null;
}
