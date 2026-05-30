using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

public class Movement : MonoBehaviour {
    [SerializeField] public float speed;
    [SerializeField] public float jumpAmount;
    [SerializeField] public Animator animator;
    [SerializeField] private LayerMask layermask;
    private float _currentVelocity;
    private Rigidbody2D _rigidbody;
    public BoxCollider2D baseCollider;
    public BoxCollider2D dashCollider;
    public BoxCollider2D slideCollider;
    public BoxCollider2D hangingCollider;
    private GameObject target;
    public GameObject weaponAttackPrefab;
    public GameObject chargeAttack1Prefab;
    public GameObject chargeAttack2Prefab;
    public GameObject chargeParticlesPrefab;
    public GameObject chargeParticlesBluePrefab;
    public GameObject stompPrefab;
    public GameObject groundCheck;
    public bool jumping;
    public bool hardParrying = false;
    public float jumpTime;
    int damage = 3;
    public float maxJump = 0.5f;
    float swingDelay = 0;
    float stompDelay = 0;
    float throwDelay = 0;
    float dashDelay = 0;
    float slideDelay = 0;
    float riposteDelay = 0;
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
    bool jumpBoostIntention = false;
    bool jumpingIntention = false;
    bool doubleJumpingIntention = false;
    public bool slideEnabled = true;
    public bool dashEnabled = true;
    int charge1Timer = 240;
    int charge2Timer = 480;
    public Vector2 hangingPoint;
    public GameObject targetPrefab;
    public GameObject throwPrefab;
    public GameObject stakePrefab;
    public GameObject ripostePrefab;
    public GameObject glassBreakPrefab;
    public GameObject hardParrySparkPrefab;
    public GameObject parryBarPrefab;
    GameObject healthBar;
    GameObject healthMeter;
    GameObject subWeaponDisplay;
    GameObject comboDisplay;

