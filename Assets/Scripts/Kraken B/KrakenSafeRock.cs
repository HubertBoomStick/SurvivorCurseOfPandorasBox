using System.Collections;
using UnityEngine;

public class KrakenSafeRock : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float safeDelay = 2f;
    [SerializeField] private float flashDuration = 2f;
    [SerializeField] private float totalLifetime = 5f;

    [Header("Flashing")]
    [SerializeField] private Renderer rockRenderer;
    [SerializeField] private float flashSpeed = 0.15f;
    [SerializeField] private Color flashColor = Color.red;

    private Material rockMaterial;
    private Color originalColor;

    private bool playerInside;
    private bool routineStarted;
    private bool isDestroying;

    private void Awake()
    {
        if (rockRenderer == null)
            rockRenderer = GetComponentInChildren<Renderer>();

        if (rockRenderer != null)
        {
            rockMaterial = new Material(rockRenderer.material);
            rockRenderer.material = rockMaterial;
            originalColor = rockMaterial.color;
        }
    }

    public void PlayerEnteredSafeZone()
    {
        playerInside = true;

        if (!routineStarted)
            StartCoroutine(RockRoutine());
    }

    public void PlayerExitedSafeZone()
    {
        playerInside = false;
    }

    private IEnumerator RockRoutine()
    {
        routineStarted = true;

        float safeTimer = 0f;

        while (safeTimer < safeDelay)
        {
            if (playerInside)
                safeTimer += Time.deltaTime;
            else
                safeTimer = 0f;

            yield return null;
        }

        float flashTimer = 0f;

        while (flashTimer < flashDuration)
        {
            flashTimer += flashSpeed * 2f;

            if (rockMaterial != null)
                rockMaterial.color = flashColor;

            yield return new WaitForSeconds(flashSpeed);

            if (rockMaterial != null)
                rockMaterial.color = originalColor;

            yield return new WaitForSeconds(flashSpeed);
        }

        float remainingTime = totalLifetime - safeDelay - flashDuration;

        if (remainingTime > 0f)
            yield return new WaitForSeconds(remainingTime);

        DestroyRock();
    }

    public void DestroyRock()
    {
        if (isDestroying)
            return;

        isDestroying = true;
        Destroy(gameObject);
    }
}