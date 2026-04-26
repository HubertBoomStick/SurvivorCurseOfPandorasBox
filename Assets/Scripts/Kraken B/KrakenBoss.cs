using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KrakenBoss : MonoBehaviour, IDamage
{
    [Header("Boss Health")]
    [SerializeField] private int maxHealth = 50;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float comboMoveSpeed = 2f;
    [SerializeField] private float stopDistanceX = 1.5f;
    [SerializeField] private string movingBoolName = "Moving";
    [SerializeField] private string moveDirectionFloatName = "MoveDirection";

    [Header("Attack Timing")]
    [SerializeField] private float startDelay = 2f;
    [SerializeField] private float cooldownBetweenAttacks = 3f;

    [Header("Dive Settings")]
    [SerializeField] private float diveDistance = 8f;
    [SerializeField] private float diveTime = 1f;
    [SerializeField] private float emergeTime = 1f;

    [Header("Tentacle Indicator")]
    [SerializeField] private GameObject tentacleIndicatorPrefab;
    [SerializeField] private float tentacleWarningTime = 1.5f;
    [SerializeField] private float tentacleIndicatorYOffset = 1.5f;

    [Header("Tentacle Attack")]
    [SerializeField] private GameObject tentaclePillarPrefab;
    [SerializeField] private Transform eruptionStartPoint;
    [SerializeField] private Transform eruptionEndPoint;
    [SerializeField] private int eruptionCount = 10;
    [SerializeField] private float eruptionDelayBetweenSpawns = 0.1f;
    [SerializeField] private float eruptionEndDelay = 1.5f;

    [Header("Tentacle Targeting")]
    [SerializeField] private bool canTargetPlayer = true;
    [SerializeField] private float chanceToTargetPlayer = 0.5f;
    [SerializeField] private float playerTargetXOffset = 0f;

    [Header("Waterfall Attack")]
    [SerializeField] private GameObject waterfallPrefab;
    [SerializeField] private float waterfallWarningTime = 2f;
    [SerializeField] private float waterfallDuration = 6f;
    [SerializeField] private float waterfallYPosition = 1f;
    [SerializeField] private float rockSpawnDelayBeforeWaterfall = 0.5f;

    [Header("UI Waterfall Warning")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform waterfallUIIcon;
    [SerializeField] private Camera uiCamera;

    [Header("Safe Rocks")]
    [SerializeField] private GameObject safeRockPrefab;
    [SerializeField] private GameObject safeRockIndicatorPrefab;
    [SerializeField] private int safeRockCount = 4;
    [SerializeField] private float safeRockYPosition = 8.5f;
    [SerializeField] private float minimumSafeRockSpacing = 6f;

    [Header("3 Combo Sweep Attack")]
    [SerializeField] private string sweepTriggerName = "Sweep";
    [SerializeField] private float sweepAttackDuration = 4.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private int currentHealth;
    private bool isDead;
    private bool isInvincible;
    private bool isBusy;

    private float normalY;
    private float normalZ;

    private void Start()
    {
        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        if (uiCamera == null)
            uiCamera = Camera.main;

        if (waterfallUIIcon != null)
            waterfallUIIcon.gameObject.SetActive(false);

        normalY = transform.position.y;
        normalZ = transform.position.z;

        StartCoroutine(BossLoop());
    }

    private void Update()
    {
        if (isDead)
            return;

        if (!isBusy && followPlayer)
            MoveTowardPlayer(moveSpeed);
    }

    private IEnumerator BossLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (!isDead)
        {
            yield return StartCoroutine(EruptionAttack());
            yield return new WaitForSeconds(cooldownBetweenAttacks);

            yield return StartCoroutine(WaterfallAttack());
            yield return new WaitForSeconds(cooldownBetweenAttacks);

            yield return StartCoroutine(SweepAttack());
            yield return new WaitForSeconds(cooldownBetweenAttacks);
        }
    }

    private void MoveTowardPlayer(float speed)
    {
        if (player == null)
        {
            SetMoving(false);
            return;
        }

        float targetX = Mathf.Clamp(player.position.x, GetMinX(), GetMaxX());
        float distanceX = Mathf.Abs(transform.position.x - targetX);

        if (distanceX <= stopDistanceX)
        {
            SetMoving(false);
            return;
        }

        float direction = Mathf.Sign(targetX - transform.position.x);

        Vector3 targetPosition = new Vector3(targetX, transform.position.y, normalZ);

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        SetMoving(true);
        SetMoveDirection(direction);
    }

    private void SetMoving(bool moving)
    {
        if (animator != null && !string.IsNullOrEmpty(movingBoolName))
            animator.SetBool(movingBoolName, moving);
    }

    private void SetMoveDirection(float direction)
    {
        if (animator != null && !string.IsNullOrEmpty(moveDirectionFloatName))
            animator.SetFloat(moveDirectionFloatName, direction);
    }

    private IEnumerator EruptionAttack()
    {
        if (isBusy)
            yield break;

        isBusy = true;
        isInvincible = true;
        SetMoving(false);

        if (showDebugLogs)
            Debug.Log("Kraken starting tentacle eruption attack.");

        yield return StartCoroutine(DiveDown());

        for (int i = 0; i < eruptionCount; i++)
        {
            Vector3 spawnPosition = GetTentacleSpawnPosition();

            GameObject indicator = null;

            if (tentacleIndicatorPrefab != null)
            {
                Vector3 indicatorPosition = spawnPosition + Vector3.up * tentacleIndicatorYOffset;
                indicator = Instantiate(
                    tentacleIndicatorPrefab,
                    indicatorPosition,
                    tentacleIndicatorPrefab.transform.rotation
                );
            }

            yield return new WaitForSeconds(tentacleWarningTime);

            if (indicator != null)
                Destroy(indicator);

            if (tentaclePillarPrefab != null)
                Instantiate(
                    tentaclePillarPrefab,
                    spawnPosition,
                    tentaclePillarPrefab.transform.rotation
                );

            yield return new WaitForSeconds(eruptionDelayBetweenSpawns);
        }

        yield return new WaitForSeconds(eruptionEndDelay);

        yield return StartCoroutine(EmergeUp());

        isInvincible = false;
        isBusy = false;
    }

    private IEnumerator WaterfallAttack()
    {
        if (isBusy)
            yield break;

        isBusy = true;
        SetMoving(false);

        if (showDebugLogs)
            Debug.Log("Kraken starting waterfall attack.");

        Vector3 waterfallPosition = GetWaterfallPosition();

        ShowWaterfallUIIcon(waterfallPosition);

        List<Vector3> rockPositions = GetSafeRockPositions();
        List<GameObject> indicators = new List<GameObject>();

        foreach (Vector3 rockPosition in rockPositions)
        {
            if (safeRockIndicatorPrefab != null)
            {
                GameObject indicator = Instantiate(
                    safeRockIndicatorPrefab,
                    rockPosition,
                    safeRockIndicatorPrefab.transform.rotation
                );

                indicators.Add(indicator);
            }
        }

        yield return new WaitForSeconds(waterfallWarningTime);

        HideWaterfallUIIcon();

        foreach (GameObject indicator in indicators)
        {
            if (indicator != null)
                Destroy(indicator);
        }

        foreach (Vector3 rockPosition in rockPositions)
        {
            if (safeRockPrefab != null)
            {
                Instantiate(
                    safeRockPrefab,
                    rockPosition,
                    safeRockPrefab.transform.rotation
                );
            }
        }

        if (showDebugLogs)
            Debug.Log("Safe rocks spawned BEFORE waterfall.");

        yield return new WaitForSeconds(rockSpawnDelayBeforeWaterfall);

        GameObject waterfall = null;

        if (waterfallPrefab != null)
        {
            waterfall = Instantiate(
                waterfallPrefab,
                waterfallPosition,
                waterfallPrefab.transform.rotation
            );

            KrakenWaterfallAttack waterfallAttack = waterfall.GetComponent<KrakenWaterfallAttack>();

            if (waterfallAttack != null)
                waterfallAttack.SetLifetime(waterfallDuration);
        }

        if (showDebugLogs)
            Debug.Log("Waterfall spawned AFTER safe rocks.");

        yield return new WaitForSeconds(waterfallDuration);

        if (waterfall != null)
            Destroy(waterfall);

        isBusy = false;
    }

    private IEnumerator SweepAttack()
    {
        if (isBusy)
            yield break;

        isBusy = true;

        if (showDebugLogs)
            Debug.Log("SWEEP COMBO ATTACK TRIGGERED");

        if (animator != null)
        {
            animator.ResetTrigger(sweepTriggerName);
            animator.SetTrigger(sweepTriggerName);
        }

        float timer = 0f;

        while (timer < sweepAttackDuration)
        {
            timer += Time.deltaTime;

            MoveTowardPlayer(comboMoveSpeed);

            yield return null;
        }

        SetMoving(false);
        isBusy = false;
    }

    private IEnumerator DiveDown()
    {
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.down * diveDistance;

        float timer = 0f;

        while (timer < diveTime)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, timer / diveTime);
            yield return null;
        }

        transform.position = end;
    }

    private IEnumerator EmergeUp()
    {
        Vector3 start = transform.position;
        Vector3 end = new Vector3(transform.position.x, normalY, normalZ);

        float timer = 0f;

        while (timer < emergeTime)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, timer / emergeTime);
            yield return null;
        }

        transform.position = end;
    }

    private Vector3 GetTentacleSpawnPosition()
    {
        if (eruptionStartPoint == null || eruptionEndPoint == null)
            return transform.position;

        float minX = GetMinX();
        float maxX = GetMaxX();

        bool targetPlayer =
            canTargetPlayer &&
            player != null &&
            Random.value <= chanceToTargetPlayer;

        float spawnX = targetPlayer
            ? player.position.x + playerTargetXOffset
            : Random.Range(minX, maxX);

        spawnX = Mathf.Clamp(spawnX, minX, maxX);

        return new Vector3(
            spawnX,
            eruptionStartPoint.position.y,
            eruptionStartPoint.position.z
        );
    }

    private List<Vector3> GetSafeRockPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        List<float> usedX = new List<float>();

        float minX = GetMinX();
        float maxX = GetMaxX();

        for (int i = 0; i < safeRockCount; i++)
        {
            float chosenX = Random.Range(minX, maxX);

            for (int attempt = 0; attempt < 30; attempt++)
            {
                bool tooClose = false;

                foreach (float x in usedX)
                {
                    if (Mathf.Abs(chosenX - x) < minimumSafeRockSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                    break;

                chosenX = Random.Range(minX, maxX);
            }

            usedX.Add(chosenX);

            positions.Add(new Vector3(
                chosenX,
                safeRockYPosition,
                eruptionStartPoint.position.z
            ));
        }

        return positions;
    }

    private Vector3 GetWaterfallPosition()
    {
        float middleX = (GetMinX() + GetMaxX()) * 0.5f;

        return new Vector3(
            middleX,
            waterfallYPosition,
            eruptionStartPoint.position.z
        );
    }

    private float GetMinX()
    {
        if (eruptionStartPoint == null || eruptionEndPoint == null)
            return transform.position.x - 20f;

        return Mathf.Min(eruptionStartPoint.position.x, eruptionEndPoint.position.x);
    }

    private float GetMaxX()
    {
        if (eruptionStartPoint == null || eruptionEndPoint == null)
            return transform.position.x + 20f;

        return Mathf.Max(eruptionStartPoint.position.x, eruptionEndPoint.position.x);
    }

    private void ShowWaterfallUIIcon(Vector3 worldPosition)
    {
        if (waterfallUIIcon == null || uiCanvas == null)
            return;

        if (uiCamera == null)
            uiCamera = Camera.main;

        Vector3 screenPosition = uiCamera.WorldToScreenPoint(worldPosition);

        waterfallUIIcon.gameObject.SetActive(true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiCanvas.transform as RectTransform,
            screenPosition,
            uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera,
            out Vector2 localPoint
        );

        waterfallUIIcon.anchoredPosition = localPoint;
    }

    private void HideWaterfallUIIcon()
    {
        if (waterfallUIIcon != null)
            waterfallUIIcon.gameObject.SetActive(false);
    }

    public void TakeTentacleDamage()
    {
        if (!isInvincible || isDead)
            return;

        currentHealth -= 1;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Kraken took 1 damage from destroyed tentacle. HP left: " + currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    public void takeDamage(int damage)
    {
        if (isDead || isInvincible)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (showDebugLogs)
            Debug.Log("Kraken took damage: " + damage + " | HP left: " + currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines();

        SetMoving(false);
        HideWaterfallUIIcon();

        if (animator != null)
            animator.SetTrigger("Death");
    }
}