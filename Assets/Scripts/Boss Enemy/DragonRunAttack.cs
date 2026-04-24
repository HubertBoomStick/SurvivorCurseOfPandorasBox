using System.Collections;
using UnityEngine;

public class DragonRunAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoxCollider runHitbox;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject leftRunIndicator;
    [SerializeField] private GameObject rightRunIndicator;

    [Header("Run Points")]
    [SerializeField] private Transform leftRunStartPoint;
    [SerializeField] private Transform rightRunStartPoint;
    [SerializeField] private Transform leftRunEndPoint;
    [SerializeField] private Transform rightRunEndPoint;

    [Header("Hide / Reappear")]
    [SerializeField] private float hiddenTimeBeforeIndicator = 0.4f;
    [SerializeField] private Renderer[] renderersToHide;
    [SerializeField] private Collider[] collidersToHide;

    [Header("Exit Before Attack")]
    [SerializeField] private float exitRunTime = 1f;
    [SerializeField] private float exitRunSpeed = 12f;

    [Header("Side Run Attack")]
    [SerializeField] private float runSpeed = 18f;
    [SerializeField] private float warningTime = 1.2f;
    [SerializeField] private int runDamage = 1;

    [Header("Animation")]
    [SerializeField] private float retriggerInterval = 0.6f;
    [SerializeField] private Vector3 facingRotationOffset = new Vector3(0f, 180f, 0f);

    public bool IsRunning { get; private set; }

    private DragonPhaseTwo phase;
    private Coroutine runningRoutine;
    private bool alreadyHit;
    private bool runDamageActive;

    public void SetPhase(DragonPhaseTwo phaseTwo)
    {
        phase = phaseTwo;
    }

    public void Execute()
    {
        StopAttack();
        runningRoutine = StartCoroutine(RunRoutine());
    }

    public void StopAttack()
    {
        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }

        IsRunning = false;
        runDamageActive = false;
        alreadyHit = false;

        HideIndicators();
        SetDragonVisible(true);
    }

    private IEnumerator RunRoutine()
    {
        if (phase == null || phase.GetBoss() == null)
            yield break;

        DragonBoss boss = phase.GetBoss();
        Transform player = phase.GetPlayer();

        if (boss == null || player == null)
            yield break;

        if (leftRunStartPoint == null || rightRunStartPoint == null || leftRunEndPoint == null || rightRunEndPoint == null)
        {
            Debug.LogWarning("Run attack points are missing.");
            phase.NotifyAttackFinished();
            yield break;
        }

        IsRunning = true;
        alreadyHit = false;
        runDamageActive = false;
        SetDragonVisible(true);

        // Run off screen first
        float exitDirection = transform.position.x < player.position.x ? -1f : 1f;
        FaceDirection(exitDirection);

        boss.Anim.ResetTrigger("RunAttack");
        boss.Anim.SetTrigger("RunAttack");

        float exitTimer = 0f;
        float exitRetriggerTimer = 0f;

        while (exitTimer < exitRunTime)
        {
            exitTimer += Time.deltaTime;
            exitRetriggerTimer += Time.deltaTime;

            transform.position += new Vector3(exitDirection * exitRunSpeed * Time.deltaTime, 0f, 0f);

            if (exitRetriggerTimer >= retriggerInterval)
            {
                exitRetriggerTimer = 0f;
                boss.Anim.ResetTrigger("RunAttack");
                boss.Anim.SetTrigger("RunAttack");
            }

            yield return null;
        }

        // Hide completely
        SetDragonVisible(false);
        yield return new WaitForSeconds(hiddenTimeBeforeIndicator);

        // Choose side
        bool startFromLeft = Random.value > 0.5f;

        Transform startPoint = startFromLeft ? leftRunStartPoint : rightRunStartPoint;
        Transform endPoint = startFromLeft ? leftRunEndPoint : rightRunEndPoint;

        transform.position = startPoint.position;

        float attackDirection = startFromLeft ? 1f : -1f;
        FaceDirection(attackDirection);

        HideIndicators();

        if (startFromLeft)
        {
            if (leftRunIndicator != null)
                leftRunIndicator.SetActive(true);
        }
        else
        {
            if (rightRunIndicator != null)
                rightRunIndicator.SetActive(true);
        }

        yield return new WaitForSeconds(warningTime);

        HideIndicators();

        // Reappear and charge
        SetDragonVisible(true);

        boss.Anim.ResetTrigger("RunAttack");
        boss.Anim.SetTrigger("RunAttack");

        runDamageActive = true;
        alreadyHit = false;

        float retriggerTimer = 0f;

        float endX = endPoint.position.x;

        while (startFromLeft ? transform.position.x < endX : transform.position.x > endX)
        {
            retriggerTimer += Time.deltaTime;

            Vector3 pos = transform.position;
            pos.x += attackDirection * runSpeed * Time.deltaTime;
            transform.position = pos;

            if (runDamageActive && !alreadyHit)
                CheckRunDamage();

            if (retriggerTimer >= retriggerInterval)
            {
                retriggerTimer = 0f;
                boss.Anim.ResetTrigger("RunAttack");
                boss.Anim.SetTrigger("RunAttack");
            }

            yield return null;
        }

        Vector3 finalPos = transform.position;
        finalPos.x = endX;
        transform.position = finalPos;

        transform.position = endPoint.position;

        runDamageActive = false;
        IsRunning = false;
        runningRoutine = null;

        boss.Anim.CrossFade("Ground Idle PhaseTwo", 0.15f);
        phase.NotifyAttackFinished();
    }

    private void SetDragonVisible(bool visible)
    {
        if (renderersToHide != null)
        {
            for (int i = 0; i < renderersToHide.Length; i++)
            {
                if (renderersToHide[i] != null)
                    renderersToHide[i].enabled = visible;
            }
        }

        if (collidersToHide != null)
        {
            for (int i = 0; i < collidersToHide.Length; i++)
            {
                if (collidersToHide[i] != null)
                    collidersToHide[i].enabled = visible;
            }
        }
    }

    private void FaceDirection(float direction)
    {
        Vector3 lookDir = direction > 0f ? Vector3.right : Vector3.left;
        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
        transform.rotation = targetRotation * Quaternion.Euler(facingRotationOffset);
    }

    private void CheckRunDamage()
    {
        if (runHitbox == null)
            return;

        Vector3 worldCenter = runHitbox.transform.TransformPoint(runHitbox.center);
        Vector3 halfExtents = Vector3.Scale(runHitbox.size * 0.5f, runHitbox.transform.lossyScale);
        Quaternion rotation = runHitbox.transform.rotation;

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
                damageable.takeDamage(runDamage);
                alreadyHit = true;
                break;
            }
        }
    }

    private void HideIndicators()
    {
        if (leftRunIndicator != null)
            leftRunIndicator.SetActive(false);

        if (rightRunIndicator != null)
            rightRunIndicator.SetActive(false);
    }
}