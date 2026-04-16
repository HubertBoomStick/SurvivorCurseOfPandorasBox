using UnityEngine;

public class EnemyTouchDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 1f;
    [SerializeField] private float bounceForceY = 2f;
    [SerializeField] private float bounceForceX = 7f;

    private float lastDamageTime;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (Time.time < lastDamageTime + damageCooldown)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null)
        {
            player.takeDamage(damage);

            Vector3 directionToPlayer = (other.transform.position - transform.position).normalized;

            float directionX;

            if (Mathf.Abs(directionToPlayer.x) < 0.2f)
            {
                directionX = transform.forward.x >= 0 ? 1f : -1f;
            }
            else
            {
                directionX = directionToPlayer.x > 0 ? 1f : -1f;
            }

            Vector3 bounce = new Vector3(directionX * bounceForceX, bounceForceY, 0f);

            player.ApplyKnockback(bounce);
        }

        lastDamageTime = Time.time;
    }
}