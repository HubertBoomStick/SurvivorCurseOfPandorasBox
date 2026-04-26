using UnityEngine;

public class DragonAirBreathAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private DragonBreath breathPrefab;
    [SerializeField] private LayerMask breathHitLayers;

    [Header("Settings")]
    [SerializeField] private int breathDamage = 1;
    [SerializeField] private float followMoveSpeed = 3f;
    [SerializeField] private float followTurnSpeed = 3f;

    private DragonPhaseOne phase;
    private bool breathActive;
    private DragonBreath currentBreathInstance;
    private Transform currentBreathAnchor;

    public void SetPhase(DragonPhaseOne phaseOne)
    {
        phase = phaseOne;
    }

    private void Update()
    {
        if (!breathActive || currentBreathAnchor == null || phase == null)
            return;

        Transform player = phase.GetPlayer();

        if (firePoint == null || player == null)
            return;

        currentBreathAnchor.position = Vector3.MoveTowards(
            currentBreathAnchor.position,
            firePoint.position,
            followMoveSpeed * Time.deltaTime
        );

        Vector3 dir = player.position - currentBreathAnchor.position;

        if (dir.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);

        currentBreathAnchor.rotation = Quaternion.Slerp(
            currentBreathAnchor.rotation,
            targetRotation,
            followTurnSpeed * Time.deltaTime
        );
    }

    public void Execute()
    {
        if (phase == null || phase.GetBoss() == null)
            return;

        Animator anim = phase.GetBoss().Anim;

        if (anim == null)
            return;

        anim.ResetTrigger("AirFireBreath");
        anim.SetTrigger("AirFireBreath");
    }

    public void StopAttack()
    {
        StopAirFireBreath();
    }

    public void StartAirFireBreath()
    {
        if (phase == null)
            return;

        Transform player = phase.GetPlayer();

        if (breathPrefab == null || firePoint == null || player == null)
            return;

        StopAirFireBreath();

        GameObject anchorObject = new GameObject("AirFireBreathAnchor");
        currentBreathAnchor = anchorObject.transform;
        currentBreathAnchor.position = firePoint.position;
        currentBreathAnchor.rotation = firePoint.rotation;

        currentBreathInstance = Instantiate(breathPrefab, currentBreathAnchor);
        currentBreathInstance.SetUp(breathDamage, breathHitLayers);

        breathActive = true;
    }

    public void FinishAirFireBreathAttack()
    {
        StopAirFireBreath();

        if (phase != null)
            phase.NotifyAttackFinished();
    }

    public void StopAirFireBreath()
    {
        breathActive = false;

        if (currentBreathInstance != null)
        {
            Destroy(currentBreathInstance.gameObject);
            currentBreathInstance = null;
        }

        if (currentBreathAnchor != null)
        {
            Destroy(currentBreathAnchor.gameObject);
            currentBreathAnchor = null;
        }
    }
}