using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Data structure: scene → item type → item ID → collected
    private Dictionary<string, Dictionary<string, Dictionary<string, bool>>> sceneData
        = new Dictionary<string, Dictionary<string, Dictionary<string, bool>>>();


    public string entranceDirection = "none";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- Public API ---- //

    /// <summary>
    /// Mark an item as collected/opened in the given scene.
    /// </summary>
    public void SetCollected(string sceneName, string itemType, string itemID, bool collected = true)
    {
        if (!sceneData.ContainsKey(sceneName))
            sceneData[sceneName] = new Dictionary<string, Dictionary<string, bool>>();

        if (!sceneData[sceneName].ContainsKey(itemType))
            sceneData[sceneName][itemType] = new Dictionary<string, bool>();

        sceneData[sceneName][itemType][itemID] = collected;
    }

    /// <summary>
    /// Check if an item is collected/opened in the given scene.
    /// Returns false if not found.
    /// </summary>
    public bool IsCollected(string sceneName, string itemType, string itemID)
    {
        if (!sceneData.ContainsKey(sceneName)) return false;
        if (!sceneData[sceneName].ContainsKey(itemType)) return false;
        if (!sceneData[sceneName][itemType].ContainsKey(itemID)) return false;

        return sceneData[sceneName][itemType][itemID];
    }

    /// <summary>
    /// Removes all stored data for a given scene.
    /// </summary>
    public void ClearSceneData(string sceneName)
    {
        if (sceneData.ContainsKey(sceneName))
            sceneData.Remove(sceneName);
    }

    /// <summary>
    /// Clears all game state data.
    /// </summary>
    public void ClearAllData()
    {
        sceneData.Clear();
    }

    /// <summary>
    /// Returns a snapshot of all data (for debugging or save systems).
    /// </summary>
    public Dictionary<string, Dictionary<string, Dictionary<string, bool>>> GetAllData()
    {
        return sceneData;
    }
}
