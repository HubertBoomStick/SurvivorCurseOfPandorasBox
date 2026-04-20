using UnityEngine;

public class DragonBreath : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 18;
    [SerializeField] private float tickRate = 0.25f;
    [SerializeField] private LayerMask hitLayers;

    private float tickTimer;
    private BoxCollider breathCollider;

    public void SetUp(int newDamage, float lifeTime, LayerMask newHitLayers)
    {
        damage = newDamage;
        hitLayers = newHitLayers;

        Destroy(gameObject, lifeTime);
    }

    private void Awake()
    {
        breathCollider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        tickTimer += Time.deltaTime;

        if (tickTimer >= tickRate)
        {
            tickTimer = 0f;
            DealBreathDamage();
        }
    }

    private void DealBreathDamage()
    {
        if (breathCollider == null)
            return;

        Vector3 worldCenter = breathCollider.transform.TransformPoint(breathCollider.center);
        Vector3 halfExtents = Vector3.Scale(breathCollider.size * 0.5f, breathCollider.transform.lossyScale);
        Quaternion rotation = breathCollider.transform.rotation;

        Collider[] hits = Physics.OverlapBox(
            worldCenter,
            halfExtents,
            rotation,
            hitLayers
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

    private void OnDrawGizmosSelected()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        if (box == null)
            return;

        Gizmos.color = Color.magenta;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = box.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = oldMatrix;
    }
}