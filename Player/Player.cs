using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Player : MonoBehaviour {
    public CircleFade circleFade;
    public Fade screenFade;
    public Movement movement;
    public Rigidbody2D _rigidbody;
    public Vector3 latestCheckpoint;
    bool respawning = false;
    static readonly int IsRunning = Animator.StringToHash("Running");
    public IEnumerator Start()
    {
        yield return null;
        if(!CutsceneManager.cutsceneActive) 
        {
            gameObject.GetComponent<Movement>().enabled = false;
            StartCoroutine(entrance());
        }
    }

    public IEnumerator entrance()
    {
        //Wait a moment
        yield return new WaitForSeconds(0.2f);
        // Walk player in from offscreen based on entrance direction
        Movement movement = gameObject.GetComponent<Movement>();
        string direction = GameManager.Instance.entranceDirection;
        Vector3 startPosition = transform.position;
        float distance = 1f;
        movement.animator.SetBool(IsRunning, true);
        // Move based on direction
        switch (direction) {
            case "Left":
                while (transform.position.x < startPosition.x + distance)
                {
                    transform.position += new Vector3(Time.deltaTime, 0, 0);
                    yield return null;
                }
                break;
            case "Right":
                while (transform.position.x > startPosition.x - distance)
                {
                    transform.position -= new Vector3(Time.deltaTime, 0, 0);
                    yield return null;
                }
                break;
            case "Up":
                startPosition.y += distance;
                transform.position = startPosition;
                while (transform.position.y > startPosition.y - distance)
                {
                    transform.position -= new Vector3(0, movement.speed * Time.deltaTime, 0);
                    yield return null;
                }
                break;
            case "Down":
                startPosition.y -= distance;
                transform.position = startPosition;
                while (transform.position.y < startPosition.y + distance)
                {
                    transform.position += new Vector3(0, movement.speed * Time.deltaTime, 0);
                    yield return null;
                }
                break;
        }
        movement.enabled = true;
    }

    public IEnumerator respawn(float extraDelay = 2f, bool circle = false, float speed = 10)
    {
        if(respawning) yield break;
        respawning = true;
        movement.enabled = false;
        _rigidbody.simulated = false;
        if(circle) 
        {
            circleFade.fadeOut(speed);
        }
        else
        {
            screenFade.fadeOut();
        }
        yield return new WaitForSeconds(extraDelay);
        // fade player out and to the back
        SpriteRenderer playerSprite = transform.Find("Sprites").GetComponent<SpriteRenderer>();
        while(playerSprite.color.a > 0)
        {
            Color colour = playerSprite.color;
            playerSprite.color = new Color(colour.r, colour.g, colour.b, colour.a - 0.1f);
            yield return null;
        }
        playerSprite.sortingLayerName = "Default";
        playerSprite.sortingOrder = 10;
        transform.position = latestCheckpoint;
        if(circle) 
        {
            circleFade.fadeIn(speed);
        }
        else
        {
            screenFade.fadeIn();
        }
        yield return new WaitForSeconds(0.3f);
        _rigidbody.simulated = true;
        PlayerData.currentHealth = PlayerData.currentHealth == 0 ? PlayerData.maxHealth : PlayerData.currentHealth;
        playerSprite.color = new Color(1,1,1,1);
        movement.enabled = true;
        respawning = false;
    }
}
