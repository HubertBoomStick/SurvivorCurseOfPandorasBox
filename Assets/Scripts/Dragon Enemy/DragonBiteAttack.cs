using UnityEngine;

public class DragonBiteAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoxCollider biteHitbox;
    [SerializeField] private LayerMask playerLayer;

    [Header("Settings")]
    [SerializeField] private int biteDamage = 1;

    private DragonPhaseTwo phase;
    private bool canDealDamage;
    private bool alreadyHit;

    public void SetPhase(DragonPhaseTwo phaseTwo)
    {
        phase = phaseTwo;
    }

    public void Execute()
    {
        if (phase == null || phase.GetBoss() == null)
            return;

        canDealDamage = false;
        alreadyHit = false;

        phase.GetBoss().Anim.ResetTrigger("Bite");
        phase.GetBoss().Anim.SetTrigger("Bite");
    }

    public void StopAttack()
    {
        canDealDamage = false;
        alreadyHit = false;
    }

    // animation event
    public void StartBiteDamage()
    {
        canDealDamage = true;
        alreadyHit = false;
        CheckBiteDamage();
    }

    // animation event
    public void StopBiteDamage()
    {
        canDealDamage = false;
    }

    // animation event on last frame
    public void FinishBiteAttack()
    {
        canDealDamage = false;

        if (phase != null)
            phase.NotifyAttackFinished();
    }

    private void CheckBiteDamage()
    {
        if (!canDealDamage || alreadyHit || biteHitbox == null)
            return;

        Vector3 worldCenter = biteHitbox.transform.TransformPoint(biteHitbox.center);
        Vector3 halfExtents = Vector3.Scale(biteHitbox.size * 0.5f, biteHitbox.transform.lossyScale);
        Quaternion rotation = biteHitbox.transform.rotation;

        Collider[] hits = Physics.OverlapBox(
            worldCenter,
            halfExtents,
            rotation,
            playerLayer,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hits.Length; i++)
        {
            IDamage damageable = hits[i].GetComponent<IDamage>();

            if (damageable == null)
                damageable = hits[i].GetComponentInParent<IDamage>();

            if (damageable == null)
                damageable = hits[i].GetComponentInChildren<IDamage>();

            if (damageable != null)
            {
                damageable.takeDamage(biteDamage);
                alreadyHit = true;
                break;
            }
        }
    }
}