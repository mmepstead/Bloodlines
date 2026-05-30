using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
public class HealthMeter : MonoBehaviour {
    public GameObject heartPrefab;
    public GameObject emptyHeartPrefab;
    List<GameObject> displayedHearts = new List<GameObject>();
    List<GameObject> emptyHearts = new List<GameObject>();
    void Awake()
    {
        addNewEmptyHearts();
    }
    void Update()
    {
        if(emptyHearts.Count < PlayerData.maxHealth) 
        {
            addNewEmptyHearts();
        }
        Transform camera = GameObject.Find("Main Camera").transform;
        int hearts = GameObject.FindGameObjectsWithTag("Heart").Length;
        if(hearts < PlayerData.currentHealth) {
            for(int i = hearts; i < PlayerData.currentHealth; i += 1)
            {
                GameObject newHeart = Instantiate(heartPrefab, new Vector3(camera.position.x + 0.27f*i-4f,camera.position.y+2.35f,-3), Quaternion.identity, GameObject.Find("Health Bar").transform);
                newHeart.GetComponent<Animator>().Play(0, -1,GameObject.Find("Heart(Clone)").GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime);
                displayedHearts.Add(newHeart);
            }
        }
        else if(hearts > PlayerData.currentHealth)
        {
            for(int i = hearts-1; i+1 > PlayerData.currentHealth; i -= 1)
            {
                Destroy(displayedHearts[i]);
                displayedHearts.RemoveAt(i);
            }
        }

    }
    void addNewEmptyHearts()
    {
        //Destroy all existing empty hearts and recreate them
        foreach(GameObject emptyHeart in emptyHearts)
        {
            Destroy(emptyHeart);
        }
        Transform camera = GameObject.Find("Main Camera").transform;
        for(int i = emptyHearts.Count; i < PlayerData.maxHealth; i += 1)
        {
            GameObject emptyHeart = Instantiate(emptyHeartPrefab, new Vector3(camera.position.x + 0.27f*i-4f,camera.position.y + 2.35f,-3), Quaternion.identity, GameObject.Find("Health Bar").transform);
            emptyHearts.Add(emptyHeart);
        }
    }
}
