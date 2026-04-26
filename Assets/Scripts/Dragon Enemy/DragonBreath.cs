using System.Collections.Generic;
using UnityEngine;

public class DragonBreath : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float tickRate = 0.75f;
    [SerializeField] private LayerMask hitLayers;

    private readonly Dictionary<Collider, float> damageTimers = new Dictionary<Collider, float>();

    public void SetUp(int newDamage, LayerMask newHitLayers)
    {
        damage = newDamage;
        hitLayers = newHitLayers;
        damageTimers.Clear();
    }

    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & hitLayers) == 0)
            return;

        if (!damageTimers.ContainsKey(other))
        {
            damageTimers[other] = 0f;
        }

        damageTimers[other] += Time.deltaTime;

        if (damageTimers[other] >= tickRate)
        {
            damageTimers[other] = 0f;

            IDamage damageable = other.GetComponent<IDamage>();
            if (damageable != null)
            {
                damageable.takeDamage(damage);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (damageTimers.ContainsKey(other))
        {
            damageTimers.Remove(other);
        }
    }

    private void OnDisable()
    {
        damageTimers.Clear();
    }
}