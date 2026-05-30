using UnityEngine;

public class SyncLightCamera : MonoBehaviour
{
    public Camera mainCamera;
    public Camera lightCamera;

    void LateUpdate()
    {
        if (mainCamera && lightCamera)
        {
            lightCamera.transform.position = mainCamera.transform.position;
            lightCamera.orthographicSize = mainCamera.orthographicSize;
        }
    }
}