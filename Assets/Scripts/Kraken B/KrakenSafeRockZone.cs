using UnityEngine;

public class KrakenSafeRockZone : MonoBehaviour
{
    [SerializeField] private KrakenSafeRock safeRock;

    private void Awake()
    {
        if (safeRock == null)
            safeRock = GetComponentInParent<KrakenSafeRock>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (safeRock != null)
            safeRock.PlayerEnteredSafeZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (safeRock != null)
            safeRock.PlayerExitedSafeZone();
    }
}