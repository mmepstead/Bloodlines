using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Enemy : MonoBehaviour {

    public int health;
    public Animator animator;
    public SpriteRenderer renderer;
    public Rigidbody2D rigidBody;
    public GameObject flameDeathPrefab;
    public GameObject bloodSplashPrefab;
    public GameObject flameDeath;
    public GameObject hardParryFlashPrefab;

    public GameObject pesetaPrefab;
    public int pesetaDropRate = 100; // out of 100
    public int pesetaDropAmount = 5;
    public bool dying;
    public Vector3 hardParryFlashOffset;

    public Sprite deathSprite;
    public GameObject hitPrefab;
    public RuntimeAnimatorController deathController;
    public Shield shield;

    public void Update() {
        // facePlayer();
    }

    public void facePlayer()
    {
        Transform playerTransform = GameObject.Find("Player").transform;
        bool playerToTheLeft = playerTransform.position.x < transform.position.x;
        transform.localScale = new Vector3(!playerToTheLeft ? -1*Mathf.Abs(transform.localScale.x) : Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }
    public void enemyDeath() {
        Instantiate(flameDeathPrefab, transform.position + new Vector3(0.2f,-0.5f,0), Quaternion.identity, transform);
        Instantiate(flameDeathPrefab, transform.position + new Vector3(-0.2f,-0.5f,0), Quaternion.identity, transform);
        gameObject.GetComponent<EnemyCombat>().InterruptCombo();
        Destroy(rigidBody);
        Destroy(gameObject.GetComponent<BoxCollider2D>());
        StartCoroutine(death());
    }

    public IEnumerator death()
    {
        if(dying) yield break;
        dying = true;
        gameObject.GetComponent<EnemyAI>().audioManager.PlayDeath();
        renderer.sprite = deathSprite;
        animator.runtimeAnimatorController = deathController;
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    public virtual void dropItems()
    {
        for(int i = 0; i < pesetaDropAmount; i++)
        {
            Instantiate(pesetaPrefab, transform.position + new Vector3(Random.Range(-0.5f,0.5f), Random.Range(-0.5f,0.5f), 0), Quaternion.identity);
        }
    }

    public void damageTaken(int damage)
    {
        health -= damage;
        if(health <= 0)
        {
            enemyDeath();
        }
        // StartCoroutine(hurtFlash());
    }

    public IEnumerator hurtFlash()
    {
        renderer.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        renderer.color = Color.white;
    }

    public void flashHardParry()
    {
        Instantiate(hardParryFlashPrefab, transform.position + hardParryFlashOffset, Quaternion.identity);
    }

    Color currentShieldColour()
    {
        if(shield.health > 100)
        {
            return Color.magenta;
        }
        else if(shield.health > 50)
        {
            return Color.blue;
        }
        else if(shield.health > 25)
        {
            return Color.green;
        }
        else if(shield.health > 10)
        {
            return Color.yellow;
        }
        else
        {
            return Color.red;
        }

    }
    
    public void impact(int damage, Vector3 pos)
    {
        Transform playerTransform = GameObject.Find("Player").transform;
        bool playerToTheLeft = playerTransform.position.x < transform.position.x;
        GameObject hitEffect = Instantiate(hitPrefab, pos + new Vector3(playerToTheLeft ? 0.5f : -0.5f,0,0), Quaternion.identity);
        if (!playerToTheLeft)
        {
            hitEffect.transform.localScale = new Vector3(-1 * Mathf.Abs(hitEffect.transform.localScale.x), hitEffect.transform.localScale.y, hitEffect.transform.localScale.z);
        }
        if (shield.health > 0)
        {
            AudioManager.Instance.PlayPlayerHitShield();
            shield.impact(damage, pos);
            gameObject.GetComponent<Flash>().flash(currentShieldColour());
        }
        else
        {
            AudioManager.Instance.PlayPlayerHitEnemy();
            gameObject.GetComponent<EnemyAI>().audioManager.PlayHurt();
            damageTaken(damage);
            GameObject blood = Instantiate(bloodSplashPrefab, pos + new Vector3(0.1f, -0.1f, 0), Quaternion.identity);
            blood.transform.Find("Blood Splatter").transform.localScale = new Vector3(playerToTheLeft ? 1f : -1f,1f,1f);
            gameObject.GetComponent<Flash>().whiteFlash();
        }
    }
}
