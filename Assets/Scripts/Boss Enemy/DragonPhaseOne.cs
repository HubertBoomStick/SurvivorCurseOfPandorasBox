using UnityEngine;

public class DragonPhaseOne : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private DragonGlideAttack glideAttack;
    [SerializeField] private DragonAirSpitAttack airSpitAttack;
    [SerializeField] private DragonAirBreathAttack airBreathAttack;

    [Header("Phase 1 Settings")]
    [SerializeField] private float takeOffHeight = 6f;
    [SerializeField] private float takeOffSpeed = 4f;
    [SerializeField] private float attackCooldown = 5f;
    [SerializeField] private Vector3 facingRotationOffset = new Vector3(0f, 180f, 0f);

    private DragonBoss boss;
    private bool phaseActive;
    private bool isBusy;
    private bool isTakingOff;
    private Vector3 takeOffTargetPosition;
    private float cooldownTimer = Mathf.Infinity;

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
        }

        if (!isBusy)
        {
            FacePlayer();

            if (cooldownTimer >= attackCooldown)
            {
                ChooseAttack();
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
    }

    public void StopPhase()
    {
        phaseActive = false;
        isBusy = false;
        isTakingOff = false;

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

    private void ChooseAttack()
    {
        isBusy = true;
        cooldownTimer = 0f;

        int attackIndex = Random.Range(0, 3);

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

    public DragonBoss GetBoss() => boss;
    public Transform GetPlayer() => player;
}