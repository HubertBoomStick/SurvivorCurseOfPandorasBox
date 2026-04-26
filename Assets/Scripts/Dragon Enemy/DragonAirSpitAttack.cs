using System.Collections;
using UnityEngine;

public class DragonAirSpitAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private DragonFireball fireballPrefab;
    [SerializeField] private LayerMask fireballHitLayers;

    [Header("Settings")]
    [SerializeField] private int minShots = 8;
    [SerializeField] private int maxShots = 10;
    [SerializeField] private float shotDelay = 0.5f;
    [SerializeField] private float fireballSpeed = 18f;
    [SerializeField] private int fireballDamage = 1;

    private DragonPhaseOne phase;
    private Coroutine runningRoutine;
    private bool canSpawnShot;
    private bool shotFired;

    public void SetPhase(DragonPhaseOne phaseOne)
    {
        phase = phaseOne;
    }

    public void Execute()
    {
        StopAttack();
        runningRoutine = StartCoroutine(AttackRoutine());
    }

    public void StopAttack()
    {
        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }

        canSpawnShot = false;
        shotFired = false;
    }

    private IEnumerator AttackRoutine()
    {
        if (phase == null || phase.GetBoss() == null)
            yield break;

        Animator anim = phase.GetBoss().Anim;
        Transform player = phase.GetPlayer();

        if (anim == null || player == null)
            yield break;

        int totalShots = Random.Range(minShots, maxShots + 1);

        for (int i = 0; i < totalShots; i++)
        {
            if (phase.GetBoss().IsDead || player == null)
                break;

            shotFired = false;
            canSpawnShot = true;

            anim.ResetTrigger("AirSpitFire");
            anim.SetTrigger("AirSpitFire");

            float waitTimer = 0f;
            float maxWaitForShot = 1.2f;

            while (!shotFired && waitTimer < maxWaitForShot)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(shotDelay);
        }

        canSpawnShot = false;
        runningRoutine = null;
        phase.NotifyAttackFinished();
    }

    public void SpawnAirSpitFire()
    {
        if (!canSpawnShot || phase == null)
            return;

        Transform player = phase.GetPlayer();

        if (fireballPrefab == null || firePoint == null || player == null)
            return;

        Vector3 dir = (player.position - firePoint.position).normalized;

        DragonFireball fireball = Instantiate(
            fireballPrefab,
            firePoint.position,
            Quaternion.LookRotation(dir)
        );

        fireball.SetUp(dir, fireballDamage, fireballSpeed, fireballHitLayers);

        shotFired = true;
        canSpawnShot = false;
    }
}