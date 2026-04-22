using UnityEngine;

public class DragonPhaseOne : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private DragonGlideAttack glideAttack;
    [SerializeField] private DragonAirSpitAttack airSpitAttack;
    [SerializeField] private DragonAirBreathAttack airBreathAttack;

    [Header("Take Off")]
    [SerializeField] private float takeOffHeight = 6f;
    [SerializeField] private float takeOffSpeed = 4f;

    [Header("Attack Timing")]
    [SerializeField] private float attackCooldown = 5f;

    [Header("Flying Movement")]
    [SerializeField] private float chaseMoveSpeed = 4f;
    [SerializeField] private float chaseStopDistance = 6f;
    [SerializeField] private float chaseDuration = 2.5f;
    [SerializeField] private float lingerMinTime = 3f;
    [SerializeField] private float lingerMaxTime = 4f;

    [Header("Facing")]
    [SerializeField] private Vector3 facingRotationOffset = new Vector3(0f, 180f, 0f);

    private DragonBoss boss;
    private bool phaseActive;
    private bool isBusy;
    private bool isTakingOff;

    private bool isChasingPlayer;
    private bool isLingering;

    private Vector3 takeOffTargetPosition;
    private float cooldownTimer = Mathf.Infinity;
    private float movementStateTimer = 0f;

    public void SetBoss(DragonBoss dragonBoss)
    {
        boss = dragonBoss;
    }

    private void Awake()
    {
        if (glideAttack == null)
            glideAttack = GetComponent<DragonGlideAttack>();

        if (airSpitAttack == null)
            airSpitAttack = GetComponent<DragonAirSpitAttack>();

        if (airBreathAttack == null)
            airBreathAttack = GetComponent<DragonAirBreathAttack>();
    }

    private void Start()
    {
        if (glideAttack != null)
            glideAttack.SetPhase(this);

        if (airSpitAttack != null)
            airSpitAttack.SetPhase(this);

        if (airBreathAttack != null)
            airBreathAttack.SetPhase(this);
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

        if (isTakingOff)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                takeOffTargetPosition,
                takeOffSpeed * Time.deltaTime
            );

            return;
        }

        if (!isBusy)
        {
            UpdateFlyingMovement();
            FacePlayer();

            if (cooldownTimer >= attackCooldown)
            {
                ChooseAttackByDistance();
            }
        }
    }

    public void BeginPhase()
    {
        phaseActive = true;
        isBusy = true;
        isTakingOff = true;
        cooldownTimer = 0f;

        takeOffTargetPosition = transform.position + Vector3.up * takeOffHeight;
        boss.Anim.SetTrigger("TakeOff");
    }

    public void FinishTakeOff()
    {
        isTakingOff = false;
        transform.position = takeOffTargetPosition;

        isBusy = false;
        cooldownTimer = attackCooldown;

        StartChaseState();
    }

    public void StopPhase()
    {
        phaseActive = false;
        isBusy = false;
        isTakingOff = false;
        isChasingPlayer = false;
        isLingering = false;
        movementStateTimer = 0f;

        if (glideAttack != null)
            glideAttack.StopAttack();

        if (airSpitAttack != null)
            airSpitAttack.StopAttack();

        if (airBreathAttack != null)
            airBreathAttack.StopAttack();

        if (boss != null && boss.Anim != null)
            boss.Anim.SetBool("IsGliding", false);
    }

    public bool IsBusy()
    {
        return isBusy;
    }

    public void NotifyAttackFinished()
    {
        isBusy = false;
        cooldownTimer = 0f;
    }

    private void UpdateFlyingMovement()
    {
        movementStateTimer -= Time.deltaTime;

        if (isLingering)
        {
            if (movementStateTimer <= 0f)
            {
                StartChaseState();
            }

            return;
        }

        if (isChasingPlayer)
        {
            MoveTowardPlayer();

            if (movementStateTimer <= 0f)
            {
                StartLingerState();
            }
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

    private void MoveTowardPlayer()
    {
        Vector3 targetPosition = player.position;
        targetPosition.y = transform.position.y;

        Vector3 toPlayer = targetPosition - transform.position;
        float distance = toPlayer.magnitude;

        if (distance <= chaseStopDistance)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            chaseMoveSpeed * Time.deltaTime
        );
    }

    private void FacePlayer()
    {
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = targetRotation * Quaternion.Euler(facingRotationOffset);
        }
    }

    private void ChooseAttackByDistance()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        isBusy = true;
        cooldownTimer = 0f;

        // 0 = Glide
        // 1 = Air Spit Fire
        // 2 = Air Fire Breath

        int attackIndex;

        if (distanceToPlayer <= 10f)
        {
            // Equal chance
            attackIndex = Random.Range(0, 3);
        }
        else if (distanceToPlayer <= 15f)
        {
            // Glide favored
            attackIndex = GetWeightedAttackIndex(35, 25, 40);
        }
        else if (distanceToPlayer <= 20f)
        {
            // Fire breath favored
            attackIndex = GetWeightedAttackIndex(40, 50, 10);
        }
        else
        {
            // Spit fire favored
            attackIndex = GetWeightedAttackIndex(40, 50, 10);
        }

        switch (attackIndex)
        {
            case 0:
                glideAttack.Execute();
                break;

            case 1:
                airSpitAttack.Execute();
                break;

            case 2:
                airBreathAttack.Execute();
                break;
        }
    }

    private int GetWeightedAttackIndex(int glideWeight, int spitWeight, int breathWeight)
    {
        int total = glideWeight + spitWeight + breathWeight;
        int roll = Random.Range(0, total);

        if (roll < glideWeight)
            return 0;

        roll -= glideWeight;

        if (roll < spitWeight)
            return 1;

        return 2;
    }

    public DragonBoss GetBoss() => boss;
    public Transform GetPlayer() => player;

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        // 🟢 Chase Stop Distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chaseStopDistance);

        // 🔵 Close Range (<= 10)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 10f);

        // 🟡 Mid Range (<= 15)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 15f);

        // 🔴 Far Range (<= 20)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 20f);

        // 🟣 Line to player (for debugging)
        if (player != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}