using System.Collections;
using UnityEngine;

public class SlimeEnemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyRoam3D roamScript;
    [SerializeField] private EnemyFacing3D facingScript;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject attackHitbox;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDuration = 0.8f;
    [SerializeField] private int damage = 2;

    private Transform player;
    private float attackTimer;
    private bool isAttacking;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;

        if (attackHitbox != null)
            attackHitbox.SetActive(false);
    }

    private void Update()
    {
        if (player == null)
            return;

        attackTimer += Time.deltaTime;

        float distanceToPlayer = Mathf.Abs(transform.position.x - player.position.x);

        if (distanceToPlayer <= attackRange)
        {
            if (roamScript != null)
                roamScript.StopRoaming();

            if (facingScript != null)
                facingScript.FaceTarget(player, transform);

            if (animator != null)
                animator.SetBool("moving", false);

            if (!isAttacking && attackTimer >= attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            if (roamScript != null)
                roamScript.StartRoaming();

            if (facingScript != null && roamScript != null)
            {
                if (roamScript.MovingLeft)
                    facingScript.FaceLeft();
                else
                    facingScript.FaceRight();
            }

            if (animator != null)
                animator.SetBool("moving", true);

            if (attackHitbox != null && attackHitbox.activeSelf)
                attackHitbox.SetActive(false);
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        attackTimer = 0f;

        if (animator != null)
            animator.SetTrigger("attack");

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
    }

    public void EnableAttackHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.SetActive(true);
    }

    public void DisableAttackHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.SetActive(false);
    }

    public void DealDamage()
    {
        if (player == null)
            return;

        float distanceToPlayer = Mathf.Abs(transform.position.x - player.position.x);

        if (distanceToPlayer <= attackRange)
        {
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

            if (playerMovement != null)
                playerMovement.takeDamage(damage);
        }
    }
}