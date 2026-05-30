using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

public class MovementOld : MonoBehaviour {
    [SerializeField] public float speed;
    [SerializeField] public float jumpAmount;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask layermask;
    private float _currentVelocity;
    private Rigidbody2D _rigidbody;
    public BoxCollider2D baseCollider;
    public BoxCollider2D dashCollider;
    public BoxCollider2D slideCollider;
    private GameObject target;
    public GameObject weaponAttackPrefab;
    public GameObject chargeAttack1Prefab;
    public GameObject chargeAttack2Prefab;
    public GameObject chargeParticlesPrefab;
    public GameObject stompPrefab;
    public bool jumping;
    public float jumpTime;
    int damage = 3;
    public float maxJump = 0.3f;
    int swingDelay = 0;
    int stompDelay = 0;
    int throwDelay = 0;
    int dashDelay = 0;
    int slideDelay = 0;
    int riposteDelay = 0;
    int chargeAttackTimer = 0;
    bool dashing = false;
    bool sliding = false;
    bool stomping = false;
    bool doubleJump = false;
    bool doubleJumping = false;
    bool walking = false;
    bool jumped = false;
    public bool grounded = true;
    bool secondAttack = false;
    int charge1Timer = 240;
    int charge2Timer = 480;
    public Vector2 hangingPoint;
    public GameObject targetPrefab;
    public GameObject throwPrefab;
    public GameObject stakePrefab;
    public GameObject ripostePrefab;
    public GameObject glassBreakPrefab;

    public GameObject groundedOn;
    public GameObject hangingOn;
    public GameObject lockedOnTo;
    public GameObject slideDustPrefab;
    public GameObject jumpDustPrefab;
    public List<GameObject> chargeFlames = new List<GameObject>();
    public ParticleSystem runningParticles;
    static readonly int IsThirdAttack = Animator.StringToHash("Third Attack");
    static readonly int IsSecondAttack = Animator.StringToHash("Second Attack");
    static readonly int IsRunning = Animator.StringToHash("Running");
    static readonly int IsJumping = Animator.StringToHash("Jumping");
    static readonly int IsFalling = Animator.StringToHash("Falling");
    static readonly int IsDrawing = Animator.StringToHash("Drawing");
    static readonly int IsSliding = Animator.StringToHash("Sliding");
    static readonly int IsHanging = Animator.StringToHash("Hanging");
    static readonly int IsRiposting = Animator.StringToHash("Riposting");
    static readonly int IsAttacking = Animator.StringToHash("Attacking");
    static readonly int IsLockedOn = Animator.StringToHash("Locked On");
    static readonly int IsLockingOn = Animator.StringToHash("Locking On");
    static readonly int dash = Animator.StringToHash("Dash");
    static readonly int stomp = Animator.StringToHash("Stomp");
    static readonly int throwTrigger = Animator.StringToHash("Throwing");

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (lockedOnTo != null)
        {
            flipToFace();
        }
        handleHitBox();
        var runningParticlesShape = runningParticles.shape;
        // runningParticlesShape.rotation = new Vector3(0,transform.localScale.x > 0 ? 270 : 90, 0);
        jumpAmount = Time.timeScale == 0.3f ? 0.15f*0.3f : 0.15f;
        IsGrounded();
        //Falling
        if(!grounded && _rigidbody.linearVelocity.y < 0)
        {
            if(runningParticles.isPlaying)
            {
                runningParticles.Stop();
            }
            animator.SetBool(IsFalling, true);
        }
        else
        {
            animator.SetBool(IsFalling, false);
        }
        // Run
        Vector3 localScale = transform.localScale;
        //Lock On
        // if(Input.GetKeyDown(KeyCode.Tab))
        // {
        //     if(lockedOnTo == null)
        //     {
        //         GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        //         float minDistance = 999;
        //          foreach (GameObject enemy in enemies)
        //          {
        //             float distance = Vector3.Distance(transform.position, enemy.transform.position);
        //             if(distance < 4 && distance < minDistance)
        //             {
        //                 minDistance = distance;
        //                 lockedOnTo = enemy;
        //                 animator.SetTrigger(IsLockingOn);
        //                 animator.SetBool(IsLockedOn, true);
        //             }
        //          }
        //     }
        //     else
        //     {
        //         lockedOnTo = null;
        //         animator.SetBool(IsLockedOn, false);
        //     }
            
