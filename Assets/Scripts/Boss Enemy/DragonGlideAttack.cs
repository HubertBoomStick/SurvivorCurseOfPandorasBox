using System.Collections;
using UnityEngine;

public class DragonGlideAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoxCollider glideHitbox;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject leftGlideIndicator;
    [SerializeField] private GameObject rightGlideIndicator;

    [Header("Settings")]
    [SerializeField] private float offscreenHeight = 8f;
    [SerializeField] private float warningTime = 1.2f;
    [SerializeField] private float sideDistance = 25f;
    [SerializeField] private float glideSpeed = 40f;
    [SerializeField] private float attackHeightOffset = 0.5f;
    [SerializeField] private float returnHeight = 6f;
    [SerializeField] private bool alternateSides = true;
    [SerializeField] private int glideDamage = 1;
    [SerializeField] private float prepMoveSpeed = 4f;
    [SerializeField] private Vector3 facingRotationOffset = new Vector3(0f, 180f, 0f);

    private DragonPhaseOne phase;
    private Coroutine runningRoutine;
    private bool glideDamageActive;
    private bool glideAlreadyHit;
    private bool nextFromLeft = true;
    private float flyingHeightY;

    public void SetPhase(DragonPhaseOne phaseOne)
    {
        phase = phaseOne;
    }

    public void Execute()
    {
        StopAttack();
        runningRoutine = StartCoroutine(GlideRoutine());
    }

    public void StopAttack()
    {
        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }

        glideDamageActive = false;
        glideAlreadyHit = false;
        HideIndicators();

        DragonBoss boss = phase != null ? phase.GetBoss() : null;
        if (boss != null && boss.Anim != null)
            boss.Anim.SetBool("IsGliding", false);
    }

    private IEnumerator GlideRoutine()
    {
        DragonBoss boss = phase.GetBoss();
        Transform player = phase.GetPlayer();

        if (boss == null || player == null)
            yield break;

        flyingHeightY = transform.position.y;

        bool fromLeft = nextFromLeft;

        if (alternateSides)
            nextFromLeft = !nextFromLeft;

        Vector3 prepPosition = transform.position;
        prepPosition.y = flyingHeightY + offscreenHeight;

        HideIndicators();
        yield return MoveTo(prepPosition, prepMoveSpeed);

        ShowIndicator(fromLeft);
        yield return new WaitForSeconds(warningTime);
        HideIndicators();

        float attackY = player.position.y + attackHeightOffset;
        float startX = player.position.x + (fromLeft ? -sideDistance : sideDistance);
        float endX = player.position.x + (fromLeft ? sideDistance : -sideDistance);

        Vector3 startPosition = new Vector3(startX, attackY, transform.position.z);
        Vector3 endPosition = new Vector3(endX, attackY, transform.position.z);

        transform.position = startPosition;

        Vector3 travelDirection = (endPosition - startPosition).normalized;
        Quaternion lockedRotation = Quaternion.LookRotation(travelDirection) * Quaternion.Euler(facingRotationOffset);
        transform.rotation = lockedRotation;

        boss.Anim.SetBool("IsGliding", true);

        glideDamageActive = true;
        glideAlreadyHit = false;

        while (Vector3.Distance(transform.position, endPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                endPosition,
                glideSpeed * Time.deltaTime
            );

            if (glideDamageActive && !glideAlreadyHit)
                CheckGlideDamage();

            yield return null;
        }

        transform.position = endPosition;
        glideDamageActive = false;

        Vector3 returnPosition = endPosition;
        returnPosition.y = returnHeight;

        yield return MoveTo(returnPosition, prepMoveSpeed);

        boss.Anim.SetBool("IsGliding", false);

        runningRoutine = null;
        phase.NotifyAttackFinished();
    }

    private IEnumerator MoveTo(Vector3 targetPosition, float moveSpeed)
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

    private void CheckGlideDamage()
    {
        if (glideHitbox == null)
            return;

        Vector3 worldCenter = glideHitbox.transform.TransformPoint(glideHitbox.center);
        Vector3 halfExtents = Vector3.Scale(glideHitbox.size * 0.5f, glideHitbox.transform.lossyScale);
        Quaternion rotation = glideHitbox.transform.rotation;

        Collider[] hits = Physics.OverlapBox(
            worldCenter,
            halfExtents,
            rotation,
            playerLayer,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hits.Length; i++)
        {
            IDamage damageable = hits[i].GetComponent<IDamage>();

            if (damageable == null)
                damageable = hits[i].GetComponentInParent<IDamage>();

            if (damageable == null)
                damageable = hits[i].GetComponentInChildren<IDamage>();

            if (damageable != null)
            {
                damageable.takeDamage(glideDamage);
                glideAlreadyHit = true;
                break;
            }
        }
    }

    private void ShowIndicator(bool fromLeft)
    {
        if (leftGlideIndicator != null)
            leftGlideIndicator.SetActive(fromLeft);

        if (rightGlideIndicator != null)
            rightGlideIndicator.SetActive(!fromLeft);
    }

    private void HideIndicators()
    {
        if (leftGlideIndicator != null)
            leftGlideIndicator.SetActive(false);

        if (rightGlideIndicator != null)
            rightGlideIndicator.SetActive(false);
    }
}