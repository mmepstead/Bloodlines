using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CursorableBox : MonoBehaviour
{
    public RectTransform self;
    public RectTransform up; // Assign in Inspector
    public RectTransform left; // Assign in Inspector
    public RectTransform right; // Assign in Inspector
    public RectTransform down; // Assign in Inspector
    public float cursorScaleX = 1f;
    public float cursorScaleY = 1f;
    public Sprite cursorOverride = null; // Optional sprite override for cursor
    public Vector3 cursorOffset = Vector3.zero;

    void Start()
    {
    }

    void Update()
    {
    }
}