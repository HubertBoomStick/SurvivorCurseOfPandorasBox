using UnityEngine;

public class EnemyHitbox3D : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 1f;

    private float cooldownTimer;

    private void Update()
    {
        cooldownTimer += Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (cooldownTimer < damageCooldown) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.takeDamage(damage);
            cooldownTimer = 0f;
        }
    }
}