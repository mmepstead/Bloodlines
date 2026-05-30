using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BoxCursor : MonoBehaviour
{
    public CursorableBox defaultBox; // The square highlight
    public CursorableBox currentBox; // The square highlight
    public RectTransform cursor;          // The square highlight
    public Sprite defaultCursorSprite; // Default sprite for the cursor

    void Start()
    {
        currentBox = defaultBox;
        UpdateCursorPosition();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentBox = currentBox.down != null ? currentBox.down.GetComponent<CursorableBox>() : currentBox;
            UpdateCursorPosition();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentBox = currentBox.up != null ? currentBox.up.GetComponent<CursorableBox>() : currentBox;
            UpdateCursorPosition();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentBox = currentBox.left != null ? currentBox.left.GetComponent<CursorableBox>() : currentBox;
            UpdateCursorPosition();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentBox = currentBox.right != null ? currentBox.right.GetComponent<CursorableBox>() : currentBox;
            UpdateCursorPosition();
        }
    }

    void UpdateCursorPosition()
    {
        // Snap cursor to the selected item
        cursor.position = currentBox.self.position;
        cursor.localScale = new Vector3(currentBox.cursorScaleX, currentBox.cursorScaleY, 1);
        cursor.gameObject.GetComponent<Image>().sprite = currentBox.cursorOverride != null ? currentBox.cursorOverride : defaultCursorSprite;
        // OPTIONAL: Match size to target
        cursor.sizeDelta = currentBox.self.sizeDelta;
    }
}