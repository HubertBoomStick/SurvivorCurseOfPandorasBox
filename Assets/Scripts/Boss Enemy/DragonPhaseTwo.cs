using System.Collections;
using UnityEngine;

public class DragonPhaseTwo : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private DragonGroundSpitAttack groundSpitAttack;
    [SerializeField] private DragonGroundBreathAttack groundBreathAttack;
    [SerializeField] private DragonRunAttack runAttack;
    [SerializeField] private DragonBiteAttack biteAttack;
    [SerializeField] private DragonClawAttack clawAttack;

    [Header("Phase 2 Settings")]
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private Vector3 facingRotationOffset = new Vector3(0f, 180f, 0f);

    [Header("Turning")]
    [SerializeField] private bool facePlayerWhileAttacking = true;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float faceDeadZone = 1.5f;

    [Header("Movement")]
    [SerializeField] private float chaseMoveSpeed = 3f;
    [SerializeField] private float chaseStopDistance = 3f;

    [Header("Melee Approach")]
    [SerializeField] private float biteAttackRange = 2.5f;
    [SerializeField] private float clawAttackRange = 3f;
    [SerializeField] private float meleeApproachSpeed = 3.5f;
    [SerializeField] private float meleeApproachTimeout = 2f;

    [Header("Anti Body Block")]
    [SerializeField] private float bodyBlockRange = 2.5f;
    [SerializeField] private float bodyBlockRunCooldown = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private float groundCheckForwardOffset = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    private DragonBoss boss;
    private bool phaseActive;
    private bool isBusy;
    private float cooldownTimer = Mathf.Infinity;
    private float bodyBlockTimer = Mathf.Infinity;
    private float currentFacingDirection = 1f;
    private Coroutine meleeApproachRoutine;

    public void SetBoss(DragonBoss dragonBoss)
    {
        boss = dragonBoss;
    }

    private void Awake()
    {
        if (groundSpitAttack == null)
            groundSpitAttack = GetComponent<DragonGroundSpitAttack>();

        if (groundBreathAttack == null)
            groundBreathAttack = GetComponent<DragonGroundBreathAttack>();

        if (runAttack == null)
            runAttack = GetComponent<DragonRunAttack>();

        if (biteAttack == null)
            biteAttack = GetComponent<DragonBiteAttack>();

        if (clawAttack == null)
            clawAttack = GetComponent<DragonClawAttack>();
    }

    private void Start()
    {
        if (groundSpitAttack != null)
            groundSpitAttack.SetPhase(this);

        if (groundBreathAttack != null)
            groundBreathAttack.SetPhase(this);

        if (runAttack != null)
            runAttack.SetPhase(this);

        if (biteAttack != null)
            biteAttack.SetPhase(this);

        if (clawAttack != null)
            clawAttack.SetPhase(this);
    }

    private void Update()
    {
        if (boss == null || boss.IsDead || !phaseActive)
            return;

        if (player == null)
            player = boss.Player;

        if (player == null)
            return;

        cooldownTimer += Time.deltaTime;
        bodyBlockTimer += Time.deltaTime;

        if (isBusy)
        {
            SetMoving(false);

            if (facePlayerWhileAttacking && (runAttack == null || !runAttack.IsRunning))
                FacePlayerSmooth();

            return;
        }

        FacePlayerSmooth();

        if (PlayerInsideDragonBody() && bodyBlockTimer >= bodyBlockRunCooldown)
        {
            ForceRunAttack();
            return;
        }

        float xDistance = player.position.x - transform.position.x;
        float absDistance = Mathf.Abs(xDistance);

        if (absDistance > chaseStopDistance)
        {
            ChasePlayer(xDistance);
            return;
        }

        SetMoving(false);

        if (cooldownTimer >= attackCooldown)
        {
            ChooseGroundAttack();
        }
    }

    public void BeginPhase()
    {
        phaseActive = true;
        isBusy = false;
        cooldownTimer = attackCooldown;
        bodyBlockTimer = bodyBlockRunCooldown;
        SetMoving(false);
        FacePlayerInstant();
    }

    public void StopPhase()
    {
        phaseActive = false;
        isBusy = false;

        if (meleeApproachRoutine != null)
        {
            StopCoroutine(meleeApproachRoutine);
            meleeApproachRoutine = null;
        }

        if (groundSpitAttack != null)
            groundSpitAttack.StopAttack();

        if (groundBreathAttack != null)
            groundBreathAttack.StopAttack();

        if (runAttack != null)
            runAttack.StopAttack();

        if (biteAttack != null)
            biteAttack.StopAttack();

        if (clawAttack != null)
            clawAttack.StopAttack();

        SetMoving(false);
    }

    public bool IsBusy()
    {
        return isBusy;
    }

    public void NotifyAttackFinished()
    {
        isBusy = false;
        cooldownTimer = 0f;
        SetMoving(false);
    }

    private void ChasePlayer(float xDistance)
    {
        float moveDirection = xDistance > 0f ? 1f : -1f;

        if (!HasGroundAhead(moveDirection))
        {
            SetMoving(false);
            return;
        }

        transform.position += new Vector3(moveDirection * chaseMoveSpeed * Time.deltaTime, 0f, 0f);
        SetMoving(true);
    }

    private void FacePlayerSmooth()
    {
        float xDistance = player.position.x - transform.position.x;

        if (xDistance > faceDeadZone)
            currentFacingDirection = 1f;
        else if (xDistance < -faceDeadZone)
            currentFacingDirection = -1f;

        Vector3 flatDir = currentFacingDirection > 0f ? Vector3.right : Vector3.left;

        Quaternion targetRotation =
            Quaternion.LookRotation(flatDir) * Quaternion.Euler(facingRotationOffset);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    private void FacePlayerInstant()
    {
        if (player == null)
            return;

        float xDistance = player.position.x - transform.position.x;
        currentFacingDirection = xDistance >= 0f ? 1f : -1f;

        Vector3 flatDir = currentFacingDirection > 0f ? Vector3.right : Vector3.left;

        transform.rotation =
            Quaternion.LookRotation(flatDir) * Quaternion.Euler(facingRotationOffset);
    }

    private bool PlayerInsideDragonBody()
    {
        if (player == null)
            return false;

        float xDistance = Mathf.Abs(player.position.x - transform.position.x);
        return xDistance <= bodyBlockRange;
    }

    private void ForceRunAttack()
    {
        if (runAttack == null)
            return;

        isBusy = true;
        cooldownTimer = 0f;
        bodyBlockTimer = 0f;
        SetMoving(false);

        runAttack.Execute();
    }

    private void ChooseGroundAttack()
    {
        isBusy = true;
        cooldownTimer = 0f;
        SetMoving(false);

        int attackIndex = Random.Range(0, 5);

        switch (attackIndex)
        {
            case 0:
                groundSpitAttack.Execute();
                break;

            case 1:
                groundBreathAttack.Execute();
                break;

            case 2:
                runAttack.Execute();
                break;

            case 3:
                StartMeleeApproach(true);
                break;

            case 4:
                StartMeleeApproach(false);
                break;
        }
    }

    private void StartMeleeApproach(bool useBite)
    {
        if (meleeApproachRoutine != null)
            StopCoroutine(meleeApproachRoutine);

        meleeApproachRoutine = StartCoroutine(MeleeApproachRoutine(useBite));
    }

    private IEnumerator MeleeApproachRoutine(bool useBite)
    {
        float timer = 0f;
        float wantedRange = useBite ? biteAttackRange : clawAttackRange;

        while (timer < meleeApproachTimeout)
        {
            if (player == null)
                break;

            timer += Time.deltaTime;

            float xDistance = player.position.x - transform.position.x;
            float absDistance = Mathf.Abs(xDistance);

            FacePlayerSmooth();

            if (absDistance <= wantedRange)
                break;

            float moveDirection = xDistance > 0f ? 1f : -1f;

            if (!HasGroundAhead(moveDirection))
                break;

            transform.position += new Vector3(moveDirection * meleeApproachSpeed * Time.deltaTime, 0f, 0f);
            SetMoving(true);

            yield return null;
        }

        SetMoving(false);

        if (useBite)
            biteAttack.Execute();
        else
            clawAttack.Execute();

        meleeApproachRoutine = null;
    }

    private bool HasGroundAhead(float direction)
    {
        if (groundCheckPoint == null)
            return true;

        Vector3 origin = groundCheckPoint.position;
        origin.x += direction * groundCheckForwardOffset;

        return Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void SetMoving(bool moving)
    {
        if (boss != null && boss.Anim != null)
            boss.Anim.SetBool("moving", moving);
    }

    public DragonBoss GetBoss()
    {
        return boss;
    }

    public Transform GetPlayer()
    {
        return player;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chaseStopDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, bodyBlockRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, biteAttackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, clawAttackRange);

        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.white;

            Vector3 leftCheck = groundCheckPoint.position + Vector3.left * groundCheckForwardOffset;
            Vector3 rightCheck = groundCheckPoint.position + Vector3.right * groundCheckForwardOffset;

            Gizmos.DrawLine(leftCheck, leftCheck + Vector3.down * groundCheckDistance);
            Gizmos.DrawLine(rightCheck, rightCheck + Vector3.down * groundCheckDistance);

            Gizmos.DrawSphere(leftCheck + Vector3.down * groundCheckDistance, 0.05f);
            Gizmos.DrawSphere(rightCheck + Vector3.down * groundCheckDistance, 0.05f);
        }
    }
}