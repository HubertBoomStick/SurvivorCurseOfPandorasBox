using System.Collections;
using UnityEngine;

public class DragonGroundSpitAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private DragonFireball fireballPrefab;
    [SerializeField] private LayerMask fireballHitLayers;

    [Header("Settings")]
    [SerializeField] private int minShots = 5;
    [SerializeField] private int maxShots = 8;
    [SerializeField] private float shotDelay = 0.5f;
    [SerializeField] private float fireballSpeed = 18f;
    [SerializeField] private int fireballDamage = 1;

    private DragonPhaseTwo phase;
    private Coroutine runningRoutine;
    private bool canSpawnShot;
    private bool shotFired;

    public void SetPhase(DragonPhaseTwo phaseTwo)
    {
        phase = phaseTwo;
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
        int totalShots = Random.Range(minShots, maxShots + 1);

        for (int i = 0; i < totalShots; i++)
        {
            canSpawnShot = true;
            shotFired = false;

            anim.ResetTrigger("GroundSpitFire");
            anim.SetTrigger("GroundSpitFire");

            float timer = 0f;

            while (!shotFired && timer < 1.2f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(shotDelay);
        }

        canSpawnShot = false;
        shotFired = false;
        runningRoutine = null;

        phase.NotifyAttackFinished();
    }

    public void SpawnGroundSpitFire()
    {
        if (!canSpawnShot || phase == null)
            return;

        Transform player = phase.GetPlayer();

        if (firePoint == null || fireballPrefab == null || player == null)
            return;

        Vector3 direction = player.position - firePoint.position;
        direction.y += 0.5f;

        if (direction.sqrMagnitude <= 0.001f)
            direction = firePoint.forward;

        direction.Normalize();

        DragonFireball fireball = Instantiate(
            fireballPrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        fireball.SetUp(direction, fireballDamage, fireballSpeed, fireballHitLayers);

        shotFired = true;
        canSpawnShot = false;
    }
}