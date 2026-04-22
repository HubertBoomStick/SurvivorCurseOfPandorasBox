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

    [Header("Distance Ranges")]
    [SerializeField] private float closeRange = 10f;
    [SerializeField] private float midRange = 15f;
    [SerializeField] private float farRange = 20f;

    [Header("Movement")]
    [SerializeField] private float chaseMoveSpeed = 3f;
    [SerializeField] private float chaseStopDistance = 3f;
    [SerializeField] private float chaseDuration = 2.5f;
    [SerializeField] private float lingerMinTime = 3f;
    [SerializeField] private float lingerMaxTime = 4f;
    [SerializeField] private float repositionDistance = 2.5f;
    [SerializeField] private float repositionSpeed = 4f;
    [SerializeField] private float faceDeadZone = 1.5f;

    [Header("Melee Attack Setup")]
    [SerializeField] private float bitePreferredRange = 2.8f;
    [SerializeField] private float clawPreferredRange = 3.2f;
    [SerializeField] private float meleeApproachSpeed = 3.5f;
    [SerializeField] private float meleeRangeTolerance = 0.2f;
    [SerializeField] private float meleeApproachTimeout = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private float groundCheckForwardOffset = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    private DragonBoss boss;
    private bool phaseActive;
    private bool isBusy;
    private float cooldownTimer = Mathf.Infinity;

    private bool isChasingPlayer;
    private bool isLingering;
    private float movementStateTimer = 0f;

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

        if (!isBusy)
        {
            UpdateGroundMovement();
            UpdateFacing();
            UpdateMoveAnimation();

            if (cooldownTimer >= attackCooldown)
            {
                ChooseGroundAttack();
            }
        }
        else
        {
            UpdateMoveAnimation();
        }
    }

    public void BeginPhase()
    {
        phaseActive = true;
        isBusy = false;
        cooldownTimer = attackCooldown;

        StartChaseState();
        UpdateFacing(true);
        SetMoving(false);
    }

    public void StopPhase()
    {
        phaseActive = false;
        isBusy = false;

        isChasingPlayer = false;
        isLingering = false;
        movementStateTimer = 0f;

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

    private void UpdateGroundMovement()
    {
        movementStateTimer -= Time.deltaTime;

        if (PlayerInsideBodyRange())
        {
            RepositionAwayFromPlayer();
            return;
        }

        if (isLingering)
        {
            SetMoving(false);

            if (movementStateTimer <= 0f)
                StartChaseState();

            return;
        }

        if (isChasingPlayer)
        {
            ChasePlayer();

            if (movementStateTimer <= 0f)
                StartLingerState();
        }
        else
        {
            StartChaseState();
        }
    }

    private void StartChaseState()
    {
        isChasingPlayer = true;
        isLingering = false;
        movementStateTimer = chaseDuration;
    }

    private void StartLingerState()
    {
        isChasingPlayer = false;
        isLingering = true;
        movementStateTimer = Random.Range(lingerMinTime, lingerMaxTime);
    }

    private void ChasePlayer()
    {
        float xDistance = player.position.x - transform.position.x;
        float absXDistance = Mathf.Abs(xDistance);

        if (absXDistance <= chaseStopDistance)
        {
            SetMoving(false);
            return;
        }

        float moveDirection = xDistance > 0f ? 1f : -1f;

        if (!HasGroundAhead(moveDirection))
        {
            SetMoving(false);
            return;
        }

        Vector3 move = new Vector3(moveDirection * chaseMoveSpeed * Time.deltaTime, 0f, 0f);
        transform.position += move;
        SetMoving(true);
    }

    private void UpdateFacing(bool force = false)
    {
        float xDistance = player.position.x - transform.position.x;

        if (force)
        {
            currentFacingDirection = xDistance >= 0f ? 1f : -1f;
        }
        else
        {
            if (xDistance > faceDeadZone)
                currentFacingDirection = 1f;
            else if (xDistance < -faceDeadZone)
                currentFacingDirection = -1f;
        }

        FaceDirection(currentFacingDirection);
    }

    private void FaceDirection(float direction)
    {
        Vector3 flatDir = direction > 0f ? Vector3.right : Vector3.left;

        if (flatDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDir);
            transform.rotation = targetRotation * Quaternion.Euler(facingRotationOffset);
        }
    }

    private bool PlayerInsideBodyRange()
    {
        float xDistance = Mathf.Abs(player.position.x - transform.position.x);
        return xDistance < repositionDistance;
    }

    private void RepositionAwayFromPlayer()
    {
        float directionAway = player.position.x > transform.position.x ? -1f : 1f;

        if (!HasGroundAhead(directionAway))
        {
            SetMoving(false);
            return;
        }

        Vector3 move = new Vector3(directionAway * repositionSpeed * Time.deltaTime, 0f, 0f);
        transform.position += move;
        SetMoving(true);
    }

    private void ChooseGroundAttack()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        int attackIndex;

        if (distance <= closeRange)
        {
            // close = Bite and Claw favored, no run
            attackIndex = GetWeightedAttack(10, 10, 0, 40, 40);
        }
        else if (distance <= midRange)
        {
            // 15 = FireBreath favored, no bite/claw
            attackIndex = GetWeightedAttack(25, 55, 20, 0, 0);
        }
        else
        {
            // 20 or higher = Run + Spit only
            attackIndex = GetWeightedAttack(55, 0, 45, 0, 0);
        }

        isBusy = true;
        cooldownTimer = 0f;

        switch (attackIndex)
        {
            case 0:
                groundSpitAttack.Execute();
                break;

            case 1:
                groundBreathAttack.Execute();
                break;

            case 2:
                if (CanUseRunAttack())
                    runAttack.Execute();
                else
                {
                    isBusy = false;
                    cooldownTimer = 0f;
                }
                break;

            case 3:
                StartMeleeAttack(bitePreferredRange, true);
                break;

            case 4:
                StartMeleeAttack(clawPreferredRange, false);
                break;
        }
    }

    private bool CanUseRunAttack()
    {
        float xDistance = player.position.x - transform.position.x;
        float moveDirection = xDistance > 0f ? 1f : -1f;
        return HasGroundAhead(moveDirection);
    }

    private void StartMeleeAttack(float preferredRange, bool useBite)
    {
        if (meleeApproachRoutine != null)
            StopCoroutine(meleeApproachRoutine);

        meleeApproachRoutine = StartCoroutine(MeleeApproachRoutine(preferredRange, useBite));
    }

    private IEnumerator MeleeApproachRoutine(float preferredRange, bool useBite)
    {
        float timer = 0f;

        while (timer < meleeApproachTimeout)
        {
            if (player == null)
                break;

            timer += Time.deltaTime;

            float xDistance = player.position.x - transform.position.x;
            float absXDistance = Mathf.Abs(xDistance);

            UpdateFacing(true);

            if (absXDistance <= preferredRange + meleeRangeTolerance &&
                absXDistance >= preferredRange - meleeRangeTolerance)
            {
                break;
            }

            float moveDirection = xDistance > 0f ? 1f : -1f;

            if (!HasGroundAhead(moveDirection))
            {
                break;
            }

            if (absXDistance > preferredRange)
            {
                transform.position += new Vector3(moveDirection * meleeApproachSpeed * Time.deltaTime, 0f, 0f);
                SetMoving(true);
            }
            else
            {
                float backDirection = -moveDirection;

                if (!HasGroundAhead(backDirection))
                    break;

                transform.position += new Vector3(backDirection * meleeApproachSpeed * Time.deltaTime, 0f, 0f);
                SetMoving(true);
            }

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

    private void UpdateMoveAnimation()
    {
        // SetMoving handles the bool updates directly
    }

    private int GetWeightedAttack(int spit, int breath, int run, int bite, int claw)
    {
        int total = spit + breath + run + bite + claw;
        int roll = Random.Range(0, total);

        if (roll < spit)
            return 0;
        roll -= spit;

        if (roll < breath)
            return 1;
        roll -= breath;

        if (roll < run)
            return 2;
        roll -= run;

        if (roll < bite)
            return 3;

        return 4;
    }

    public DragonBoss GetBoss() => boss;
    public Transform GetPlayer() => player;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chaseStopDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, repositionDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, closeRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, midRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, farRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, bitePreferredRange);

        Gizmos.color = new Color(1f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, clawPreferredRange);

        if (player != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, player.position);
        }

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