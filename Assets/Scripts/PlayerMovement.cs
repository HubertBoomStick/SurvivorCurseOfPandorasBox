using UnityEngine;

public class PlayerMovement : MonoBehaviour, IDamage
{
    [SerializeField] int HP;
    int HPOrig;

    [Header("Life Steal Bar")]
    [SerializeField] private int lifeSteal = 0;
    [SerializeField] private int maxLifeSteal = 10;
    [SerializeField] private int lifeStealPerHit = 1;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Ground Check")]
    [SerializeField] private Transform feetPosition;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Jump Settings")]
    [SerializeField] private float jumpCD = 0.1f;
    [SerializeField] private float fallMultiplier = 2f;

    [Header("Sprint")]
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Crouch")]
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private Vector3 crouchScale = new Vector3(1, 0.5f, 1);

    [Header("Slide")]
    [SerializeField] private float slideSpeed = 12f;
    [SerializeField] private float slideDuration = 0.8f;

    [Header("Double Jump")]
    [SerializeField] private int maxJumps = 2;

    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(5f, 7f);

    [Header("Wall Slide / Hang")]
    [SerializeField] private float wallSlideSpeed = 2f;

    [Header("Dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.Q;
    [SerializeField] private float dashForce = 15f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Float")]
    [SerializeField] private float floatFallSpeed = 1.5f;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 0.3f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private Vector3 originalScale;

    private int jumpsLeft;

    private bool isWallSliding;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isSprinting;
    private bool isCrouching;
    private bool isSliding;
    private bool isKnockedBack;
    private bool isDashing;
    private bool isFloating;
    private bool wasTouchingWall;

    private float attackTimer;
    private float knockbackTimer;
    private float dashTimer;
    private float dashCooldownTimer;
    private float slideTimer;
    private float jumpTimer;
    private float moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void Start()
    {
        HPOrig = HP;
        updatePlayerUI();

        originalScale = transform.localScale;
        jumpsLeft = maxJumps;

        gamemanager.instance.updateLifeStealUI(lifeSteal, maxLifeSteal);
    }

    private void Update()
    {
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;

            if (knockbackTimer <= 0f)
                isKnockedBack = false;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        CheckGround();
        CheckWall();
        UpdateAnimations();

        if (!isKnockedBack)
        {
            isSprinting = Input.GetKey(sprintKey) && isGrounded && !isCrouching && !isSliding;

            if (Input.GetKeyDown(KeyCode.Space) && !isSliding)
            {
                if (isTouchingWall && !isGrounded)
                {
                    WallJump();
                }
                else if (jumpsLeft > 0)
                {
                    Jump();
                    jumpsLeft--;
                }

                if (!isGrounded && jumpsLeft == 0 && Input.GetKey(KeyCode.Space) && rb.linearVelocity.y < 0)
                    isFloating = true;
                else
                    isFloating = false;
            }

            if (Input.GetKeyDown(KeyCode.E) && lifeSteal >= maxLifeSteal)
            {
                HealFull();
            }

            if (attackTimer > 0)
                attackTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(0) && attackTimer <= 0 && !isSliding)
            {
                bool holdingW = Input.GetKey(KeyCode.W);
                bool holdingS = Input.GetKey(KeyCode.S);

                if (holdingW)
                    AttackUp();
                else if (holdingS)
                    AttackDown();
                else
                    Attack();

                attackTimer = attackCooldown;
            }

            HandleFacingDirection();

            if (Input.GetKeyDown(crouchKey))
            {
                if (Input.GetKey(sprintKey) && isGrounded && !isSliding)
                    StartSlide();
                else
                    StartCrouch();
            }

            if (Input.GetKeyUp(crouchKey) && !isSliding)
                StopCrouch();

            if (Input.GetKeyDown(dashKey) && dashCooldownTimer <= 0 && !isSliding)
            {
                StartDash();
            }

            if (dashCooldownTimer > 0)
                dashCooldownTimer -= Time.deltaTime;

            if (isDashing)
            {
                dashTimer -= Time.deltaTime;

                if (dashTimer <= 0)
                    StopDash();
            }
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            if (slideTimer <= 0f)
                StopSlide();
        }
    }

    private void FixedUpdate()
    {
        if (!isKnockedBack && !isDashing)
            Move();

        if (isWallSliding && rb.linearVelocity.y < -wallSlideSpeed)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                -wallSlideSpeed,
                rb.linearVelocity.z
            );
        }

