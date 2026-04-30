using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private KrakenBoss krakenBoss;
    [SerializeField] private DragonBoss dragonBoss;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (krakenBoss != null)
            krakenBoss.StartBossFight();

        if (dragonBoss != null)
            dragonBoss.StartBossFight();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (krakenBoss != null)
            krakenBoss.ResetBossFight();

        if (dragonBoss != null)
            dragonBoss.ResetBossFight();
    }
}