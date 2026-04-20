using UnityEngine;

public class DragonFireball : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private int damage = 15;
    [SerializeField] private LayerMask hitLayers;

    private Vector3 moveDirection;

    public void SetUp(Vector3 direction, int newDamage, float newSpeed, LayerMask newHitLayers)
    {
        moveDirection = direction.normalized;
        damage = newDamage;
        speed = newSpeed;
        hitLayers = newHitLayers;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & hitLayers) == 0)
            return;

        IDamage damageable = other.GetComponent<IDamage>();

        if (damageable != null)
        {
            damageable.takeDamage(damage);
        }

        Destroy(gameObject);
    }
}