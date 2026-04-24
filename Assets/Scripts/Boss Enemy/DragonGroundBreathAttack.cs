using UnityEngine;

public class DragonGroundBreathAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private DragonBreath breathPrefab;
    [SerializeField] private LayerMask breathHitLayers;

    [Header("Settings")]
    [SerializeField] private int breathDamage = 1;
    [SerializeField] private float followMoveSpeed = 3f;
    [SerializeField] private float followTurnSpeed = 3f;

    private DragonPhaseTwo phase;
    private bool breathActive;
    private DragonBreath currentBreathInstance;
    private Transform currentBreathAnchor;

    public void SetPhase(DragonPhaseTwo phaseTwo)
    {
        phase = phaseTwo;
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

        phase.GetBoss().Anim.ResetTrigger("GroundFireBreath");
        phase.GetBoss().Anim.SetTrigger("GroundFireBreath");
    }

    public void StopAttack()
    {
        StopGroundFireBreath();
    }

    public void StartGroundFireBreath()
    {
        if (phase == null)
            return;

        Transform player = phase.GetPlayer();

        if (breathPrefab == null || firePoint == null || player == null)
            return;

        StopGroundFireBreath();

        GameObject anchorObject = new GameObject("GroundFireBreathAnchor");
        currentBreathAnchor = anchorObject.transform;
        currentBreathAnchor.position = firePoint.position;
        currentBreathAnchor.rotation = firePoint.rotation;

        currentBreathInstance = Instantiate(breathPrefab, currentBreathAnchor);
        currentBreathInstance.SetUp(breathDamage, breathHitLayers);

        breathActive = true;
    }

    public void FinishGroundFireBreathAttack()
    {
        StopGroundFireBreath();

        if (phase != null)
            phase.NotifyAttackFinished();
    }

    public void StopGroundFireBreath()
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