using UnityEngine;

public class KrakenSweepController : MonoBehaviour
{
    [SerializeField] private KrakenSweepHitbox sweepHitbox;

    public void StartSweepHitbox()
    {
        if (sweepHitbox != null)
            sweepHitbox.StartSweepHitbox();
    }

    public void StopSweepHitbox()
    {
        if (sweepHitbox != null)
            sweepHitbox.StopSweepHitbox();
    }
}