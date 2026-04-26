using System.Collections.Generic;
using UnityEngine;

public class KrakenSweepHitbox : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 2;
    [SerializeField] private string playerTag = "Player";

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float upwardForce = 3f;

    private Collider hitbox;
    private readonly HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    private void Awake()
    {
        hitbox = GetComponent<Collider>();

        if (hitbox != null)
        {
            hitbox.isTrigger = true;
            hitbox.enabled = false;
        }
    }

    public void StartSweepHitbox()
    {
        alreadyHit.Clear();

        if (hitbox != null)
            hitbox.enabled = true;
    }

    public void StopSweepHitbox()
    {
        if (hitbox != null)
            hitbox.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        GameObject playerObject = other.gameObject;

        if (alreadyHit.Contains(playerObject))
            return;

        alreadyHit.Add(playerObject);

        IDamage damageable = other.GetComponent<IDamage>();
        if (damageable == null)
            damageable = other.GetComponentInParent<IDamage>();

        if (damageable != null)
            damageable.takeDamage(damage);

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null)
            rb = other.GetComponentInParent<Rigidbody>();

        if (rb != null)
        {
            Vector3 knockbackDirection = (other.transform.position - transform.position).normalized;
            knockbackDirection.y = 0f;

            rb.AddForce(
                knockbackDirection * knockbackForce + Vector3.up * upwardForce,
                ForceMode.Impulse
            );
        }
    }
}