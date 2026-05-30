using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
public class Health : MonoBehaviour {
    public bool riposteAvailable = false;
    public bool riposted = false;
    public float iFrames = 0f;
    bool invincible = false;
    public SpriteRenderer renderer;
    public Animator animator;
    public GameObject riposteFlash;
    public GameObject bloodSplashPrefab;
    public GameObject ripplePrefab;
    public GameObject parryBarPrefab;
    public float knockbackDirection = 0f;
    private bool knockedBack = false;
    public SpriteRenderer playerSprite;
    int riposteDamage = 5;
    static readonly int hurt = Animator.StringToHash("Hurt");
    static readonly int death = Animator.StringToHash("Death");
    bool inEnemyTrigger = false;
    Collider2D currentCollision;
    void Awake()
    {
    }
    void Update()
    {
        if(iFrames > 0 && PlayerData.currentHealth > 0)
        {
            invincible = true;
            iFrames -= Time.deltaTime;
            flicker();
        }
        else
        {
            invincible = false;
            iFrames = 0;
        }
        if(inEnemyTrigger)
        {
            //If the GameObject has the same tag as specified, output this message in the console
            if(PlayerData.currentHealth > 0 && !invincible && (currentCollision.gameObject.tag == "Enemy" || currentCollision.gameObject.tag == "Hazard"))
            {
                enemyHit(currentCollision);
            }
        }
    }

    void FixedUpdate()
    {
        if(knockedBack)
        {
            knockedBack = false;
            
            gameObject.GetComponent<Rigidbody2D>().AddForce(new Vector3(knockbackDirection, 100, 0));
        }
    }

