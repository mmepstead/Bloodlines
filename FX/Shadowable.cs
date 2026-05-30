using UnityEngine;
using System;
/// <summary>
/// Attach this to a GameObject with a SpriteRenderer.
/// It will create a shadow object that copies the sprite and animations.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Shadowable : MonoBehaviour
{
    [Header("Shadow Settings")]
    [Tooltip("Offset of the shadow from the parent object.")]
    public Vector2 shadowOffset = new Vector2(0.1f, -0.1f);

    [Tooltip("Opacity of the shadow (0 = invisible, 1 = solid).")]
    [Range(0f, 1f)] public float shadowAlpha = 0.5f;

    private SpriteRenderer parentRenderer;
    private SpriteRenderer shadowRenderer;
    private Transform shadowTransform;
    public bool pulse = false;

    void Awake()
    {
        parentRenderer = GetComponent<SpriteRenderer>();

        // Create shadow object
        GameObject shadowObj = new GameObject(name + "_Shadow");
        shadowObj.transform.SetParent(transform); // same layer as parent
        shadowTransform = shadowObj.transform;

        // Copy sprite renderer
        shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
        if(pulse) shadowRenderer.material = Resources.Load<Material>("Gothic/Materials/Breathing/Shadow Breathing");
        shadowRenderer.sortingLayerID = parentRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = parentRenderer.sortingOrder - 1; // behind

        // Apply initial settings
        shadowRenderer.color = new Color(0f, 0f, 0f, shadowAlpha);
    }

    void LateUpdate()
    {
        if (shadowRenderer == null || parentRenderer == null) return;

        // Copy sprite & flip state
        shadowRenderer.sprite = parentRenderer.sprite;
        shadowRenderer.flipX = parentRenderer.flipX;
        shadowRenderer.flipY = parentRenderer.flipY;

        // Position shadow with offset
        shadowTransform.position = (Vector2)transform.position + shadowOffset;

        // Match scale
        shadowTransform.localScale = new Vector3(Math.Abs(transform.localScale.x), transform.localScale.y, 1f);

        // Match rotation
        shadowTransform.rotation = transform.rotation;
    }
}
