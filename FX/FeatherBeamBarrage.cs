using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns and manages multiple FeatherBeam prefabs in a coordinated pattern.
/// Creates four beams (Up, Down, Left, Right) evenly spaced around the enemy.
/// </summary>
public class FeatherBeamBarrage : MonoBehaviour
{
    [Header("Prefab & Setup")]
    [SerializeField] private GameObject featherBeamPrefab;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float horizontalSpacing = 2f;

    [Header("Spawning")]
    [SerializeField] private bool autoTriggerOnStart = false;
    [SerializeField] private float delayBetweenBeams = 0f;

    [Header("Cleanup")]
    [SerializeField] private bool destroyBeamsAfterComplete = true;
    [SerializeField] private float cleanupDelay = 0.5f;

    private List<GameObject> activeBeams = new List<GameObject>();
    private List<FeatherBeamController> activeControllers = new List<FeatherBeamController>();

    private void Start()
    {
        if (autoTriggerOnStart)
        {
            FireBarrage();
        }
    }

    /// <summary>
    /// Spawn and fire the four-beam barrage
    /// </summary>
    public void FireBarrage()
    {
        if (featherBeamPrefab == null)
        {
            Debug.LogError("FeatherBeamBarrage: Prefab not assigned!", gameObject);
            return;
        }

        // Clear any existing beams
        ClearBeams();

        // Determine which side the player is on so the beams spawn towards them
        // instead of always to the enemy's left.
        float sign = IsPlayerToRight() ? 1f : -1f;

        // Define beam positions relative to this enemy
        // Arranged in a cross pattern: center positions for each cardinal direction
        Vector3[] beamPositions = new Vector3[]
        {
            new Vector3(sign * 2f * horizontalSpacing, yOffset, 0f), // Left beam
            new Vector3(sign * 3f * horizontalSpacing, yOffset, 0f),  // Right beam
            new Vector3(sign * horizontalSpacing, yOffset, 0f),                 // Up beam (center-left)
            // new Vector3(horizontalSpacing, yOffset, 0f)                  // Down beam (center-right)
        };

        FeatherBeamController.CardinalDirection[] directions = new FeatherBeamController.CardinalDirection[]
        {
            FeatherBeamController.CardinalDirection.Up,
            FeatherBeamController.CardinalDirection.Up,
            FeatherBeamController.CardinalDirection.Up,
            // FeatherBeamController.CardinalDirection.Up
        };

        // Spawn each beam
        for (int i = 0; i < 3; i++)
        {
            Vector3 worldPosition = transform.position + beamPositions[i];
            GameObject beamInstance = Instantiate(featherBeamPrefab, worldPosition, Quaternion.identity);
            
            FeatherBeamController controller = beamInstance.GetComponent<FeatherBeamController>();
            if (controller == null)
            {
                Debug.LogError("FeatherBeamBarrage: Prefab does not have FeatherBeamController component!", beamInstance);
                Destroy(beamInstance);
                continue;
            }

            // Set direction via reflection or add a public setter method
            SetBeamDirection(controller, directions[i]);

            activeBeams.Add(beamInstance);
            activeControllers.Add(controller);

            // Stagger the beam starts if delay is set
            // if (delayBetweenBeams > 0f)
            // {
            //     float delay = i * delayBetweenBeams;
            //     StartCoroutine(DelayedBeamStart(controller, delay));
            // }
            // else
            // {
            //     controller.StartBeam();
            // }
        }


        // Schedule cleanup if enabled
        if (destroyBeamsAfterComplete && activeControllers.Count > 0)
        {
            float maxEmissionTime = GetMaxEmissionTime();
            float cleanupTime = maxEmissionTime + cleanupDelay;
            Invoke(nameof(ClearBeams), cleanupTime);
        }
    }

    /// <summary>
    /// Returns true if the Player is to the right of this enemy (on the positive-x side),
    /// so beam spawn positions can be mirrored towards them. Defaults to right if no
    /// player is found in the scene.
    /// </summary>
    private bool IsPlayerToRight()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            return true;
        }
        return player.transform.position.x > transform.position.x;
    }

    /// <summary>
    /// Set the direction of a beam controller via reflection
    /// </summary>
    private void SetBeamDirection(FeatherBeamController controller, FeatherBeamController.CardinalDirection direction)
    {
        var field = typeof(FeatherBeamController).GetField("direction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(controller, direction);
        }
        else
        {
            Debug.LogWarning("Could not set beam direction via reflection", controller.gameObject);
        }
    }

    /// <summary>
    /// Get the maximum emission time across all parameters
    /// </summary>
    private float GetMaxEmissionTime()
    {
        var field = typeof(FeatherBeamController).GetField("emissionDuration",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null && activeControllers.Count > 0)
        {
            float emissionDuration = (float)field.GetValue(activeControllers[0]);
            var delayField = typeof(FeatherBeamController).GetField("delayBeforeEruption",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (delayField != null)
            {
                float delay = (float)delayField.GetValue(activeControllers[0]);
                return delay + emissionDuration;
            }
            return emissionDuration;
        }
        return 5f; // Default fallback
    }

    /// <summary>
    /// Start a beam after a delay
    /// </summary>
    private System.Collections.IEnumerator DelayedBeamStart(FeatherBeamController controller, float delay)
    {
        yield return new WaitForSeconds(delay);
        controller.StartBeam();
    }

    /// <summary>
    /// Stop all active beams immediately
    /// </summary>
    public void StopAllBeams()
    {
        foreach (var controller in activeControllers)
        {
            if (controller != null)
            {
                controller.StopBeam();
            }
        }
    }

    /// <summary>
    /// Clear all spawned beams
    /// </summary>
    public void ClearBeams()
    {
        CancelInvoke(nameof(ClearBeams)); // Cancel any pending cleanup

        foreach (var beam in activeBeams)
        {
            if (beam != null)
            {
                Destroy(beam);
            }
        }

        activeBeams.Clear();
        activeControllers.Clear();
    }

    /// <summary>
    /// Get list of active beam controllers for external manipulation
    /// </summary>
    public List<FeatherBeamController> GetActiveControllers()
    {
        return new List<FeatherBeamController>(activeControllers);
    }

    /// <summary>
    /// Manually trigger a barrage from code (useful for animations, events, etc.)
    /// </summary>
    public static void TriggerBarrageOnEnemy(GameObject enemyObject, GameObject featherBeamPrefab, 
        float yOffset = 0f, float horizontalSpacing = 2f)
    {
        var barrage = enemyObject.GetComponent<FeatherBeamBarrage>();
        if (barrage != null)
        {
            barrage.FireBarrage();
        }
        else
        {
            Debug.LogWarning("Enemy does not have FeatherBeamBarrage component", enemyObject);
        }
    }
}