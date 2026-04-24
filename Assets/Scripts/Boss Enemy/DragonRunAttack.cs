using System.Collections;
using UnityEngine;

public class DragonRunAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoxCollider runHitbox;
    [SerializeField] private LayerMask playerLayer;

    [Header("Settings")]
    [SerializeField] private float runSpeed = 14f;
    [SerializeField] private float runDuration = 3f;
    [SerializeField] private float startupDelay = 0.2f;
    [SerializeField] private int runDamage = 1;
    [SerializeField] private Vector3 facingRotationOffset = new Vector3(0f, 180f, 0f);

    private DragonPhaseTwo phase;
    private Coroutine runningRoutine;
    private bool runDamageActive;
    private bool alreadyHit;

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

        runDamageActive = false;
        alreadyHit = false;
    }

    private IEnumerator RunRoutine()
    {
        if (phase == null || phase.GetBoss() == null)
            yield break;

        DragonBoss boss = phase.GetBoss();
        Transform player = phase.GetPlayer();

        if (player == null)
            yield break;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
            transform.rotation = targetRotation * Quaternion.Euler(facingRotationOffset);
        }

        runDamageActive = true;
        alreadyHit = false;

        float timer = 0f;
        float retriggerTimer = 0f;
        float retriggerInterval = 0.6f; // match your run animation length

        boss.Anim.ResetTrigger("RunAttack");
        boss.Anim.SetTrigger("RunAttack");

        yield return new WaitForSeconds(startupDelay);

        while (timer < runDuration)
        {
            timer += Time.deltaTime;
            retriggerTimer += Time.deltaTime;

            transform.position -= transform.forward * runSpeed * Time.deltaTime;

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

        runDamageActive = false;
        runningRoutine = null;

        boss.Anim.Play("Ground Idle PhaseTwo");
        phase.NotifyAttackFinished();
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
}