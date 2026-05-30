using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TabCursor : MonoBehaviour
{
    public RectTransform cursor;          // The square highlight
    public List<RectTransform> menuItems; // Assign in Inspector
    public List<float> menuItemScales; // Assign in Inspector
    public List<float> menuItemHeights; // Assign in Inspector
    public List<float> menuItemWidths; // Assign in Inspector
    public GameObject inventoryTabContent; // Assign in Inspector
    public GameObject mapTabContent; // Assign in Inspector
    public GameObject questsTabContent; // Assign in Inspector
    public GameObject journalsTabContent; // Assign in Inspector
    public GameObject settingsTabContent; // Assign in Inspector
    private GameObject currentTabContent;
    private int currentIndex = 0;

    void Start()
    {
        currentTabContent = inventoryTabContent;
        UpdateCursorPosition();                   
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentIndex = (currentIndex + 1) % menuItems.Count;
            UpdateCursorPosition();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            currentIndex = (currentIndex - 1 + menuItems.Count) % menuItems.Count;
            UpdateCursorPosition();
        }
    }

    void UpdateCursorPosition()
    {
        // Snap cursor to the selected item
        cursor.position = menuItems[currentIndex].position + new Vector3(menuItemWidths[currentIndex], menuItemHeights[currentIndex], 0);
        cursor.localScale = new Vector3(menuItemScales[currentIndex], menuItemScales[currentIndex], 1);
        // OPTIONAL: Match size to target
        cursor.sizeDelta = menuItems[currentIndex].sizeDelta;

        changeTabContent(currentIndex);
    }

    void changeTabContent(int index)
    {
        // Implement tab content change logic here
        switch(index)
        {
            case 0:
                // Show first tab content
                currentTabContent.SetActive(false);
                inventoryTabContent.SetActive(true);
                currentTabContent = inventoryTabContent;
                break;
            case 1:
                // Show second tab content
                currentTabContent.SetActive(false);
                mapTabContent.SetActive(true);
                currentTabContent = mapTabContent;
                break;
            case 2:
                // Show third tab content
                currentTabContent.SetActive(false);
                questsTabContent.SetActive(true);
                currentTabContent = questsTabContent;
                break;
            case 3:
                // Show third tab content
                currentTabContent.SetActive(false);
                journalsTabContent.SetActive(true);
                currentTabContent = journalsTabContent;
                break;
            case 4:
                // Show third tab content
                currentTabContent.SetActive(false);
                settingsTabContent.SetActive(true);
                currentTabContent = settingsTabContent;
                break;
            default:
                break;
        }
    }
}