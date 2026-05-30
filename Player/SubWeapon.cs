using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
public class SubWeapon : MonoBehaviour {
    int selectedSubWeaponIndex = 0;
    List<string> availableSubWeapons = new List<string>{"Stake", "Stomp"};
    public SpriteRenderer weaponSprite;
    void Awake()
    {
    }
    void Update()
    {
        if(selectedSubWeaponIndex != null)
        {
            weaponSprite.sprite = Resources.Load<Sprite>("Gothic/Sub Weapons/"+availableSubWeapons[selectedSubWeaponIndex]);
            if(Input.GetKeyDown(KeyCode.LeftShift))
            {
                selectedSubWeaponIndex = selectedSubWeaponIndex == availableSubWeapons.Count -1 ? 0 : selectedSubWeaponIndex + 1;
            }
        }
        if(Input.GetKeyDown(KeyCode.R))
        {
            useSubWeapon();
        }
    }

    public void useSubWeapon()
    {
        if(availableSubWeapons[selectedSubWeaponIndex] == "Stake")
        {
            GameObject.Find("Player").GetComponent<Movement>().throwStake();
        }
        else if(availableSubWeapons[selectedSubWeaponIndex] == "Stomp")
        {
            GameObject.Find("Player").GetComponent<Movement>().useStomp();
        }
    }
}
