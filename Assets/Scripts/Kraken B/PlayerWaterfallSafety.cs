using UnityEngine;

public class PlayerWaterfallSafety : MonoBehaviour
{
    private int safeZoneCount = 0;

    public bool IsSafe => safeZoneCount > 0;

    public void EnterSafeZone()
    {
        safeZoneCount++;
        Debug.Log("Entered Safe Zone. Count: " + safeZoneCount);
    }

    public void ExitSafeZone()
    {
        safeZoneCount = Mathf.Max(0, safeZoneCount - 1);
        Debug.Log("Exited Safe Zone. Count: " + safeZoneCount);
    }
}