using System.Collections;
using UnityEngine;

public class ImpactManager : MonoBehaviour
{
    public static ImpactManager Instance;

    [Header("Time Freeze Settings")]
    public bool useTimeFreeze = true;
    public float freezeDuration = 0.05f;
    public float slowMoFactor = 0.2f;
    public float slowMoDuration = 0.2f;

    [Header("Camera Shake Settings")]
    public Transform cameraTransform;
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.2f;

    private Vector3 originalCamPos;
    private FollowPlayer followPlayer; // Reference if the camera has FollowPlayer

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            followPlayer = cameraTransform.GetComponent<FollowPlayer>();
    }

    public void DoImpact()
    {
        if (useTimeFreeze)
            StartCoroutine(TimeFreeze());

        StartCoroutine(CameraShake());
    }

    private IEnumerator TimeFreeze()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(freezeDuration);

        Time.timeScale = slowMoFactor;
        yield return new WaitForSecondsRealtime(slowMoDuration);

        Time.timeScale = 1f;
    }

    private IEnumerator CameraShake()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            Vector3 offset = Random.insideUnitCircle * shakeMagnitude;

            if (followPlayer != null)
            {
                // Apply shake offset through FollowPlayer
                followPlayer.ApplyShake(offset);
            }
            else
            {
                // Directly move camera if FollowPlayer is not present
                cameraTransform.localPosition = originalCamPos + offset;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (followPlayer != null)
        {
            followPlayer.ApplyShake(Vector3.zero);
        }
        else
        {
            cameraTransform.localPosition = originalCamPos;
        }
    }
}