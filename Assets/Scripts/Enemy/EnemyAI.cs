using System.Collections;
using UnityEngine;

public class Enemy_Sideways : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float movementDistance = 8f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float chaseStopDistance = 1.5f;
    [SerializeField] private float faceDeadZone = 0.5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float loseRange = 7f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Enemy Spacing")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float enemySpacingDistance = 1.2f;
    [SerializeField] private float enemySpacingRadius = 0.35f;

    [Header("Random Movement")]
    [SerializeField] private float minMoveTime = 0.5f;
    [SerializeField] private float maxMoveTime = 3f;
    [SerializeField] private float idleTime = 1f;

    [Header("Animation")]
    [SerializeField] private Animator anim;

    [Header("Rotation")]
    [SerializeField] private float leftRotation = 90f;
    [SerializeField] private float rightRotation = -90f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private float groundCheckForwardOffset = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Physics")]
    [SerializeField] private Rigidbody rb;

    private float leftEdge;
    private float rightEdge;
    private float moveTimer;

    private bool movingLeft;
    private bool isIdle;
    private bool isChasing;

    private Transform player;
    private float currentFacingDirection = 1f;

    private void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        leftEdge = transform.position.x - movementDistance;
        rightEdge = transform.position.x + movementDistance;

        PickNewDirection();
        FaceDirection(currentFacingDirection);
    }

    private void Update()
    {
        DetectPlayer();
    }

    private void FixedUpdate()
    {
        if (isChasing)
        {
            ChasePlayer();
            return;
        }

        Patrol();
    }

    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);

        if (hits.Length > 0)
        {
            player = hits[0].transform;
            isChasing = true;
        }

        if (isChasing && player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer > loseRange)
            {
                StopChasing();
            }
        }
    }

    private void ChasePlayer()
    {
        if (player == null)
        {
            StopChasing();
            return;
        }

        float xDistance = player.position.x - transform.position.x;

        if (xDistance > faceDeadZone)
            currentFacingDirection = 1f;
        else if (xDistance < -faceDeadZone)
            currentFacingDirection = -1f;

        FaceDirection(currentFacingDirection);

        if (Mathf.Abs(xDistance) <= chaseStopDistance)
        {
            if (anim != null)
                anim.SetBool("moving", false);

            return;
        }

        if (!HasGroundAhead(currentFacingDirection))
        {
            StopChasing();
            return;
        }

        if (EnemyTooCloseAhead(currentFacingDirection))
        {
            if (anim != null)
                anim.SetBool("moving", false);

            return;
        }

        Vector3 move = new Vector3(currentFacingDirection * chaseSpeed * Time.fixedDeltaTime, 0f, 0f);
        rb.MovePosition(rb.position + move);

        if (anim != null)
            anim.SetBool("moving", true);
    }

    private void Patrol()
    {
        if (isIdle)
        {
            if (anim != null)
                anim.SetBool("moving", false);

            return;
        }

        moveTimer -= Time.fixedDeltaTime;

        if (moveTimer <= 0f)
        {
            StartCoroutine(IdleThenMove());
            return;
        }

        float direction = movingLeft ? -1f : 1f;
        currentFacingDirection = direction;

        FaceDirection(direction);

        if (!HasGroundAhead(direction))
        {
            movingLeft = !movingLeft;
            return;
        }

        if (EnemyTooCloseAhead(direction))
        {
            if (anim != null)
                anim.SetBool("moving", false);

            return;
        }

        Vector3 move = new Vector3(direction * speed * Time.fixedDeltaTime, 0f, 0f);
        rb.MovePosition(rb.position + move);

        if (anim != null)
            anim.SetBool("moving", true);

        if (transform.position.x <= leftEdge)
            movingLeft = false;

        if (transform.position.x >= rightEdge)
            movingLeft = true;
    }

    private void FaceDirection(float direction)
    {
        if (direction < 0f)
            transform.rotation = Quaternion.Euler(0f, leftRotation, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, rightRotation, 0f);
    }

    private bool HasGroundAhead(float direction)
    {
        if (groundCheckPoint == null)
            return true;

        Vector3 origin = groundCheckPoint.position;
        origin.x += direction * groundCheckForwardOffset;

        return Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundLayer);
    }

    private bool EnemyTooCloseAhead(float direction)
    {
        Vector3 origin = transform.position + new Vector3(direction * enemySpacingDistance, 0f, 0f);

        Collider[] hits = Physics.OverlapSphere(origin, enemySpacingRadius, enemyLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject != gameObject)
                return true;
        }

        return false;
    }

    private void StopChasing()
    {
        isChasing = false;
        player = null;
        PickNewDirection();
    }

    private void PickNewDirection()
    {
        movingLeft = Random.value > 0.5f;
        currentFacingDirection = movingLeft ? -1f : 1f;
        moveTimer = Random.Range(minMoveTime, maxMoveTime);
    }

    private IEnumerator IdleThenMove()
    {
        isIdle = true;

        if (anim != null)
            anim.SetBool("moving", false);

        yield return new WaitForSeconds(idleTime);

        isIdle = false;
        PickNewDirection();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chaseStopDistance);

        Gizmos.color = Color.magenta;
        Vector3 leftSpaceCheck = transform.position + Vector3.left * enemySpacingDistance;
        Vector3 rightSpaceCheck = transform.position + Vector3.right * enemySpacingDistance;
        Gizmos.DrawWireSphere(leftSpaceCheck, enemySpacingRadius);
        Gizmos.DrawWireSphere(rightSpaceCheck, enemySpacingRadius);

        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;

            Vector3 leftCheck = groundCheckPoint.position + Vector3.left * groundCheckForwardOffset;
            Vector3 rightCheck = groundCheckPoint.position + Vector3.right * groundCheckForwardOffset;

            Gizmos.DrawLine(leftCheck, leftCheck + Vector3.down * groundCheckDistance);
            Gizmos.DrawLine(rightCheck, rightCheck + Vector3.down * groundCheckDistance);

            Gizmos.DrawSphere(leftCheck + Vector3.down * groundCheckDistance, 0.05f);
            Gizmos.DrawSphere(rightCheck + Vector3.down * groundCheckDistance, 0.05f);
        }
    }
}