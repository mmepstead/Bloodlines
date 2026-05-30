using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
public class Shield : MonoBehaviour {
    public int health;
    public int maxHealth;
    public int regen;
    public int fullRegen;
    int fullRegenCountdown;
    public float shieldScale = 1;
    public SpriteRenderer baseShieldCore;
    public GameObject shieldImpactPrefab;
    public GameObject shieldImpactPrefabYellow;

    public GameObject shieldImpactPrefabRed;

    public GameObject shieldImpactPrefabGreen;

    public GameObject shieldImpactPrefabPurple;
    public SpriteRenderer nextShieldLevel;
    public GameObject shieldBreakPrefab;
    public Vector3 impactShift;
    static readonly int staggerTrigger = Animator.StringToHash("Stagger");

    public void Start()
    {
        InvokeRepeating("regenShield", 0f, 2f);
        nextShieldLevel.material = new Material(nextShieldLevel.material);
    }

    public void Update()
    {
        if(transform.localScale.x > 0)
        {
            baseShieldCore.gameObject.transform.localScale = new Vector3(-1*Mathf.Abs(baseShieldCore.gameObject.transform.localScale.x), baseShieldCore.gameObject.transform.localScale.y, baseShieldCore.gameObject.transform.localScale.z);
        }
        else
        {
            baseShieldCore.gameObject.transform.localScale = new Vector3(Mathf.Abs(baseShieldCore.gameObject.transform.localScale.x), baseShieldCore.gameObject.transform.localScale.y, baseShieldCore.gameObject.transform.localScale.z);
        }
        int nextShieldLevelHealth = Mathf.Min(10, maxHealth);
        int healthForRatio = 0;
        if(health > 100)
        {
            nextShieldLevelHealth = maxHealth;
            healthForRatio = 100;
        }
        else if(health > 50)
        {
            healthForRatio = 50;
            nextShieldLevelHealth = Mathf.Min(100, maxHealth);
        }
        else if(health > 25)
        {
            healthForRatio = 25;
            nextShieldLevelHealth = Mathf.Min(50, maxHealth);
        }
        else if(health > 10)
        {
            healthForRatio = 10;
            nextShieldLevelHealth = Mathf.Min(25, maxHealth);
        }
        switchColours();
        if(nextShieldLevel != null)
        {
            if(health == 0)
            {
                nextShieldLevel.material.SetFloat("_FillAmount", ((float)(fullRegen - fullRegenCountdown))/fullRegen);
            }
            else
            {
                nextShieldLevel.material.SetFloat("_FillAmount", (float)(health-healthForRatio)/(nextShieldLevelHealth - healthForRatio)); // Set unique value
            }
        }

    }

    public void impact(int damage, Vector3 pos)
    {
        // If shield is broken hit enemy itself
        if(health == 0) 
        {
            Enemy enemy = gameObject.GetComponent<Enemy>();
        }
        // Otherwise hit shield
        else if(health - damage > 0)
        {
            Vector3 impactPos = relativePlayerPosition() ? new Vector3(pos.x-impactShift.x, pos.y + impactShift.y, pos.z) : new Vector3(pos.x+impactShift.x, pos.y + impactShift.y, pos.z);
            GameObject shieldImpact = Instantiate(getImpactPrefab(), impactPos, Quaternion.identity, gameObject.transform);
            shieldImpact.transform.localScale = new Vector3(relativePlayerPosition() ? -1*Math.Abs(shieldScale) : shieldScale,shieldScale,1);
            var impactParticles = shieldImpact.transform.Find("Impact Particles").GetComponent<ParticleSystem>();
            var impactShape = impactParticles.shape;
            var vel = impactParticles.velocityOverLifetime;
            impactShape.rotation = new Vector3(0,relativePlayerPosition() ? 0 : 270, 0);
            vel.speedModifier = relativePlayerPosition() ? 1 : -1;

            health -= damage;
        }
        else
        {
            // Break Shield
            health = 0;
            fullRegenCountdown = fullRegen;
            stagger();
            StartCoroutine(breakShield());
        }

    }

    public bool relativePlayerPosition() 
    {
        return GameObject.Find("Player").transform.position.x < transform.position.x;
    }

    public void stagger()
    {
        // Break combo
        gameObject.GetComponent<EnemyCombat>().InterruptCombo();
        gameObject.GetComponent<Animator>().SetTrigger(staggerTrigger);

    }

    public void regenShield()
    {
        if (health < maxHealth && health != 0)
        {
            health += regen;
        }
        if (health == 0)
        {
            fullRegenCountdown -= 1;
            if (fullRegenCountdown == 0)
            {
                health = maxHealth;
            }
        }
    }

    public void switchColours()
    {
        if(health > 150)
        {
            baseShieldCore.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core - Purple");
            nextShieldLevel = null;
        }
        else if(health > 100)
        {
            baseShieldCore.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core");
            nextShieldLevel.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core - Purple");
        }
        else if(health > 50)
        {
            baseShieldCore.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core - Green");
            nextShieldLevel.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core");
        }
        else if(health > 25)
        {
            baseShieldCore.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core - Yellow");
            nextShieldLevel.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core - Green");
        }
        else if(health > 10)
        {
            baseShieldCore.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core - Red");
            nextShieldLevel.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core - Yellow");
        }
        else if(health > 0)
        {
            baseShieldCore.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core - Grey");
            nextShieldLevel.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core - Red");
        }
        else
        {
            // Full regen (unique case)
            baseShieldCore.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core - Grey");
            nextShieldLevel.sprite = Resources.Load<Sprite>("Gothic/Enemy Animations/Shields/Cores/Shield Core" + getMaxShieldColour());
        }
    }

    string getMaxShieldColour()
    {
        if(maxHealth > 100)
        {
            return " - Purple";
        }
        else if(maxHealth > 50)
        {
            return "";
        }
        else if(maxHealth > 25)
        {
            return " - Green";
        }
        else if(maxHealth > 10)
        {
            return " - Yellow";
        }
        else
        {
            return " - Red";
        }
    }

    public IEnumerator breakShield() 
    {

        GameObject shieldBreaking = Instantiate(shieldBreakPrefab, transform.position + new Vector3(0.18f,-0.05f,-1), Quaternion.identity);
        shieldBreaking.transform.localScale = new Vector3((relativePlayerPosition() ? -1 : 1)*Mathf.Abs(shieldBreaking.transform.localScale.x), (relativePlayerPosition() ? -1 : 1)*Mathf.Abs(shieldBreaking.transform.localScale.y), shieldBreaking.transform.localScale.z);
        while(Time.timeScale > 0.5f)
        {
            Time.timeScale -= 0.1f;
            yield return null;
        }
        yield return new WaitForSeconds(0.8f);
        while(Time.timeScale < 1)
        {
            Time.timeScale += 0.1f;
            yield return null;
        }
        Time.timeScale = 1;
    }

    public GameObject getImpactPrefab()
    {
        if(health > 100)
        {
            return shieldImpactPrefabPurple;
        }
        else if(health > 50)
        {
            return shieldImpactPrefab;
        }
        else if(health > 25)
        {
            return shieldImpactPrefabGreen;
        }
        else if(health > 10)
        {
            return shieldImpactPrefabYellow;
        }
        else
        {
            return shieldImpactPrefabRed;
        }
    }
}
