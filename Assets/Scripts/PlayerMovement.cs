using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour, IDamage
{
    [SerializeField] int HP;
    int HPOrig;

    [Header("Life Steal Bar")]
    [SerializeField] private int lifeSteal = 0;
    [SerializeField] private int maxLifeSteal = 10;
    [SerializeField] private int lifeStealPerHit = 1;
    [SerializeField] private int runAttackLifeStealGain = 5;

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

    [Header("Sprint / Run")]
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Crouch")]
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;

    [Header("Capsule Settings")]
    [SerializeField] private float standingCapsuleHeight = 2.2f;
    [SerializeField] private Vector3 standingCapsuleCenter = new Vector3(0f, 1.1f, 0f);
    [SerializeField] private float crouchCapsuleHeight = 1.4f;
    [SerializeField] private Vector3 crouchCapsuleCenter = new Vector3(0f, 0.7f, 0f);
    [SerializeField] private float slideCapsuleHeight = 1.0f;
    [SerializeField] private Vector3 slideCapsuleCenter = new Vector3(0f, 0.5f, 0f);

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
    [SerializeField] private int runAttackDamage = 3;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Flash")]
    [SerializeField] private Renderer[] playerRenderers;
    [SerializeField] private Color healFlashColor = Color.green;
    [SerializeField] private float healFlashDuration = 0.25f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.15f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private int jumpsLeft;

    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool isTouchingWall;
    private bool wasTouchingWall;
    private bool isWallSliding;
    private bool isCrouching;
    private bool isSliding;
    private bool isKnockedBack;
    private bool isDashing;
    private bool isFloating;

    private float moveInput;
    private float attackTimer;
    private float knockbackTimer;
    private float dashTimer;
    private float dashCooldownTimer;
    private float slideTimer;
    private float jumpTimer;

    private enum AttackType
    {
        None,
        Horizontal,
        Up,
        Down,
        Run
    }

    private AttackType currentAttackType = AttackType.None;
    private bool attackHasHit;

    private Color[][] originalRendererColors;
    private Coroutine flashRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        if (playerRenderers == null || playerRenderers.Length == 0)
            playerRenderers = GetComponentsInChildren<Renderer>();

        SaveOriginalRendererColors();

        if (capsule != null)
        {
            standingCapsuleHeight = capsule.height;
            standingCapsuleCenter = capsule.center;
        }
    }

    private void Start()
    {
        HPOrig = HP;
        updatePlayerUI();

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

        if (!isKnockedBack)
        {
            if (Input.GetKeyDown(KeyCode.Space) && !isSliding)
            {
                if (isTouchingWall && !isGrounded)
                {
                    WallJump();
                }
                else if (jumpsLeft > 0)
                {
                    bool isDoubleJump = !isGrounded;
                    Jump(isDoubleJump);
                    jumpsLeft--;
                }
            }

            isFloating = !isGrounded && jumpsLeft == 0 && Input.GetKey(KeyCode.Space) && rb.linearVelocity.y < 0;

            if (Input.GetKeyDown(KeyCode.E) && lifeSteal >= maxLifeSteal)
                HealInstant();

            if (attackTimer > 0)
                attackTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(0) && attackTimer <= 0 && !isSliding)
            {
                if (Input.GetKey(KeyCode.W))
                    AttackUp();
                else if (Input.GetKey(KeyCode.S))
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
                StartDash();

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

        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!isKnockedBack && !isDashing)
            Move();

        if (isWallSliding && rb.linearVelocity.y < -wallSlideSpeed)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallSlideSpeed, rb.linearVelocity.z);

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
            bool running = Input.GetKey(sprintKey) && Mathf.Abs(moveInput) > 0.1f && !isCrouching;

            if (running)
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

    private void Jump(bool isDoubleJump)
    {
        isGrounded = false;
        wasGroundedLastFrame = false;
        jumpTimer = 0f;

        if (isDoubleJump)
            TriggerAnim("DoubleJump");
        else
            TriggerAnim("Jump");
    }

    public void JumpForceEvent()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpForce;
        rb.linearVelocity = velocity;
    }

    private void CheckGround()
    {
        if (jumpTimer < jumpCD)
        {
            jumpTimer += Time.deltaTime;
            isGrounded = false;
            wasGroundedLastFrame = false;
            return;
        }

        bool groundedNow = Physics.CheckSphere(feetPosition.position, groundCheckRadius, groundLayer);

        if (groundedNow && !wasGroundedLastFrame)
        {
            jumpsLeft = maxJumps;
            isFloating = false;
        }

        isGrounded = groundedNow;
        wasGroundedLastFrame = groundedNow;
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
        SetCapsuleCrouch();
    }

    private void StopCrouch()
    {
        isCrouching = false;
        SetCapsuleStanding();
    }

    private void StartSlide()
    {
        isSliding = true;
        isCrouching = false;
        slideTimer = slideDuration;

        SetCapsuleSlide();
        TriggerAnim("Slide");
    }

    private void StopSlide()
    {
        isSliding = false;
        isCrouching = false;

        SetCapsuleStanding();

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);
    }

    private void SetCapsuleCrouch()
    {
        if (capsule == null) return;

        capsule.height = crouchCapsuleHeight;
        capsule.center = crouchCapsuleCenter;
    }

    private void SetCapsuleSlide()
    {
        if (capsule == null) return;

        capsule.height = slideCapsuleHeight;
        capsule.center = slideCapsuleCenter;
    }

    private void SetCapsuleStanding()
    {
        if (capsule == null) return;

        capsule.height = standingCapsuleHeight;
        capsule.center = standingCapsuleCenter;
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

        bool hasMoveInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f;
        bool canMoveAnimate = isGrounded && !isSliding && !isCrouching;

        bool running = hasMoveInput && Input.GetKey(sprintKey) && canMoveAnimate;
        bool walking = hasMoveInput && !Input.GetKey(sprintKey) && canMoveAnimate;

        bool falling = !isGrounded && rb.linearVelocity.y < -0.1f;
        bool hanging = isWallSliding;
        bool crouching = isCrouching && !isSliding;
        bool crouchWalking = isCrouching && hasMoveInput && isGrounded && !isSliding;
        bool sliding = isSliding;

        animator.SetBool("IsWalk", walking);
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
        animator.ResetTrigger("DoubleJump");
        animator.ResetTrigger("Slide");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("AttackUp");
        animator.ResetTrigger("AttackDown");
        animator.ResetTrigger("RunAttack");

        animator.SetTrigger(triggerName);
    }

    private void Attack()
    {
        bool runningAttack =
            Mathf.Abs(moveInput) > 0.1f &&
            Input.GetKey(sprintKey) &&
            isGrounded &&
            !isCrouching &&
            !isSliding;

        attackHasHit = false;

        if (runningAttack)
        {
            currentAttackType = AttackType.Run;
            TriggerAnim("RunAttack");
        }
        else
        {
            currentAttackType = AttackType.Horizontal;
            TriggerAnim("Attack");
        }
    }

    private void AttackUp()
    {
        currentAttackType = AttackType.Up;
        attackHasHit = false;

        TriggerAnim("AttackUp");
    }

    private void AttackDown()
    {
        currentAttackType = AttackType.Down;
        attackHasHit = false;

        TriggerAnim("AttackDown");
    }

    public void HorizontalAttackHitEvent()
    {
        if (currentAttackType != AttackType.Horizontal) return;
        if (attackHasHit) return;

        attackHasHit = true;

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
        DealDamageAmount(hits, attackDamage);
    }

    public void RunAttackHitEvent()
    {
        if (currentAttackType != AttackType.Run) return;
        if (attackHasHit) return;

        attackHasHit = true;

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
        DealDamageAmount(hits, runAttackDamage, runAttackLifeStealGain);
    }

    public void UpAttackHitEvent()
    {
        if (currentAttackType != AttackType.Up) return;
        if (attackHasHit) return;

        attackHasHit = true;

        Vector3 attackPos = attackPoint.position + Vector3.up * attackRange;
        Collider[] hits = Physics.OverlapSphere(attackPos, attackRange, enemyLayer);

        DealDamageAmount(hits, attackDamage);
    }

    public void DownAttackHitEvent()
    {
        if (currentAttackType != AttackType.Down) return;
        if (attackHasHit) return;

        attackHasHit = true;

        Vector3 attackPos = attackPoint.position + Vector3.down * attackRange;
        Collider[] hits = Physics.OverlapSphere(attackPos, attackRange, enemyLayer);

        DealDamageAmount(hits, attackDamage);
    }

    public void EndAttackEvent()
    {
        currentAttackType = AttackType.None;
        attackHasHit = false;
    }

    private void DealDamageAmount(Collider[] hits, int damageAmount, int lifeStealGain = -1)
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
                damageable.takeDamage(damageAmount);
                hitSomething = true;
            }
        }

        if (hitSomething)
        {
            if (lifeStealGain < 0)
                lifeStealGain = lifeStealPerHit;

            AddLifeSteal(lifeStealGain);
        }
    }

    private void AddLifeSteal(int amount)
    {
        lifeSteal += amount;

        if (lifeSteal > maxLifeSteal)
            lifeSteal = maxLifeSteal;

        gamemanager.instance.updateLifeStealUI(lifeSteal, maxLifeSteal);
    }

    private void HealInstant()
    {
        HP = HPOrig;
        updatePlayerUI();

        lifeSteal = 0;
        gamemanager.instance.updateLifeStealUI(lifeSteal, maxLifeSteal);

        FlashColor(healFlashColor, healFlashDuration);
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        updatePlayerUI();

        FlashColor(damageFlashColor, damageFlashDuration);

        if (HP <= 0)
            gamemanager.instance.youLose();
    }

    private void FlashColor(Color color, float duration)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        SetRendererColor(color);

        yield return new WaitForSeconds(duration);

        RestoreOriginalRendererColors();

        flashRoutine = null;
    }

    private void SaveOriginalRendererColors()
    {
        if (playerRenderers == null) return;

        originalRendererColors = new Color[playerRenderers.Length][];

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] == null)
                continue;

            Material[] materials = playerRenderers[i].materials;
            originalRendererColors[i] = new Color[materials.Length];

            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j].HasProperty("_Color"))
                    originalRendererColors[i][j] = materials[j].color;
            }
        }
    }

    private void SetRendererColor(Color color)
    {
        if (playerRenderers == null) return;

        foreach (Renderer rend in playerRenderers)
        {
            if (rend == null) continue;

            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                    mat.color = color;
            }
        }
    }

    private void RestoreOriginalRendererColors()
    {
        if (playerRenderers == null || originalRendererColors == null) return;

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] == null)
                continue;

            Material[] materials = playerRenderers[i].materials;

            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j].HasProperty("_Color") && originalRendererColors[i] != null && j < originalRendererColors[i].Length)
                    materials[j].color = originalRendererColors[i][j];
            }
        }
    }

    public void ApplyKnockback(Vector3 force)
    {
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);
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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position + Vector3.up * attackRange, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(attackPoint.position + Vector3.down * attackRange, attackRange);
    }
}