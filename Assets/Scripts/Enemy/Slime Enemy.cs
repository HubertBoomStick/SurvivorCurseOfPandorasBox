using UnityEngine;

public class SlimeEnemy : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int damage = 2;

    [Header("References")]
    [SerializeField] private BoxCollider boxCollider;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Animator anim;

    [Header("Detection")]
    [SerializeField] private Vector3 boxSizeMultiplier = Vector3.one;
    [SerializeField] private Vector3 boxOffset = Vector3.zero;

    private float cooldownTimer = Mathf.Infinity;
    private bool isAttacking;

    private void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();
    }

    private void Update()
    {
        cooldownTimer += Time.deltaTime;

        if (PlayerInSight() && cooldownTimer >= attackCooldown && !isAttacking)
        {
            Attack();
        }
    }

    private void Attack()
    {
        isAttacking = true;
        cooldownTimer = 2f;

        anim.ResetTrigger("attack");
        anim.SetTrigger("attack");
    }
    public void DealDamage()
    {
        if (boxCollider == null) return;

        Vector3 scaledSize = Vector3.Scale(boxCollider.size, boxSizeMultiplier);

        Vector3 worldCenter =
            transform.TransformPoint(boxCollider.center + boxOffset) +
            transform.forward * (scaledSize.z * 0.5f);

        Collider[] hits = Physics.OverlapBox(
            worldCenter,
            scaledSize * 0.5f,
            transform.rotation,
            playerLayer
        );

        for (int i = 0; i < hits.Length; i++)
        {
            IDamage damageable = hits[i].GetComponent<IDamage>();

            if (damageable != null)
            {
                damageable.takeDamage(damage);
            }
        }
    }
    public void EndAttack()
    {
        isAttacking = false;
    }

    private bool PlayerInSight()
    {
        if (boxCollider == null) return false;

        Vector3 scaledSize = Vector3.Scale(boxCollider.size, boxSizeMultiplier);

        Vector3 worldCenter =
            transform.TransformPoint(boxCollider.center + boxOffset) +
            transform.forward * (scaledSize.z * 0.5f);

        Collider[] hits = Physics.OverlapBox(
            worldCenter,
            scaledSize * 0.5f,
            transform.rotation,
            playerLayer
        );

        return hits.Length > 0;
    }

    private void OnDrawGizmos()
    {
        if (boxCollider == null) return;

        Gizmos.color = Color.red;

        Vector3 scaledSize = Vector3.Scale(boxCollider.size, boxSizeMultiplier);

        Vector3 worldCenter =
            transform.TransformPoint(boxCollider.center + boxOffset) +
            transform.forward * (scaledSize.z * 0.5f);

        Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, scaledSize);
    }
}