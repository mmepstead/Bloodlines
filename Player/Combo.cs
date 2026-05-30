using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
public class Combo : MonoBehaviour {
        public GameObject comboBurstPrefab;
    public SpriteRenderer renderer;
    int comboLevel = 0;
    int comboTimer = 600;
    int nextComboLevelHits = 3;

    public void Update()
    {
        if(comboTimer > 0) 
        {
            comboTimer -= 1;
        }
        else if(comboTimer == 0)
        {
            breakCombo();
        }
    }
    public void extendCombo() 
    {
        comboTimer = 600;
        if(comboLevel == 0)
        {
            comboLevel = 1;
        }
        else
        {
            nextComboLevelHits -= 1;
            if(nextComboLevelHits == 0)
            {
                comboLevel += 1;
                if(comboLevel == 5) 
                {
                    PlayerData.currentHealth += PlayerData.currentHealth < PlayerData.maxHealth ? 1 : 0;
                }
                nextComboLevelHits = 3;
            }
        }
        // Instantiate combo burst prefab
        Instantiate(comboBurstPrefab, transform.position, Quaternion.identity, gameObject.transform);
        changeSprite();
    }
    public void breakCombo()
    {
        comboLevel = 0;
        changeSprite();
    }
    public void changeSprite()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Gothic/UI/Combo");
        if (comboLevel == 1)
        {
            renderer.sprite = sprites[0];
        }
        else if (comboLevel == 2)
        {
            renderer.sprite = sprites[1];
        }
        else if (comboLevel == 3)
        {
            renderer.sprite = sprites[2];
        }
        else if (comboLevel == 4)
        {
            renderer.sprite = sprites[3];
        }
        else if (comboLevel > 4)
        {
            renderer.sprite = sprites[4];
        }
        else
        {
            renderer.sprite = null;
        }
    }

}
