using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] CharacterController controller;
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

    private Rigidbody rb;
    private Animator animator;

    private bool isGrounded;
    private float jumpTimer;
    private float moveInput;

    private void Start()
    {
        HPOrig = HP;
        updatePlayerUI();
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        CheckGround();
        UpdateAnimations();

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        if (moveInput > 0)
        {
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }
        else if (moveInput < 0)
        {
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
    }

    private void FixedUpdate()
    {
        Move();

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void Move()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;
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
        {
            animator.SetBool("IsJump", true);
        }
    }

    private void CheckGround()
    {
        if (jumpTimer < jumpCD)
        {
            jumpTimer += Time.deltaTime;
            return;
        }

        isGrounded = Physics.CheckSphere(feetPosition.position, groundCheckRadius, groundLayer);

        if (isGrounded && animator != null)
        {
            animator.SetBool("IsJump", false);
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool isRunning = Mathf.Abs(rb.linearVelocity.x) > 0.1f && isGrounded;
        animator.SetBool("IsRun", isRunning);
    }

    private void OnDrawGizmosSelected()
    {
        if (feetPosition == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(feetPosition.position, groundCheckRadius);
    }

    public void takeDamage(int amount)
    {
        HP -= amount;
        updatePlayerUI();
     
        if (HP <= 0)
        {
            //player is dead
            gamemanager.instance.youLose();
        }
    }

    public void updatePlayerUI()
    {
        gamemanager.instance.playerHPbar.fillAmount = (float)HP / HPOrig;
    }

    //public void spawnPlayer()
    //{
    //    controller.transform.position = gamemanager.instance.playerSpawnPos.transform.position;
    //    Physics.SyncTransforms();
    //    HP = HPOrig;
    //    updatePlayerUI();
    //}

}