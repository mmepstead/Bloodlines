using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player GameObject (with a Movement script).")]
    public GameObject player;

    [Tooltip("The fade screen GameObject (must have fadeOut() and fadeIn() methods).")]
    public GameObject fade;

    [Header("Transition Settings")]
    [Tooltip("Name of the scene to load.")]
    public string sceneName;

    [Tooltip("Direction the player will enter in the next scene.")]
    public string enterDirection;

    [Tooltip("Delay before loading the new scene after fade out starts.")]
    public float fadeDelay = 1.5f;

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;

        if (other.gameObject == player)
        {
            StartCoroutine(HandleSceneTransition());
        }
    }

    private IEnumerator HandleSceneTransition()
    {
        isTransitioning = true;

        // Disable movement
        var movement = player.GetComponent<MonoBehaviour>();
        if (movement != null && movement.GetType().Name == "Movement")
            movement.enabled = false;

        // Trigger fade out
        if (fade != null)
        {
            var fadeScript = fade.GetComponent<MonoBehaviour>();
            var fadeOutMethod = fadeScript?.GetType().GetMethod("fadeOut");
            fadeOutMethod?.Invoke(fadeScript, null);
        }

        // Wait before loading the new scene
        yield return new WaitForSeconds(fadeDelay);
        GameManager.Instance.entranceDirection = enterDirection;
        SceneManager.LoadScene(sceneName);
    }
}
