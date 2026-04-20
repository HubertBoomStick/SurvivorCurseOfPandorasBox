using System.Collections;
using UnityEngine;

public class DragonBoss : MonoBehaviour, IDamage
{
    [Header("Health")]
    [SerializeField] private int maxHP = 200;
    [SerializeField] private int currentHP;

    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask playerLayer;

    [Header("Boss Settings")]
    [SerializeField] private float detectionRange = 25f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Phase Settings")]
    [SerializeField] private int phaseTwoThresholdPercent = 50;

    [Header("Facing Offset")]
    [SerializeField] private Vector3 facingRotationOffset = new Vector3(0f, 180f, 0f);

    [Header("Flight Movement")]
    [SerializeField] private float takeOffHeight = 6f;
    [SerializeField] private float takeOffSpeed = 4f;

    [Header("Glide Attack")]
    [SerializeField] private float glideOffscreenHeight = 8f;
    [SerializeField] private float glideWarningTime = 1.2f;
    [SerializeField] private float glideSideDistance = 25f;
    [SerializeField] private float glideSpeed = 12f;
    [SerializeField] private float glideAttackHeightOffset = 1.5f;
    [SerializeField] private float glideReturnHeight = 6f;
    [SerializeField] private bool alternateGlideSides = true;

    [Header("Glide Indicators")]
    [SerializeField] private GameObject leftGlideIndicator;
    [SerializeField] private GameObject rightGlideIndicator;

    [Header("Fireball")]
    [SerializeField] private DragonFireball fireballPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireballSpeed = 12f;
    [SerializeField] private LayerMask fireballHitLayers;

    /*
    [Header("Air Fire Breath")]
    [SerializeField] private DragonBreath airFireBreathPrefab;
    [SerializeField] private float airFireBreathLifetime = 2.5f;
    [SerializeField] private LayerMask airFireBreathHitLayers;
    */

    [Header("Flying Attack Damages")]
    [SerializeField] private int glideDamage = 20;
    [SerializeField] private int airSpitFireDamage = 15;
    // [SerializeField] private int airFireBreathDamage = 18;

    [Header("Ground Attack Damages")]
    [SerializeField] private int biteDamage = 20;
    [SerializeField] private int clawAttackDamage = 18;
    [SerializeField] private int runAttackDamage = 25;
    [SerializeField] private int spitFireDamage = 15;
    // [SerializeField] private int fireBreathDamage = 20;

    [Header("Flying Hitboxes")]
    [SerializeField] private BoxCollider glideHitbox;

    [Header("Ground Hitboxes")]
    [SerializeField] private BoxCollider biteHitbox;
    [SerializeField] private BoxCollider clawHitbox;
    [SerializeField] private BoxCollider runHitbox;
    [SerializeField] private BoxCollider spitFireHitbox;
    // [SerializeField] private BoxCollider fireBreathHitbox;

    private float cooldownTimer = Mathf.Infinity;

    private bool isAttacking;
    private bool isDead;
    private bool canAttack;
    private bool phaseTwoStarted;
    private bool hasStartedFight;

    private bool isTakingOff;
    private Vector3 takeOffTargetPosition;
    private float flyingHeightY;

    private bool glideDamageActive;
    private bool glideAlreadyHit;
    private bool lockFacingDuringAttack;
    private Quaternion lockedAttackRotation;
    private bool nextGlideFromLeft = true;

    /*
    private DragonBreath currentAirBreathInstance;
    */