    public GameObject groundedOn;
    public GameObject hangingOn;
    public GameObject lockedOnTo;
    public GameObject slideDustPrefab;
    public GameObject slideParticlesPrefab;
    public GameObject slideParticles;
    public GameObject jumpDustPrefab;
    public GameObject dashSparksPrefab;
    public GameObject hardParryEnemy;
    public GameObject hardParryBar;
    public GameObject hardParrySpark;
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
    static readonly int IsHardParrying = Animator.StringToHash("Hard Parrying");
    static readonly int dash = Animator.StringToHash("Dash");
    static readonly int stomp = Animator.StringToHash("Stomp");
    static readonly int throwTrigger = Animator.StringToHash("Throwing");

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if(PauseMenu.IsPaused || hardParrying) {
            return;
        }
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
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
            transform.localScale = new Vector3(localScale.x < 1 ? -1 * localScale.x : localScale.x, localScale.y, localScale.z);
            _currentVelocity = speed;

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
            transform.localScale = new Vector3(localScale.x < 1 ? localScale.x : -1 * localScale.x, localScale.y, localScale.z);
            _currentVelocity = -1*speed;
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
                    doubleJumpingIntention = true;
                    doubleJumping = true;
                    jumping = false;
                    jumpTime = 0;
                    gameObject.GetComponentInChildren<Animator>().SetTrigger("Vault");
                }
                else
                {
                    jumpingIntention = true;
                    jumping = true;
                    doubleJumping = false;
                    jumpTime = 0;
                }
            }
            if(runningParticles.isPlaying)
            {
                runningParticles.Stop();
            }
            animator.SetBool(IsHanging, false);
            animator.SetBool(IsRunning, false);
        }
        if (Input.GetKey(KeyCode.UpArrow) && jumpTime < maxJump && (jumping || doubleJumping))
        {
            if (runningParticles.isPlaying)
            {
                runningParticles.Stop();
            }
            jumpBoostIntention = true;
            animator.SetBool(IsRunning, false);
            animator.SetBool(IsJumping, true);
        }
        else
        {
            jumping = false;
            doubleJumping = false;
            jumpBoostIntention = false;
            if (_rigidbody.linearVelocity.y < 0)
            {
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
                destroyChargeFlames();
                createChargeFlames(1);
                createChargeParticles(2);
            }
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
                swingDelay = 0.7f;
                Physics2D.IgnoreCollision(attack.GetComponent<Collider2D>(), GetComponent<Collider2D>());
            }
        }
        //Sliding
        if(Input.GetKey(KeyCode.DownArrow) && slideEnabled&& grounded && !sliding && slideDelay == 0 && !hanging() && !dashing && ableToSlide())
        {
            AudioManager.Instance.PlayPlayerSlide();
            animator.SetBool(IsSliding, true);
            GameObject slideDust = Instantiate(slideDustPrefab, transform.position + new Vector3(transform.localScale.x < 0 ? -0.5f : 0.5f, 0, 0), Quaternion.identity);
            slideDust.transform.localScale = transform.localScale;
            if (runningParticles.isPlaying)
            {
                runningParticles.Stop();
            }
            slideParticles = Instantiate(slideParticlesPrefab, transform.position + new Vector3(0, -0.5f, 0), Quaternion.identity, transform);
            slideParticles.transform.localScale = transform.localScale;
            sliding = true;
            slideDelay = 2;
            StartCoroutine(slideTimer());
        }
        if(Input.GetKeyDown(KeyCode.W) && riposteDelay == 0) {
            animator.SetBool(IsRiposting, true);
        }
        //Dashing
        if(Input.GetKeyDown(KeyCode.E) && dashEnabled && !dashing && dashDelay == 0 && !sliding && !hanging() && !stomping)
        {
            if (runningParticles.isPlaying)
            {
                runningParticles.Stop();
            }
            animator.SetTrigger(dash);

            dashing = true;
            dashDelay = 2;
            StartCoroutine(dashTimer());
        }
        swingDelay = swingDelay <= 0 ? 0 : swingDelay - Time.deltaTime;
        stompDelay = stompDelay <= 0 ? 0 : stompDelay - Time.deltaTime;
        throwDelay = throwDelay <= 0 ? 0 : throwDelay - Time.deltaTime;
        dashDelay = dashDelay <= 0 ? 0 : dashDelay - Time.deltaTime;
        slideDelay = slideDelay <= 0 ? 0 : slideDelay - Time.deltaTime;
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

    public bool ableToSlide()
    {
        // slideCollider.GetContacts(new List<Collider2D>());
        RaycastHit2D collision = Physics2D.Raycast(transform.position + new Vector3(slideCollider.offset.x, slideCollider.offset.y, 0), transform.localScale.x < 1 ? Vector2.left : Vector2.right, 0.5f, layermask);
        return !collision;
    }

    public void startHardParry()
    {
        hardParrying = true;
        hardParrySpark = Instantiate(hardParrySparkPrefab, transform.position + new Vector3(transform.localScale.x < 0 ? -0.4f : 0.4f,0.2f,0), Quaternion.identity);
        hardParryBar = Instantiate(parryBarPrefab, transform.position + new Vector3(transform.localScale.x < 0 ? -0.5f : 0.5f,1f,0), Quaternion.identity);
        healthBar = GameObject.Find("Health Bar");
        healthBar.SetActive(false);
        healthMeter = GameObject.Find("Health Meter");
        healthMeter.SetActive(false);
        subWeaponDisplay = GameObject.Find("Sub Weapon Display");
        subWeaponDisplay.SetActive(false);
        comboDisplay = GameObject.Find("Combo");
        comboDisplay.SetActive(false);
        animator.SetBool(IsHardParrying, true);
    }

    public void endHardParry(bool win)
    {
        hardParrying = false;
        GameObject.Destroy(hardParrySpark);
        GameObject.Destroy(hardParryBar);
        subWeaponDisplay.SetActive(true);
        healthBar.SetActive(true);
        healthMeter.SetActive(true);
        comboDisplay.SetActive(true);
        animator.SetBool(IsHardParrying, false);
        hardParryEnemy.GetComponent<EnemyAI>().endHardParry();
        if(win) 
        {
            hardParryEnemy.GetComponent<Enemy>().impact(5, hardParryEnemy.transform.position);
        }
        else 
        {
            gameObject.GetComponent<Health>().damageTaken(null);
        }
        Camera.main.GetComponent<Zoom>().StartCoroutine(Camera.main.GetComponent<Zoom>().endParryZoom());
    }

    public void handleHitBox()
    {
        if (dashing)
        {
            // slideCollider.enabled = false;
            // baseCollider.enabled = false;
            // dashCollider.enabled = true;
        }
        else if (hanging())
        {
            baseCollider.enabled = false;
            dashCollider.enabled = false;
            slideCollider.enabled = false;
            hangingCollider.enabled = true;
        }
        else if (animator.GetBool(IsSliding))
        {
            baseCollider.enabled = false;
            dashCollider.enabled = false;
            slideCollider.enabled = true;
            hangingCollider.enabled = false;
        }
        else if(hardParrying)
        {
            baseCollider.enabled = false;
            dashCollider.enabled = false;
            slideCollider.enabled = false;
            hangingCollider.enabled = false;
        }
        else
        {
            baseCollider.enabled = true;
            dashCollider.enabled = false;
            slideCollider.enabled = false;
            hangingCollider.enabled = false;
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
            col.collider.gameObject.GetComponent<Pickup>().pickup();
        }
    }

    public IEnumerator doubleJumpTimer()
    {
        doubleJump = true;
        yield return new WaitForSeconds(0.2f);
        doubleJump = false;
    }

    public void useStomp()
    {
        if(!stomping && stompDelay == 0 && grounded)
        {
            stompDelay = 2;
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
            throwDelay = 1;
            animator.SetTrigger(throwTrigger);
            Instantiate(stakePrefab, GameObject.Find("Player").transform.position, Quaternion.identity);
        }
    }

    public void FixedUpdate()
    {
        if (jumpingIntention || doubleJumpingIntention)
        {
            _rigidbody.AddForce(Vector2.up * (jumpingIntention ? 4f : 6f), ForceMode2D.Impulse);
            jumpingIntention = false;
            doubleJumpingIntention = false;
        }
        else if (jumpBoostIntention)
        {
            _rigidbody.AddForce(Vector2.up * jumpAmount, ForceMode2D.Impulse);
        }
        if (sliding)
        {
            _rigidbody.linearVelocity = new Vector2(transform.localScale.x > 0 ? 7 : -7, 0);
        }
        else if (dashing)
        {
            if (dashDelay > 1.82f)
            {
                _rigidbody.linearVelocity = new Vector2(0, 0);
            }
            else if(dashDelay > 1.7f)
            {
                _rigidbody.linearVelocity = new Vector2(transform.localScale.x > 0 ? 15 : -15, 0); 
            }
            else if(dashDelay > 1.6f)
            {
               _rigidbody.linearVelocity = new Vector2(transform.localScale.x > 0 ? 10 : -10, 0); 
            }
            else
            {
               _rigidbody.linearVelocity = new Vector2(transform.localScale.x > 0 ? 5 : -5, 0); 
            }
        }
        else if (stomping)
        {
            _rigidbody.linearVelocity = new Vector2(0, 0);
        }
        else if (animator.GetCurrentAnimatorStateInfo(0).IsName("Backflip") || (animator.GetCurrentAnimatorStateInfo(0).IsName("Hurt")))
        {
            _rigidbody.linearVelocity = new Vector2(clampVelocity(_rigidbody.linearVelocity.x, 4), clampVelocity(_rigidbody.linearVelocity.y, 4));
        }
        else
        {
            _rigidbody.linearVelocity = new Vector2(clampVelocity(_currentVelocity, 4), clampVelocity(_rigidbody.linearVelocity.y, 12));
        }
        if(hanging())
        {
            destroyChargeFlames();
            _rigidbody.linearVelocity = new Vector2(0,0);
            transform.position = hangingOn ? hangingOn.transform.position + new Vector3((transform.localScale.x > 0 ? -0.7f : 0.7f), hangingPoint.y, transform.position.z) : hangingPoint;
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
        if(stage == 1)
        {
            chargeFlames.Add(Instantiate(chargeParticlesPrefab, transform.position + new Vector3(0f,0f,1), Quaternion.identity, transform));
        }
        else
        {
            chargeFlames.Add(Instantiate(chargeParticlesBluePrefab, transform.position + new Vector3(0f,0f,1), Quaternion.identity, transform));
        }
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
        float rayLength = 0.11f; // Adjust based on your character's size
        RaycastHit2D collision = Physics2D.Raycast(groundCheck.transform.position, Vector2.down, rayLength, LayerMask.GetMask("Ground"));
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
        yield return new WaitForSeconds(0.2f);
        GameObject dashSparks = Instantiate(dashSparksPrefab, transform.position, Quaternion.identity, transform);
        yield return new WaitForSeconds(0.3f);
        _currentVelocity = 0;
        dashing = false;
        dashSparks.GetComponent<ParticleSystem>().emissionRate = 0;
        yield return new WaitForSeconds(1f);
        Destroy(dashSparks);
    }

    public IEnumerator slideTimer()
    {
        yield return new WaitForSeconds(0.5f);
        animator.SetBool(IsSliding, false);
        _currentVelocity = 0;
        sliding = false;
        Destroy(slideParticles);

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