        if (rb.linearVelocity.y < 0)
        {
            if (isFloating)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    Mathf.Max(rb.linearVelocity.y, -floatFallSpeed),
                    rb.linearVelocity.z
                );
            }
            else
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
        }
    }

    private void Move()
    {
        float currentSpeed = moveSpeed;

        if (isSliding)
        {
            currentSpeed = slideSpeed;
        }
        else
        {
            if (isSprinting)
                currentSpeed *= sprintMultiplier;

            if (isCrouching)
                currentSpeed *= crouchSpeedMultiplier;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveInput * currentSpeed;
        rb.linearVelocity = velocity;
    }

    private void HandleFacingDirection()
    {
        if (moveInput > 0)
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        else if (moveInput < 0)
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
    }

    private void Jump()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpForce;
        rb.linearVelocity = velocity;

        isGrounded = false;
        jumpTimer = 0f;

        TriggerAnim("Jump");
    }

    private void CheckGround()
    {
        if (jumpTimer < jumpCD)
        {
            jumpTimer += Time.deltaTime;
            return;
        }

        isGrounded = Physics.CheckSphere(feetPosition.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            jumpsLeft = maxJumps;
            isFloating = false;
        }
    }

    private void CheckWall()
    {
        bool touchingWallNow =
            Physics.Raycast(transform.position, Vector3.right, wallCheckDistance, wallLayer) ||
            Physics.Raycast(transform.position, Vector3.left, wallCheckDistance, wallLayer);

        if (touchingWallNow && !wasTouchingWall)
        {
            if (jumpsLeft < 1)
                jumpsLeft = 1;
        }

        isTouchingWall = touchingWallNow;
        wasTouchingWall = touchingWallNow;

        isWallSliding = isTouchingWall && !isGrounded && rb.linearVelocity.y < 0;
    }

    private void WallJump()
    {
        float direction = -transform.forward.x;

        rb.linearVelocity = new Vector3(
            wallJumpForce.x * direction,
            wallJumpForce.y,
            0f
        );

        jumpsLeft = maxJumps - 1;

        TriggerAnim("Jump");
    }

    private void StartCrouch()
    {
        isCrouching = true;
        transform.localScale = crouchScale;
    }

    private void StopCrouch()
    {
        isCrouching = false;
        transform.localScale = originalScale;
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

        StartCrouch();
        TriggerAnim("Slide");
    }

    private void StopSlide()
    {
        isSliding = false;
        StopCrouch();

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        float direction = transform.eulerAngles.y > 180f ? -1f : 1f;

        rb.linearVelocity = new Vector3(
            dashForce * direction,
            0f,
            0f
        );
    }

    private void StopDash()
    {
        isDashing = false;
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool running = Mathf.Abs(rb.linearVelocity.x) > 0.1f && isGrounded && !isSliding && !isCrouching;
        bool falling = !isGrounded && rb.linearVelocity.y < -0.1f;
        bool hanging = isWallSliding;
        bool crouching = isCrouching && !isSliding;
        bool crouchWalking = isCrouching && Mathf.Abs(rb.linearVelocity.x) > 0.1f && isGrounded && !isSliding;
        bool sliding = isSliding;

        animator.SetBool("IsRun", running);
        animator.SetBool("IsFalling", falling);
        animator.SetBool("IsWallHang", hanging);
        animator.SetBool("IsCrouch", crouching);
        animator.SetBool("IsCrouchWalk", crouchWalking);
        animator.SetBool("IsSlide", sliding);
        animator.SetBool("IsGrounded", isGrounded);
    }

    private void TriggerAnim(string triggerName)
    {
        if (animator == null) return;

        animator.ResetTrigger("Jump");
        animator.ResetTrigger("Slide");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("AttackUp");
        animator.ResetTrigger("AttackDown");
        animator.ResetTrigger("RunAttack");
        animator.ResetTrigger("Heal");

        animator.SetTrigger(triggerName);
    }

    private void Attack()
    {
        bool runningAttack = Mathf.Abs(rb.linearVelocity.x) > 0.1f && isGrounded && !isCrouching && !isSliding;

        if (runningAttack)
            TriggerAnim("RunAttack");
        else
            TriggerAnim("Attack");

        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        DealDamage(hits);
    }

    private void AttackUp()
    {
        TriggerAnim("AttackUp");

        Vector3 attackPos = attackPoint.position + Vector3.up * attackRange;

        Collider[] hits = Physics.OverlapSphere(
            attackPos,
            attackRange,
            enemyLayer
        );

        DealDamage(hits);
    }

    private void AttackDown()
    {
        TriggerAnim("AttackDown");

        Vector3 attackPos = attackPoint.position + Vector3.down * attackRange;

        Collider[] hits = Physics.OverlapSphere(
            attackPos,
            attackRange,
            enemyLayer
        );

        DealDamage(hits);
    }

    private void DealDamage(Collider[] hits)
    {
        bool hitSomething = false;

        foreach (Collider hit in hits)
        {
            IDamage damageable = hit.GetComponent<IDamage>();

            if (damageable == null)
                damageable = hit.GetComponentInParent<IDamage>();

            if (damageable == null)
                damageable = hit.GetComponentInChildren<IDamage>();

            if (damageable != null)
            {
                damageable.takeDamage(attackDamage);
                hitSomething = true;
            }
        }

        if (hitSomething)
            AddLifeSteal(lifeStealPerHit);
    }

    private void AddLifeSteal(int amount)
    {
        lifeSteal += amount;

        if (lifeSteal > maxLifeSteal)
            lifeSteal = maxLifeSteal;

        gamemanager.instance.updateLifeStealUI(lifeSteal, maxLifeSteal);
    }

    private void HealFull()
    {
        HP = HPOrig;
        updatePlayerUI();

        lifeSteal = 0;
        gamemanager.instance.updateLifeStealUI(lifeSteal, maxLifeSteal);

        TriggerAnim("Heal");
    }

    public void ApplyKnockback(Vector3 force)
    {
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        updatePlayerUI();

        if (HP <= 0)
            gamemanager.instance.youLose();
    }

    public void updatePlayerUI()
    {
        gamemanager.instance.playerHPbar.fillAmount = (float)HP / HPOrig;
    }

    public void ResetHealth()
    {
        HP = HPOrig;
        updatePlayerUI();
    }

    public void ResetLifeSteal()
    {
        lifeSteal = 0;
        gamemanager.instance.updateLifeStealUI(lifeSteal, maxLifeSteal);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}