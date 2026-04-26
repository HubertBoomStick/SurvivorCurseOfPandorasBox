using System.Collections.Generic;
using UnityEngine;

public class KrakenWaterfallAttack : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float lifetime = 6f;

    [Header("Damage Over Time")]
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float tickRate = 0.5f;

    [Header("Protection Settings")]
    [SerializeField] private LayerMask safeRockLayer;

    private readonly HashSet<PlayerMovement> playersInside = new HashSet<PlayerMovement>();
    private float tickTimer;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        tickTimer -= Time.deltaTime;

        if (tickTimer > 0f)
            return;

        tickTimer = tickRate;

        foreach (PlayerMovement player in playersInside)
        {
            if (player == null)
                continue;

            // NEW LOGIC: Check straight up for rock cover
            if (IsProtectedByRock(player.transform))
            {
                Debug.Log("Player is SAFE under rock.");
                continue;
            }

            Debug.Log("Player is taking damage.");
            player.takeDamage(damagePerTick);
        }
    }

    private bool IsProtectedByRock(Transform player)
    {
        Vector3 start = player.position + Vector3.up * 0.5f;

        // Visual debug ray (green = checking upward)
        Debug.DrawRay(start, Vector3.up * 20f, Color.green, 0.2f);

        return Physics.Raycast(
            start,
            Vector3.up,
            20f,
            safeRockLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

        if (player != null)
            playersInside.Add(player);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

        if (player != null)
            playersInside.Remove(player);
    }

    public void SetLifetime(float newLifetime)
    {
        lifetime = newLifetime;
        Destroy(gameObject, lifetime);
    }
}