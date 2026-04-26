using UnityEngine;

public class WaterfallSafeZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerWaterfallSafety safety = other.GetComponentInParent<PlayerWaterfallSafety>();

        if (safety == null)
            return;

        safety.EnterSafeZone();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerWaterfallSafety safety = other.GetComponentInParent<PlayerWaterfallSafety>();

        if (safety == null)
            return;

        safety.ExitSafeZone();
    }
}