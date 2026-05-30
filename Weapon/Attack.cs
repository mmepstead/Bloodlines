using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
public class Attack : MonoBehaviour {
    public int damage;
    public int level;
    public Animator animator;
    static readonly int coreBroken = Animator.StringToHash("Broken");

    void Start()
    {
        // StartCoroutine(attackTimer());
        Debug.Log("Attack level: " + level);
        switch(level)
        {
            case 1:
                gameObject.GetComponent<SpriteRenderer>().enabled = true;
                // animator.enabled = true;
                animator.runtimeAnimatorController = Resources.Load("Gothic/Player Animations/Attacks/Charge Attack 1 VFX Controller") as RuntimeAnimatorController;
            break;
            case 2:
                gameObject.GetComponent<SpriteRenderer>().enabled = true;
                // animator.enabled = true;
                animator.runtimeAnimatorController = Resources.Load("Gothic/Player Animations/Attacks/Charge Attack 2 VFX Controller") as RuntimeAnimatorController;
            break;
        }
    }

    public IEnumerator attackTimer()
    {
        yield return new WaitForSeconds(0.25f);
        Animator playerAnimator = GameObject.Find("Player").GetComponentInChildren<Animator>();
        int IsAttacking = Animator.StringToHash("Attacking");
        playerAnimator.SetBool(IsAttacking, false);
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        //Check for a match with the specified name on any GameObject that collides with your GameObject
        if (collision.gameObject.tag == "Core")
        {
            Enemy enemy = collision.gameObject.transform.parent.parent.gameObject.GetComponent<Enemy>();
            // collision.gameObject.GetComponent<Animator>().SetBool(coreBroken, true);
            Destroy(collision.gameObject.GetComponent<Animator>());
            Destroy(collision.gameObject.GetComponent<SpriteRenderer>());
            enemy.health -= 10;
            // Destroy(collision.gameObject.transform.parent.parent.gameObject);
        }

        //Check for a match with the specific tag on any GameObject that collides with your GameObject
        else if (collision.gameObject.tag == "Enemy")
        {
            Combo combo = GameObject.Find("Combo").GetComponent<Combo>();
            combo.extendCombo();
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy)
            {
                gameObject.GetComponent<BoxCollider2D>().enabled = false;
                ImpactManager.Instance.DoImpact();
                enemy.impact(damage, collision.ClosestPoint(transform.position));
                if (enemy.health > 0)
                {
                    Vector3 knockback = collision.gameObject.transform.position - GameObject.Find("Player").transform.position;
                    if (collision.gameObject.GetComponent<Rigidbody2D>() != null)
                    {
                        collision.gameObject.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0, 0);
                        collision.gameObject.GetComponent<Rigidbody2D>().AddForce(knockback.normalized * 150);
                    }
                }
            }
        }
    }
}