        // }
        if(Input.GetKey(KeyCode.RightArrow) && !hanging() && !attacking() && !riposting()) 
        {
            if(grounded)
            {
                if(!walking)
                {
                    walking = true;
                }
                animator.SetBool(IsRunning, true);
            }
            if (lockedOnTo == null)
            {
                transform.localScale = new Vector3(localScale.x < 1 ? -1 * localScale.x : localScale.x, localScale.y, localScale.z);
                _currentVelocity = speed;

            }
            else
            {
                _currentVelocity = 0.75f * speed;
            }
        }
        else if(Input.GetKey(KeyCode.LeftArrow) && !hanging() && !attacking() && !riposting())
        {
            if(grounded)
            {
                if (!walking)
                {
                    walking = true;
                }
                animator.SetBool(IsRunning, true);
            }
            if (lockedOnTo == null)
            {
                transform.localScale = new Vector3(localScale.x < 1 ? localScale.x : -1 * localScale.x, localScale.y, localScale.z);
                _currentVelocity = -1*speed;
            }
            else
            {
                _currentVelocity = -0.75f*speed;
            }
        }
        else
        {
            if (walking)
            {
                walking = false;
            }
            animator.SetBool(IsRunning, false);
            _currentVelocity = 0;
        }
        // Jump
        if(Input.GetKeyDown(KeyCode.UpArrow) && grounded)
        {
            Instantiate(jumpDustPrefab, transform.position, Quaternion.identity);
            AudioManager.Instance.PlayPlayerJump();
            jumped = true;
            // Regular Jump
            if(!jumping && !doubleJumping)
            {
               if(doubleJump || hanging()) 
                {
                    doubleJumping = true;
                    jumpTime = 0;
                    gameObject.GetComponentInChildren<Animator>().SetTrigger("Vault");
                    // iTween.RotateBy(gameObject, new Vector3(0,0,gameObject.transform.localScale.x < 0 ? 1f : -1f), 0.5f);
                }                else
                {
                    jumping = true;
                    jumpTime = 0;
                }
            }
            if(runningParticles.isPlaying)
            {
                runningParticles.Stop();
            }
            _rigidbody.AddForce(Vector2.up * (jumping ? 3 : 6), ForceMode2D.Impulse);
            animator.SetBool(IsHanging, false);
            animator.SetBool(IsRunning, false);
        }
        if(Input.GetKey(KeyCode.UpArrow) && jumpTime < maxJump && (jumping || doubleJumping))
        {
            if(runningParticles.isPlaying)
            {
                runningParticles.Stop();
            }
            animator.SetBool(IsRunning, false);
            animator.SetBool(IsJumping, true);
            _rigidbody.AddForce(Vector2.up * jumpAmount, ForceMode2D.Impulse);
        }
        else
        {
            jumping = false;
            doubleJumping = false;
            if(_rigidbody.linearVelocity.y < 0) {
                animator.SetBool(IsJumping, false);
            }
        }
        if(jumping || doubleJumping)
        {
            jumpTime += Time.deltaTime;
        }
        // Attacking
        if (swingDelay == 0 && !dashing && !sliding && !stomping && !hanging() && (Input.GetKeyDown(KeyCode.Q) || Input.GetKey(KeyCode.Q)))
        {
            if (chargeAttackTimer == 0)
            {
                AudioManager.Instance.PlayPlayerCharging();
            }
            chargeAttackTimer += 1;
            if (chargeAttackTimer == 1)
            {
                createChargeParticles(1);
            }
            else if (chargeAttackTimer == charge2Timer)
            {
                AudioManager.Instance.PlayPlayerCharged();
                AudioManager.Instance.StopPlayerCharging();
                destroyChargeFlames();
                createChargeFlames(2);

            }
            else if (chargeAttackTimer == charge1Timer)
            {
                AudioManager.Instance.PlayPlayerCharged();
                createChargeFlames(1);
                createChargeParticles(2);
            }
            // if(GameObject.Find("Target(Clone)") == null) {
            //     target = Instantiate(targetPrefab, transform.position, Quaternion.identity, transform);
            // }
        }
        if(((Input.GetKeyUp(KeyCode.Q) && chargeAttackTimer > 0) || Input.GetKeyDown(KeyCode.Q)) && !dashing && !sliding && !stomping && !hanging())
        {
            int attackDamage = damage;
            int level = 0;
            if(chargeAttackTimer > charge2Timer) {
                level = 2;
                attackDamage += 10;
            }
            else if(chargeAttackTimer > charge1Timer) {
                level = 1;
                attackDamage += 5;
            }
            chargeAttackTimer = 0;
            destroyChargeFlames();
            if(!GameObject.Find("Attack(Clone)") && swingDelay == 0)
            {
                
                animator.SetBool(IsAttacking, true);
                animator.SetBool(IsDrawing, false);
                AudioManager.Instance.StopPlayerCharging();
                if (level > 0)
                {
                    AudioManager.Instance.PlayPlayerChargeAttack();
                }
                GameObject attack = Instantiate(weaponAttackPrefab, transform.position + new Vector3(gameObject.transform.localScale.x < 0 ? -0.35f : 0.35f,0,-1), Quaternion.identity, transform);
                attack.GetComponent<Attack>().level = level;
                attack.GetComponent<Attack>().damage = attackDamage;
                swingDelay = 45;
                Physics2D.IgnoreCollision(attack.GetComponent<Collider2D>(), GetComponent<Collider2D>());
            }
        }
        //Sliding
        if(Input.GetKey(KeyCode.DownArrow) && grounded && !sliding && slideDelay == 0)
        {
            AudioManager.Instance.PlayPlayerSlide();
            animator.SetBool(IsSliding, true);
            GameObject slideDust = Instantiate(slideDustPrefab, transform.position + new Vector3(transform.localScale.x < 0 ? -0.5f : 0.5f, 0, 0), Quaternion.identity);
            slideDust.transform.localScale = transform.localScale;
            if (runningParticles.isPlaying)
            {
                runningParticles.Stop();
            }
            sliding = true;
            slideDelay = 120;
            StartCoroutine(slideTimer());
        }
        if(Input.GetKeyDown(KeyCode.W) && riposteDelay == 0) {
            animator.SetBool(IsRiposting, true);
        }
        //Dashing
        if(Input.GetKeyDown(KeyCode.E) && !dashing && dashDelay == 0 && !sliding)
        {
            if(runningParticles.isPlaying)
            {
                runningParticles.Stop();
            }
            animator.SetTrigger(dash);

            dashing = true;
            dashDelay = 120;
            StartCoroutine(dashTimer());
        }
        // if(hanging()) {
        //     transform.position = hangingOn.transform.position + new Vector3(hangingPoint.x, hangingPoint.y, transform.position.z);
        // }
        swingDelay -= swingDelay == 0 ? 0 : 1;
        stompDelay -= stompDelay == 0 ? 0 : 1;
        throwDelay -= throwDelay == 0 ? 0 : 1;
        dashDelay -= dashDelay == 0 ? 0 : 1;
        slideDelay -= slideDelay == 0 ? 0 : 1;
    }

    public void flipToFace()
    {
        if(lockedOnTo.transform.position.x < transform.position.x && !sliding && !dashing)
        {
            transform.localScale = new Vector3(-1*Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if(!sliding && !dashing)
        {
             transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    public void resetMomentum()
    {
        _rigidbody.linearVelocity = new Vector2(0,0);
    }

    public void handleHitBox()
    {
        if (dashing)
        {
            // slideCollider.enabled = false;
            // baseCollider.enabled = false;
            // dashCollider.enabled = true;
        }
        else if (animator.GetBool(IsSliding))
        {
            baseCollider.enabled = false;
            dashCollider.enabled = false;
            slideCollider.enabled = true;
        }
        else
        {
            baseCollider.enabled = true;
            dashCollider.enabled = false;
            slideCollider.enabled = false;
        }
    }

    public void OnCollisionEnter2D(Collision2D col)
    {
        if(col.collider.gameObject.tag == "Glass Wall") {
           if(dashing) 
           {
                GameObject glassBreak = Instantiate(glassBreakPrefab, col.otherCollider.transform.position, Quaternion.identity);
                glassBreak.transform.localScale = new Vector3(transform.localScale.x*glassBreak.transform.localScale.x, transform.localScale.x*glassBreak.transform.localScale.y, glassBreak.transform.localScale.z);
                Destroy(col.collider.gameObject);
           } 
        }
        if(col.collider.gameObject.tag == "Pickup")
        {
            Destroy(col.collider.gameObject);
        }
    }

    public IEnumerator doubleJumpTimer()
    {
        doubleJump = true;
        yield return new WaitForSeconds(0.1f);
        doubleJump = false;
    }

    public void useStomp()
    {
        if(!stomping && stompDelay == 0 && grounded)
        {
            stompDelay = 120;
            animator.SetTrigger(stomp);
            stomping = true;
            StartCoroutine(stompTimer());
        }
    }

    public void playWalkSound()
    {
        AudioManager.Instance.PlayPlayerWalk();
    }

    public void throwStake()
    {
        if (throwDelay == 0)
        {
            throwDelay = 120;
            animator.SetTrigger(throwTrigger);
            // Instantiate(throwPrefab, GameObject.Find("Player").transform.position + new Vector3(transform.localScale.x < 0 ? 0.1f : -0.1f, 0,0), Quaternion.identity, transform);
            Instantiate(stakePrefab, GameObject.Find("Player").transform.position, Quaternion.identity);
        }
    }

    public void FixedUpdate()
    {
        if(sliding)
        {
            _rigidbody.linearVelocity = new Vector2(transform.localScale.x > 0 ? 7 : -7, 0);
        }
        else if(dashing)
        {
            if(dashDelay > 85)
            {
                _rigidbody.linearVelocity = new Vector2(0, 0);
            }
            else
            {
                _rigidbody.linearVelocity = new Vector2(transform.localScale.x > 0 ? 20 : -20, 0);
            }
        }
        else if(stomping)
        {
            _rigidbody.linearVelocity = new Vector2(0,0);
        }
        else if(animator.GetCurrentAnimatorStateInfo(0).IsName("Backflip") || (animator.GetCurrentAnimatorStateInfo(0).IsName("Hurt")))
        {
            _rigidbody.linearVelocity = new Vector2(clampVelocity(_rigidbody.linearVelocity.x, 4), clampVelocity(_rigidbody.linearVelocity.y, 4));
        }
        else
        {
            _rigidbody.linearVelocity = new Vector2(clampVelocity(_currentVelocity, 4), clampVelocity(_rigidbody.linearVelocity.y, 8));
        }
        if(hanging())
        {
            destroyChargeFlames();
            _rigidbody.linearVelocity = new Vector2(0,0);
            transform.position = hangingOn.transform.position + new Vector3(hangingPoint.x, hangingPoint.y, transform.position.z);
        }
    }

    public float clampVelocity(float velocity, float max)
    {
        bool positive = velocity > 0;
        if(positive)
        {
            return Mathf.Min(velocity, max);
        }
        else
        {
            return Mathf.Max(velocity, -1*max);
        }
    }

    public void createChargeParticles(int stage)
    {
        chargeFlames.Add(Instantiate(chargeParticlesPrefab, transform.position + new Vector3(0f,0f,1), Quaternion.identity, transform));
    }

    public void createChargeFlames(int stage)
    {
        Vector3 offset = transform.localScale.x < 0 ? new Vector3(-0.1f,0.3f,1) : new Vector3(0.1f,0.3f,1);
        chargeFlames.Add(Instantiate(stage == 1 ? chargeAttack1Prefab : chargeAttack2Prefab, transform.position + offset, Quaternion.identity, transform));
    }

    public void destroyChargeFlames()
    {
        foreach(GameObject chargeFlame in chargeFlames)
        {
            Destroy(chargeFlame);
        }
        chargeFlames = new List<GameObject>();
    }

    public void IsGrounded() 
    {
        float rayLength = 0.38f; // Adjust based on your character's size
        RaycastHit2D collision = Physics2D.CircleCast(transform.position+ (transform.localScale.x < 0 ? new Vector3(0.1f,0,0) : new Vector3(-0.1f,0,0)), 0.1f, Vector2.down, rayLength, layermask);
        bool newGrounded = hanging() || collision;
        if(!grounded && jumped && newGrounded) 
        {
            StartCoroutine(doubleJumpTimer());
        }
        if(!grounded && newGrounded)
        {
            AudioManager.Instance.PlayPlayerLanding();
            jumped = false;
        }
        grounded = newGrounded;
        groundedOn = collision ? collision.transform.gameObject : null;
    }

    public void movePlayer(Vector3 move)
    {
        transform.position += move;
    }

    public bool hanging() 
    {
        return animator.GetBool(IsHanging);
    }
    public bool attacking() 
    {
        return grounded && (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack Idle") || animator.GetCurrentAnimatorStateInfo(0).IsName("First Attack") || animator.GetCurrentAnimatorStateInfo(0).IsName("Third Attack") || animator.GetCurrentAnimatorStateInfo(0).IsName("Second Attack"));
    }
    public bool riposting() 
    {
        return grounded && animator.GetCurrentAnimatorStateInfo(0).IsName("Riposte");
    }
    public IEnumerator dashTimer()
    {
        yield return new WaitForSeconds(0.5f);
        _currentVelocity = 0;
        dashing = false;
    }

    public IEnumerator slideTimer()
    {
        yield return new WaitForSeconds(0.5f);
        animator.SetBool(IsSliding, false);
        _currentVelocity = 0;
        sliding = false;
    }

    public IEnumerator riposteTimer()
    {
        yield return new WaitForSeconds(0.5f);
        dashing = false;
    }

    public IEnumerator stompTimer()
    {
        yield return new WaitForSeconds(0.4f);
        _currentVelocity = 0;
        stomping = false;
        StartCoroutine(spawnStomps());
    }

    public IEnumerator spawnStomps()
    {
        Vector3 startingPoint = transform.position;
        bool flip = transform.localScale.x < 0;
        Instantiate(stompPrefab, new Vector3(startingPoint.x+1, startingPoint.y, 2), Quaternion.identity);
        Instantiate(stompPrefab, new Vector3(startingPoint.x-1, startingPoint.y, 2), Quaternion.identity);
        yield return new WaitForSeconds(0.1f);
        // for(int i = 1; i < 4; i += 1)
        // {
        //     Instantiate(stompPrefab, new Vector3(startingPoint.x+ (flip ? -i : i), startingPoint.y, 2), Quaternion.identity);
        //     yield return new WaitForSeconds(0.5f);
        // }
    }
}