using UnityEngine;

public class DragonBoss : MonoBehaviour, IDamage
{
    [Header("Health")]
    [SerializeField] private int maxHP = 50;
    [SerializeField] private int currentHP;

    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private Transform player;

    [Header("Phase References")]
    [SerializeField] private DragonPhaseOne phaseOne;
    [SerializeField] private DragonPhaseTwo phaseTwo;

    [Header("General Settings")]
    [SerializeField] private float detectionRange = 25f;
    [SerializeField] private int phaseTwoThresholdPercent = 50;
    [SerializeField] private float hitReactionCooldown = 0.3f;

    private bool isDead;
    private bool hasStartedFight;
    private bool phaseTwoStarted;
    private float hitReactionTimer = Mathf.Infinity;

    public Animator Anim => anim;
    public Transform Player => player;
    public bool IsDead => isDead;
    public bool IsPhaseTwo => phaseTwoStarted;
    public float DetectionRange => detectionRange;

    private void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (phaseOne == null)
            phaseOne = GetComponent<DragonPhaseOne>();

        if (phaseTwo == null)
            phaseTwo = GetComponent<DragonPhaseTwo>();

        currentHP = maxHP;

        if (phaseOne != null)
            phaseOne.SetBoss(this);

        if (phaseTwo != null)
        {
            phaseTwo.SetBoss(this);
            phaseTwo.enabled = false;
        }
    }

    private void Update()
    {
        if (isDead || player == null)
            return;

        hitReactionTimer += Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!hasStartedFight && distanceToPlayer <= detectionRange)
        {
            hasStartedFight = true;

            if (phaseOne != null)
                phaseOne.BeginPhase();
        }

        if (!phaseTwoStarted && currentHP <= maxHP * (phaseTwoThresholdPercent / 100f))
        {
            StartPhaseTwo();
        }
    }

    private void StartPhaseTwo()
    {
        if (phaseTwoStarted || isDead)
            return;

        phaseTwoStarted = true;

        if (phaseOne != null)
        {
            phaseOne.StopPhase();
            phaseOne.enabled = false;
        }

        anim.SetTrigger("PhaseTwo");
    }

    public void FinishPhaseTwo()
    {
        if (isDead)
            return;

        if (phaseTwo != null)
        {
            phaseTwo.enabled = true;
            phaseTwo.BeginPhase();
        }
    }

    public void takeDamage(int amount)
    {
        if (isDead)
            return;

        currentHP -= amount;

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
            return;
        }

        if (!phaseTwoStarted)
        {
            if (phaseOne != null && !phaseOne.IsBusy() && hitReactionTimer >= hitReactionCooldown)
            {
                anim.SetTrigger("FlyHit");
                hitReactionTimer = 0f;
            }
        }
        else
        {
            if (phaseTwo != null && !phaseTwo.IsBusy() && hitReactionTimer >= hitReactionCooldown)
            {
                anim.SetTrigger("GroundHit");
                hitReactionTimer = 0f;
            }
        }
    }

    private void Die()
    {
        isDead = true;

        if (phaseOne != null)
            phaseOne.StopPhase();

        if (phaseTwo != null)
            phaseTwo.StopPhase();

        anim.SetTrigger("Die");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}