    private void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        currentHP = maxHP;
        flyingHeightY = transform.position.y + takeOffHeight;
        HideGlideIndicators();
    }

    private void Update()
    {
        if (isDead)
            return;

        if (player == null)
            return;

        if (isTakingOff)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                takeOffTargetPosition,
                takeOffSpeed * Time.deltaTime
            );
        }

        cooldownTimer += Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!hasStartedFight && distanceToPlayer <= detectionRange)
        {
            hasStartedFight = true;
            StartFlyingPhase();
            return;
        }

        if (!phaseTwoStarted && currentHP <= maxHP * (phaseTwoThresholdPercent / 100f))
        {
            StartPhaseTwo();
            return;
        }

        if (glideDamageActive && !glideAlreadyHit)
        {
            CheckGlideDamage();
        }

        if (!canAttack)
            return;

        if (distanceToPlayer > detectionRange)
            return;

        FacePlayer();

        if (cooldownTimer >= attackCooldown && !isAttacking)
        {
            ChooseAttack();
        }
    }

    private void StartFlyingPhase()
    {
        canAttack = false;
        isAttacking = true;
        isTakingOff = true;

        takeOffTargetPosition = transform.position + Vector3.up * takeOffHeight;
        flyingHeightY = takeOffTargetPosition.y;

        anim.SetTrigger("TakeOff");
    }

    public void FinishTakeOff()
    {
        isTakingOff = false;
        transform.position = takeOffTargetPosition;

        isAttacking = false;
        canAttack = true;
    }

    private void StartPhaseTwo()
    {
        StopAllCoroutines();

        phaseTwoStarted = true;
        canAttack = false;
        isAttacking = true;
        isTakingOff = false;
        glideDamageActive = false;
        lockFacingDuringAttack = false;

        /*
        StopAirFireBreath();
        */

        anim.SetBool("IsGliding", false);
        cooldownTimer = 0f;

        HideGlideIndicators();
        anim.SetTrigger("PhaseTwo");
    }

    public void FinishPhaseTwo()
    {
        isAttacking = false;
        canAttack = true;
    }

    private void FacePlayer()
    {
        if (lockFacingDuringAttack)
        {
            transform.rotation = lockedAttackRotation;
            return;
        }

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
        isAttacking = true;
        cooldownTimer = 0f;

        if (!phaseTwoStarted)
            ChooseFlyingAttack();
        else
            ChooseGroundAttack();
    }

    private void ChooseFlyingAttack()
    {
        int flyingAttack = Random.Range(0, 2);

        switch (flyingAttack)
        {
            case 0:
                StartCoroutine(DoGlideAttack());
                break;
            case 1:
                anim.SetTrigger("AirSpitFire");
                break;
        }
    }

    private void ChooseGroundAttack()
    {
        int groundAttack = Random.Range(0, 4);

        switch (groundAttack)
        {
            case 0:
                anim.SetTrigger("Bite");
                break;
            case 1:
                anim.SetTrigger("ClawAttack");
                break;
            case 2:
                anim.SetTrigger("RunAttack");
                break;
            case 3:
                anim.SetTrigger("SpitFire");
                break;
                // case 4:
                //     anim.SetTrigger("FireBreath");
                //     break;
        }
    }

    private IEnumerator DoGlideAttack()
    {
        canAttack = false;
        isAttacking = true;
        glideDamageActive = false;
        glideAlreadyHit = false;

        bool fromLeft = nextGlideFromLeft;

        if (alternateGlideSides)
            nextGlideFromLeft = !nextGlideFromLeft;

        Vector3 prepPosition = transform.position;
        prepPosition.y = flyingHeightY + glideOffscreenHeight;

        HideGlideIndicators();
        yield return MoveBossTo(prepPosition, takeOffSpeed);

        ShowGlideIndicator(fromLeft);
        yield return new WaitForSeconds(glideWarningTime);
        HideGlideIndicators();

        float attackY = player.position.y + glideAttackHeightOffset;
        float startX = player.position.x + (fromLeft ? -glideSideDistance : glideSideDistance);
        float endX = player.position.x + (fromLeft ? glideSideDistance : -glideSideDistance);

        Vector3 startPosition = new Vector3(startX, attackY, transform.position.z);
        Vector3 endPosition = new Vector3(endX, attackY, transform.position.z);

        transform.position = startPosition;

        Vector3 travelDirection = (endPosition - startPosition).normalized;
        lockedAttackRotation = Quaternion.LookRotation(travelDirection) * Quaternion.Euler(facingRotationOffset);
        transform.rotation = lockedAttackRotation;
        lockFacingDuringAttack = true;

        anim.SetBool("IsGliding", true);

        glideDamageActive = true;
        glideAlreadyHit = false;

        yield return MoveBossTo(endPosition, glideSpeed);

        glideDamageActive = false;

        Vector3 returnPosition = endPosition;
        returnPosition.y = flyingHeightY;

        yield return MoveBossTo(returnPosition, takeOffSpeed);

        anim.SetBool("IsGliding", false);

        lockFacingDuringAttack = false;
        isAttacking = false;
        canAttack = true;
    }

    private IEnumerator MoveBossTo(Vector3 targetPosition, float moveSpeed)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;
    }

    private void ShowGlideIndicator(bool fromLeft)
    {
        if (leftGlideIndicator != null)
            leftGlideIndicator.SetActive(fromLeft);

        if (rightGlideIndicator != null)
            rightGlideIndicator.SetActive(!fromLeft);
    }

    private void HideGlideIndicators()
    {
        if (leftGlideIndicator != null)
            leftGlideIndicator.SetActive(false);

        if (rightGlideIndicator != null)
            rightGlideIndicator.SetActive(false);
    }

    // ---------------- PROJECTILE ATTACKS ----------------

    public void SpawnAirSpitFire()
    {
        if (fireballPrefab == null || firePoint == null || player == null)
            return;

        Vector3 dir = (player.position - firePoint.position).normalized;

        DragonFireball newFireball = Instantiate(
            fireballPrefab,
            firePoint.position,
            Quaternion.LookRotation(dir)
        );

        newFireball.SetUp(dir, airSpitFireDamage, fireballSpeed, fireballHitLayers);
    }

    public void SpawnGroundSpitFire()
    {
        if (fireballPrefab == null || firePoint == null || player == null)
            return;

        Vector3 dir = (player.position - firePoint.position).normalized;

        DragonFireball newFireball = Instantiate(
            fireballPrefab,
            firePoint.position,
            Quaternion.LookRotation(dir)
        );

        newFireball.SetUp(dir, spitFireDamage, fireballSpeed, fireballHitLayers);
    }

    // ---------------- AIR FIRE BREATH ----------------

    /*
    public void StartAirFireBreath()
    {
        if (airFireBreathPrefab == null || firePoint == null)
            return;

        StopAirFireBreath();

        currentAirBreathInstance = Instantiate(airFireBreathPrefab, firePoint);
        currentAirBreathInstance.transform.localPosition = Vector3.zero;
        currentAirBreathInstance.transform.localRotation = Quaternion.identity;
        currentAirBreathInstance.transform.localScale = Vector3.one;

        currentAirBreathInstance.SetUp(
            airFireBreathDamage,
            airFireBreathLifetime,
            airFireBreathHitLayers
        );
    }

    public void StopAirFireBreath()
    {
        if (currentAirBreathInstance != null)
        {
            Destroy(currentAirBreathInstance.gameObject);
            currentAirBreathInstance = null;
        }
    }
    */

    public void EndAttack()
    {
        if (isDead)
            return;

        /*
        StopAirFireBreath();
        */

        isAttacking = false;
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
        }
    }

    private void Die()
    {
        StopAllCoroutines();

        isDead = true;
        canAttack = false;
        isAttacking = false;
        isTakingOff = false;
        glideDamageActive = false;
        lockFacingDuringAttack = false;

        /*
        StopAirFireBreath();
        */

        anim.SetBool("IsGliding", false);

        HideGlideIndicators();
        anim.SetTrigger("Die");
    }

    // ---------------- HITBOX DAMAGE ----------------

    private void CheckGlideDamage()
    {
        DealDamageFromHitbox(glideHitbox, glideDamage, true);
    }

    public void DealBiteDamage()
    {
        DealDamageFromHitbox(biteHitbox, biteDamage);
    }

    public void DealClawAttackDamage()
    {
        DealDamageFromHitbox(clawHitbox, clawAttackDamage);
    }

    public void DealRunAttackDamage()
    {
        DealDamageFromHitbox(runHitbox, runAttackDamage);
    }

    /*
    public void DealFireBreathDamage()
    {
        DealDamageFromHitbox(fireBreathHitbox, fireBreathDamage);
    }
    */

    private void DealDamageFromHitbox(BoxCollider hitbox, int damage, bool glideCheck = false)
    {
        if (hitbox == null)
            return;

        Vector3 worldCenter = hitbox.transform.TransformPoint(hitbox.center);
        Vector3 halfExtents = Vector3.Scale(hitbox.size * 0.5f, hitbox.transform.lossyScale);
        Quaternion rotation = hitbox.transform.rotation;

        Collider[] hits = Physics.OverlapBox(
            worldCenter,
            halfExtents,
            rotation,
            playerLayer
        );

        for (int i = 0; i < hits.Length; i++)
        {
            IDamage damageable = hits[i].GetComponent<IDamage>();

            if (damageable != null)
            {
                damageable.takeDamage(damage);

                if (glideCheck)
                    glideAlreadyHit = true;
            }
        }
    }

    // ---------------- GIZMOS ----------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        DrawColliderHitbox(glideHitbox, Color.cyan);

        DrawColliderHitbox(biteHitbox, Color.red);
        DrawColliderHitbox(clawHitbox, Color.green);
        DrawColliderHitbox(runHitbox, Color.yellow);
        DrawColliderHitbox(spitFireHitbox, new Color(1f, 0.5f, 0f));
        // DrawColliderHitbox(fireBreathHitbox, Color.white);
    }

    private void DrawColliderHitbox(BoxCollider hitbox, Color color)
    {
        if (hitbox == null)
            return;

        Gizmos.color = color;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = hitbox.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(hitbox.center, hitbox.size);
        Gizmos.matrix = oldMatrix;
    }
}