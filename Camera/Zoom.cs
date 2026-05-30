using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
public class Zoom : MonoBehaviour
{
    float regularZoom = 3f;
    public IEnumerator startParryZoom()
    {
        // Quickly and smoothly zoom in the camera for parry moment
        float targetSize = 2.5f;
        float speed = 0.03f;
        while (Camera.main.orthographicSize > targetSize)
        {
            Camera.main.orthographicSize -= speed;
            yield return null;
        }
    }

    public IEnumerator endParryZoom()
    {
        // Quickly and smoothly zoom in the camera for parry moment
        float speed = 0.03f;
        while (Camera.main.orthographicSize < regularZoom)
        {
            Camera.main.orthographicSize += speed;
            yield return null;
        }
        Camera.main.orthographicSize = regularZoom;
    }
}