    void flicker()
    {
        if (iFrames <= 0)
        {
            playerSprite.color = new Color(1f, 1f, 1f, 1f);
        }
        else if(iFrames % 0.1 < 0.02) 
        {
            playerSprite.color = new Color(1f, 1f, 1f, 0f);
        }

        else
        {
            playerSprite.color = new Color(1f, 1f, 1f, 1f);
        }
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        //Check for a match with the specific tag on any GameObject that collides with your GameObject
        if (collision.gameObject.tag == "Enemy" || collision.gameObject.tag == "Hazard")
        {
            inEnemyTrigger = true;
            currentCollision = collision;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        //Check for a match with the specific tag on any GameObject that collides with your GameObject
        if (collision.gameObject.tag == "Enemy" || collision.gameObject.tag == "Hazard")
        {
            inEnemyTrigger = false;
            currentCollision = null;
        }
    }

    public void enemyHit(Collider2D collision)
    {
        Movement playerMovement= gameObject.GetComponent<Movement>();
        if (playerMovement.riposting())
        {
            riposte(collision);
        }
        else
        {
            damageTaken(collision);
            iFrames = 1.0f;
        }
    }

    // public IEnumerator waitForRiposte(Collider2D collision)
    // {
    //     riposteAvailable = true;
    //     yield return new WaitForSeconds(0.1f);
    //     if(riposted)
    //     {
    //         riposte(collision);
    //     }
    //     else
    //     {
    //         damageTaken(collision);
    //         riposteAvailable = false;
    //     }
    // }

    public void riposte(Collider2D collision)
    {
        AudioManager.Instance.PlayPlayerParry();
        collision.enabled = false;
        Combo combo = GameObject.Find("Combo").GetComponent<Combo>();
        combo.extendCombo();
        Transform enemy = collision.gameObject.transform.parent;
        EnemyAttack attackHitBy = enemy.gameObject.GetComponent<EnemyCombat>().currentAttack;
        if(attackHitBy != null && attackHitBy.hardParry)
        {
            // Spawn parry bar, quick zoom, put both player and enemy in parry stance with no hitboxes
            Transform camera = GameObject.Find("Main Camera").transform;
            StartCoroutine(camera.GetComponent<Zoom>().startParryZoom());
            // gameObject.GetComponent<Movement>().startParryStance();
            // enemy.gameObject.GetComponent<EnemyAI>().startParryStance();
            // Instantiate(parryBarPrefab, gameObject.transform.position, Quaternion.identity, gameObject.transform);
            Movement movement = gameObject.GetComponent<Movement>();
            movement.startHardParry();
            movement.hardParryEnemy = enemy.gameObject;
            enemy.gameObject.GetComponent<EnemyAI>().startHardParry();
        }
        else
        {
            enemy.gameObject.GetComponent<Shield>().impact(5, collision.ClosestPoint(enemy.position));
            animator.SetTrigger("Riposte");
            GameObject riposteHit = Instantiate(riposteFlash, GameObject.Find("Player").transform.position, Quaternion.identity);
            riposteHit.transform.localScale = new Vector3(GameObject.Find("Player").transform.localScale.x == -1 ? -2 : 2, 2, 1);
        }
        // iTween.RotateBy(gameObject, new Vector3(0,0,gameObject.transform.localScale.x < 0 ? -1f : 1f), 0.5f);
    }

    public void damageTaken(Collider2D collision)
    {
        AudioManager.Instance.PlayPlayerHurt();
        Combo combo = GameObject.Find("Combo").GetComponent<Combo>();
        combo.breakCombo();
        PlayerData.currentHealth -= 1;
        if(collision != null && collision.gameObject != null)
        {
            // Vector3 knockback = collision.gameObject.transform.position - GameObject.Find("Player").transform.position;
            // gameObject.GetComponent<Rigidbody2D>().AddForce(new Vector3(-knockback.normalized.x*500, -knockback.normalized.y*100, 0));
            // if(collision.gameObject.GetComponent<Rigidbody2D>() != null)
            // {
            //     collision.gameObject.GetComponent<Rigidbody2D>().AddForce(new Vector3(knockback.normalized.x*500, knockback.normalized.y*100, 0));
            // }
            // if(health > 0)
            // {
                knockbackDirection = collision.gameObject.transform.position.x > gameObject.transform.position.x ? -250f : 250f;
                knockedBack = true;
            // }
        }
        // We got hit by a hard parry
        if(collision == null)
        {
            GameObject hardParryEnemy = GameObject.Find("Player").GetComponent<Movement>().hardParryEnemy;
            knockbackDirection = hardParryEnemy.transform.position.x > gameObject.transform.position.x ? -250f : 250f;
            knockedBack = true;
        }
        StartCoroutine(hurtFlash());
    }

    public IEnumerator hurtFlash()
    {
        if(PlayerData.currentHealth > 0) 
        {
            GameObject blood = Instantiate(bloodSplashPrefab, gameObject.transform.position, Quaternion.identity);
            blood.transform.localScale = new Vector3(-1f,1f,1f);
            renderer.color = Color.red;
            animator.SetTrigger(hurt);
            yield return new WaitForSeconds(0.3f);
            renderer.color = Color.white;
        }
        else
        {
            StartCoroutine(gameObject.GetComponent<Player>().respawn(3.3f, true, 20));
            GameObject blood = Instantiate(bloodSplashPrefab, gameObject.transform.position, Quaternion.identity);
            blood.transform.localScale = new Vector3(-1f,1f,1f);
            SpriteRenderer playerSprite = transform.Find("Sprites").GetComponent<SpriteRenderer>();
            playerSprite.sortingLayerName = "UI";
            playerSprite.sortingOrder = 101;
            animator.SetTrigger(hurt);
            yield return new WaitForSeconds(0.3f);
            AudioManager.Instance.PlayPlayerDeath();
            animator.SetTrigger(death);
            // Instantiate(ripplePrefab, gameObject.transform.position + new Vector3(gameObject.transform.localScale.x < 0 ? 0.2f : -0.2f, 0,0), Quaternion.identity, gameObject.transform);
        }

    }
}
