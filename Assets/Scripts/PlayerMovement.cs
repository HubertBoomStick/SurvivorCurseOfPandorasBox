using UnityEngine;

public class PlayerMovement : MonoBehaviour, IDamage
{
    [SerializeField] int HP;
    int HPOrig;

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
    [SerializeField] private float slideDuration = 3f;

    [Header("Double Jump")]
    [SerializeField] private int maxJumps = 2;

    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(5f, 7f);

    [Header("Wall Slide")]
    [SerializeField] private float wallSlideSpeed = 2f;

    [Header("Dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.E;
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

    private Rigidbody rb;
    private Animator animator;

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

    private void Start()
    {
        HPOrig = HP;
        updatePlayerUI();
        originalScale = transform.localScale;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // knockback timer
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
            isSprinting = Input.GetKey(sprintKey) && isGrounded;

            // Jump
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

                // Float after second jump
                if (!isGrounded && jumpsLeft == 0 && Input.GetKey(KeyCode.Space) && rb.linearVelocity.y < 0)
                    isFloating = true;
                else
                    isFloating = false;
            }

            // Attack cooldown
            if (attackTimer > 0)
                attackTimer -= Time.deltaTime;

            // Attack input
            if (Input.GetMouseButtonDown(0) && attackTimer <= 0)
            {
                Attack();
                attackTimer = attackCooldown;
            }

            // Rotate
            if (moveInput > 0)
                transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            else if (moveInput < 0)
                transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            // Slide or crouch
            if (Input.GetKeyDown(crouchKey))
            {
                if (isSprinting && isGrounded && !isSliding)
                    StartSlide();
                else
                    StartCrouch();
            }

            if (Input.GetKeyUp(crouchKey) && !isSliding)
                StopCrouch();

            // Dash
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

        // Slide timer
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
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
            currentSpeed = slideSpeed;
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

    private void Jump()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpForce;
        rb.linearVelocity = velocity;

        isGrounded = false;
        jumpTimer = 0f;

        if (animator != null)
            animator.SetBool("IsJump", true);
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
            jumpsLeft = maxJumps;

        if (isGrounded && animator != null)
            animator.SetBool("IsJump", false);
    }

    private void CheckWall()
    {
        bool touchingWallNow =
            Physics.Raycast(transform.position, Vector3.right, wallCheckDistance, wallLayer) ||
            Physics.Raycast(transform.position, Vector3.left, wallCheckDistance, wallLayer);

        // Give one jump when first touching wall
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
    }

    private void StopSlide()
    {
        isSliding = false;
        StopCrouch();
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool isRunning = Mathf.Abs(rb.linearVelocity.x) > 0.1f && isGrounded && !isSliding;
        animator.SetBool("IsRun", isRunning);
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

    private void Attack()
    {
        Collider[] hits = Physics.OverlapSphere(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            IDamage damageable = hit.GetComponent<IDamage>();
            if (damageable != null)
            {
                damageable.takeDamage(attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}