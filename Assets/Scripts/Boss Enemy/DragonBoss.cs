using System.Collections;
using UnityEngine;

public class DragonBoss : MonoBehaviour, IDamage
{
    [Header("Debug")]
    [SerializeField] private bool startInPhaseTwo;

    [Header("Health")]
    [SerializeField] private int maxHP = 50;
    [SerializeField] private int currentHP;

    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private Transform player;

    [Header("Damage Hitboxes")]
    [SerializeField] private CapsuleCollider airHitBox;
    [SerializeField] private CapsuleCollider groundHitBox;

    [Header("Phase References")]
    [SerializeField] private DragonPhaseOne phaseOne;
    [SerializeField] private DragonPhaseTwo phaseTwo;

    [Header("General Settings")]
    [SerializeField] private float detectionRange = 25f;
    [SerializeField] private int phaseTwoThresholdPercent = 50;
    [SerializeField] private float hitReactionCooldown = 0.3f;

    [Header("Phase 2 Entrance")]
    [SerializeField] private Transform phaseTwoLandPoint;
    [SerializeField] private float phaseTwoFlyUpHeight = 12f;
    [SerializeField] private float phaseTwoMoveSpeed = 4f;
    [SerializeField] private float phaseTwoDescendSpeed = 4f;
    [SerializeField] private float phaseTwoLandingStartHeight = 0f;
    [SerializeField] private float phaseTwoAttackDelay = 3f;
    [SerializeField] private Vector3 phaseTwoFacingRotationOffset = new Vector3(0f, 180f, 0f);
    [SerializeField] private PhasePopupUI phaseTwoPopup;
    [SerializeField] private GameObject phaseTwoShockwaveObject;

    private bool isDead;
    private bool hasStartedFight;
    private bool phaseTwoStarted;
    private bool phaseTwoEntrancePlaying;
    private bool isInvulnerable;
    private float hitReactionTimer = Mathf.Infinity;

    public Animator Anim => anim;
    public Transform Player => player;
    public bool IsDead => isDead;
    public bool IsPhaseTwo => phaseTwoStarted;
    public float DetectionRange => detectionRange;
    public bool IsInvulnerable => isInvulnerable;

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

        if (phaseTwoShockwaveObject != null)
            phaseTwoShockwaveObject.SetActive(false);

        SetDamageHitboxState(useAirHitbox: true, useGroundHitbox: false);
    }

    private void Start()
    {
        if (startInPhaseTwo)
        {
            StartPhaseTwoImmediately();
        }
    }

    private void Update()
    {
        if (isDead || player == null)
            return;

        hitReactionTimer += Time.deltaTime;

        if (startInPhaseTwo)
            return;

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

    public void StartPhaseTwoImmediately()
    {
        if (phaseTwoStarted)
            return;

        phaseTwoStarted = true;
        hasStartedFight = true;
        phaseTwoEntrancePlaying = false;
        isInvulnerable = false;

        if (phaseOne != null)
        {
            phaseOne.StopPhase();
            phaseOne.enabled = false;
        }

        if (phaseTwoLandPoint != null)
        {
            transform.position = phaseTwoLandPoint.position;
        }

        FacePlayerForPhaseTwo();

        SetDamageHitboxState(useAirHitbox: false, useGroundHitbox: true);

        if (phaseTwoShockwaveObject != null)
            phaseTwoShockwaveObject.SetActive(false);

        if (phaseTwo != null)
        {
            phaseTwo.enabled = true;
            phaseTwo.BeginPhase();
        }

        anim.Play("Ground Idle PhaseTwo");
    }

    private void StartPhaseTwo()
    {
        if (phaseTwoStarted || isDead || phaseTwoEntrancePlaying)
            return;

        phaseTwoStarted = true;
        phaseTwoEntrancePlaying = true;
        isInvulnerable = true;

        if (phaseOne != null)
        {
            phaseOne.StopPhase();
            phaseOne.enabled = false;
        }

        if (phaseTwo != null)
            phaseTwo.enabled = false;

        // disable both while phase 2 intro is playing
        SetDamageHitboxState(useAirHitbox: false, useGroundHitbox: false);

        StartCoroutine(PhaseTwoEntranceRoutine());
    }

    private IEnumerator PhaseTwoEntranceRoutine()
    {
        Vector3 flyUpTarget = transform.position;
        flyUpTarget.y += phaseTwoFlyUpHeight;

        while (Vector3.Distance(transform.position, flyUpTarget) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                flyUpTarget,
                phaseTwoMoveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = flyUpTarget;

        if (phaseTwoLandPoint != null)
        {
            Vector3 aboveLandPoint = phaseTwoLandPoint.position + Vector3.up * phaseTwoFlyUpHeight;
            transform.position = aboveLandPoint;

            FacePlayerForPhaseTwo();

            Vector3 descendTarget = phaseTwoLandPoint.position + Vector3.up * phaseTwoLandingStartHeight;

            while (Vector3.Distance(transform.position, descendTarget) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    descendTarget,
                    phaseTwoDescendSpeed * Time.deltaTime
                );

                FacePlayerForPhaseTwo();

                yield return null;
            }

            transform.position = descendTarget;
        }

        if (phaseTwoPopup != null)
            phaseTwoPopup.ShowPhasePopup("Phase 2");

        if (phaseTwoShockwaveObject != null)
            phaseTwoShockwaveObject.SetActive(true);

        anim.SetTrigger("PhaseTwo");
    }

    public void FinishPhaseTwo()
    {
        if (isDead)
            return;

        if (phaseTwoLandPoint != null)
        {
            Vector3 pos = transform.position;
            pos.y = phaseTwoLandPoint.position.y;
            transform.position = pos;
        }

        FacePlayerForPhaseTwo();
        StartCoroutine(BeginPhaseTwoAfterDelay());
    }

    private IEnumerator BeginPhaseTwoAfterDelay()
    {
        yield return new WaitForSeconds(phaseTwoAttackDelay);

        if (phaseTwoShockwaveObject != null)
            phaseTwoShockwaveObject.SetActive(false);

        isInvulnerable = false;
        phaseTwoEntrancePlaying = false;

        SetDamageHitboxState(useAirHitbox: false, useGroundHitbox: true);

        if (phaseTwo != null)
        {
            phaseTwo.enabled = true;
            phaseTwo.BeginPhase();
        }
    }

    private void FacePlayerForPhaseTwo()
    {
        if (player == null)
            return;

        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = targetRotation * Quaternion.Euler(phaseTwoFacingRotationOffset);
        }
    }

    private void SetDamageHitboxState(bool useAirHitbox, bool useGroundHitbox)
    {
        if (airHitBox != null)
            airHitBox.enabled = useAirHitbox;

        if (groundHitBox != null)
            groundHitBox.enabled = useGroundHitbox;
    }

    public void takeDamage(int amount)
    {
        if (isDead || isInvulnerable)
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
        isInvulnerable = true;
        phaseTwoEntrancePlaying = false;

        StopAllCoroutines();

        if (phaseOne != null)
            phaseOne.StopPhase();

        if (phaseTwo != null)
            phaseTwo.StopPhase();

        SetDamageHitboxState(useAirHitbox: false, useGroundHitbox: false);

        if (phaseTwoShockwaveObject != null)
            phaseTwoShockwaveObject.SetActive(false);

        anim.SetTrigger("Die");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}