using System.Collections;
using UnityEngine;

public class DragonPhaseTwo : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Phase 2 Settings")]
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private Vector3 facingRotationOffset = new Vector3(0f, 180f, 0f);

    private DragonBoss boss;
    private bool phaseActive;
    private bool isBusy;
    private float cooldownTimer = Mathf.Infinity;

    public void SetBoss(DragonBoss dragonBoss)
    {
        boss = dragonBoss;
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

        if (!isBusy)
        {
            FacePlayer();

            if (cooldownTimer >= attackCooldown)
            {
                ChooseGroundAttack();
            }
        }
    }

    public void BeginPhase()
    {
        phaseActive = true;
        isBusy = false;
        cooldownTimer = attackCooldown;
    }

    public void StopPhase()
    {
        phaseActive = false;
        isBusy = false;
        StopAllCoroutines();
    }

    public bool IsBusy()
    {
        return isBusy;
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

    private void ChooseGroundAttack()
    {
        isBusy = true;
        cooldownTimer = 0f;

        int attackIndex = Random.Range(0, 5);

        switch (attackIndex)
        {
            case 0:
                boss.Anim.SetTrigger("Bite");
                break;
            case 1:
                boss.Anim.SetTrigger("ClawAttack");
                break;
            case 2:
                boss.Anim.SetTrigger("RunAttack");
                break;
            case 3:
                boss.Anim.SetTrigger("SpitFire");
                break;
            case 4:
                boss.Anim.SetTrigger("FireBreath");
                break;
        }

        StartCoroutine(FinishGroundAttackAfterDelay());
    }

    private IEnumerator FinishGroundAttackAfterDelay()
    {
        yield return new WaitForSeconds(attackCooldown);
        isBusy = false;
    }
}