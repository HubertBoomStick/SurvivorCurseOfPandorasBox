using UnityEngine;

public class DragonGroundBreathAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private DragonBreath breathPrefab;
    [SerializeField] private LayerMask breathHitLayers;

    [Header("Settings")]
    [SerializeField] private int breathDamage = 1;
    [SerializeField] private float followTurnSpeed = 3f;

    private DragonPhaseTwo phase;
    private DragonBreath currentBreathInstance;
    private Quaternion prefabLocalRotation;

    public void SetPhase(DragonPhaseTwo phaseTwo)
    {
        phase = phaseTwo;
    }

    private void Update()
    {
        if (currentBreathInstance == null || firePoint == null || phase == null)
            return;

        Transform player = phase.GetPlayer();

        currentBreathInstance.transform.position = firePoint.position;

        if (player == null)
            return;

        Vector3 dir = player.position - firePoint.position;
        dir.y += 0.5f;

        if (dir.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized) * prefabLocalRotation;

        currentBreathInstance.transform.rotation = Quaternion.Slerp(
            currentBreathInstance.transform.rotation,
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
        if (firePoint == null || breathPrefab == null)
            return;

        StopGroundFireBreath();

        currentBreathInstance = Instantiate(breathPrefab, firePoint);

        currentBreathInstance.transform.localPosition = Vector3.zero;

        prefabLocalRotation = currentBreathInstance.transform.localRotation;

        currentBreathInstance.SetUp(breathDamage, breathHitLayers);
    }

    public void FinishGroundFireBreathAttack()
    {
        StopGroundFireBreath();

        if (phase != null)
            phase.NotifyAttackFinished();
    }

    private void StopGroundFireBreath()
    {
        if (currentBreathInstance != null)
        {
            Destroy(currentBreathInstance.gameObject);
            currentBreathInstance = null;
        }
    }
}