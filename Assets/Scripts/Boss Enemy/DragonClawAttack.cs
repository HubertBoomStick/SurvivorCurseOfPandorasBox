using UnityEngine;

public class DragonClawAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoxCollider clawHitbox;
    [SerializeField] private LayerMask playerLayer;

    [Header("Settings")]
    [SerializeField] private int clawDamage = 1;

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

        phase.GetBoss().Anim.ResetTrigger("ClawAttack");
        phase.GetBoss().Anim.SetTrigger("ClawAttack");
    }

    public void StopAttack()
    {
        canDealDamage = false;
        alreadyHit = false;
    }

    // animation event
    public void StartClawDamage()
    {
        canDealDamage = true;
        alreadyHit = false;
        CheckClawDamage();
    }

    // animation event
    public void StopClawDamage()
    {
        canDealDamage = false;
    }

    // animation event on last frame
    public void FinishClawAttack()
    {
        canDealDamage = false;

        if (phase != null)
            phase.NotifyAttackFinished();
    }

    private void CheckClawDamage()
    {
        if (!canDealDamage || alreadyHit || clawHitbox == null)
            return;

        Vector3 worldCenter = clawHitbox.transform.TransformPoint(clawHitbox.center);
        Vector3 halfExtents = Vector3.Scale(clawHitbox.size * 0.5f, clawHitbox.transform.lossyScale);
        Quaternion rotation = clawHitbox.transform.rotation;

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
                damageable.takeDamage(clawDamage);
                alreadyHit = true;
                break;
            }
        }
    }
}