using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KrakenBoss : MonoBehaviour, IDamage
{
    [Header("Boss Health")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject healthBarObject;
    [SerializeField] private bool hideHealthBarAtStart = true;

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
    [SerializeField] private float battleStartDelay = 1.5f;
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
    [SerializeField] private Transform waterfallSpawnPoint;
    [SerializeField] private float waterfallWarningTime = 2f;
    [SerializeField] private float waterfallDuration = 6f;
    [SerializeField] private float rockSpawnDelayBeforeWaterfall = 0.5f;

    [Header("UI Waterfall Warning")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform waterfallUIIcon;
    [SerializeField] private Camera uiCamera;

    [Header("Safe Rocks")]
    [SerializeField] private GameObject safeRockPrefab;
    [SerializeField] private GameObject safeRockIndicatorPrefab;
    [SerializeField] private Transform safeRockStartPoint;
    [SerializeField] private Transform safeRockEndPoint;
    [SerializeField] private int safeRockCount = 4;
    [SerializeField] private float minimumSafeRockSpacing = 6f;

    [Header("3 Combo Sweep Attack")]
    [SerializeField] private string sweepTriggerName = "Sweep";
    [SerializeField] private float sweepAttackDuration = 4.5f;

    [Header("Death")]
    [SerializeField] private string deathTriggerName = "Death";
    [SerializeField] private float destroyAfterDeathDelay = 4f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private int currentHealth;

    private bool isDead;
    private bool hasBeenDefeated;
    private bool isInvincible;
    private bool isBusy;
    private bool battleStarted;
    private bool bossLoopStarted;

    private float normalY;
    private float normalZ;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private readonly List<GameObject> spawnedFightObjects = new List<GameObject>();

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        currentHealth = maxHealth;
        UpdateHealthBar();

        if (healthBarObject != null)
            healthBarObject.SetActive(!hideHealthBarAtStart);

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

        SetMoving(false);
    }

    private void Update()
    {
        if (isDead || hasBeenDefeated)
            return;

        if (!battleStarted)
            return;

        if (!isBusy && followPlayer)
            MoveTowardPlayer(moveSpeed);
    }

    public void StartBossFight()
    {
        if (battleStarted || isDead || hasBeenDefeated)
            return;

        StartCoroutine(StartBattleRoutine());
    }

    public void ResetBossFight()
    {
        if (hasBeenDefeated)
            return;

        ResetBoss();
    }

    private IEnumerator StartBattleRoutine()
    {
        if (battleStarted)
            yield break;

        battleStarted = true;

        if (healthBarObject != null)
            healthBarObject.SetActive(true);

        if (showDebugLogs)
            Debug.Log("KRAKEN BATTLE STARTED");

        SetMoving(false);

        yield return new WaitForSeconds(battleStartDelay);

        if (!bossLoopStarted && !isDead && !hasBeenDefeated)
        {
            bossLoopStarted = true;
            StartCoroutine(BossLoop());
        }
    }

    private IEnumerator BossLoop()
    {
        while (!isDead && !hasBeenDefeated)
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

                spawnedFightObjects.Add(indicator);
            }

            yield return new WaitForSeconds(tentacleWarningTime);

            if (indicator != null)
                Destroy(indicator);

            if (tentaclePillarPrefab != null)
            {
                GameObject tentacle = Instantiate(
                    tentaclePillarPrefab,
                    spawnPosition,
                    tentaclePillarPrefab.transform.rotation
                );

                spawnedFightObjects.Add(tentacle);

                KrakenTentaclePillar tentacleScript = tentacle.GetComponentInChildren<KrakenTentaclePillar>();

                if (tentacleScript != null)
                    tentacleScript.SetKrakenBoss(this);
            }

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
                spawnedFightObjects.Add(indicator);
            }
        }

        yield return new WaitForSeconds(waterfallWarningTime);

        HideWaterfallUIIcon();

        foreach (GameObject indicator in indicators)
        {
            if (indicator != null)
                Destroy(indicator);
        }

        List<GameObject> spawnedRocks = new List<GameObject>();

        foreach (Vector3 rockPosition in rockPositions)
        {
            if (safeRockPrefab != null)
            {
                GameObject rock = Instantiate(
                    safeRockPrefab,
                    rockPosition,
                    safeRockPrefab.transform.rotation
                );

                spawnedRocks.Add(rock);
                spawnedFightObjects.Add(rock);
            }
        }

        yield return new WaitForSeconds(rockSpawnDelayBeforeWaterfall);

        GameObject waterfall = null;

        if (waterfallPrefab != null)
        {
            waterfall = Instantiate(
                waterfallPrefab,
                waterfallPosition,
                waterfallPrefab.transform.rotation
            );

            spawnedFightObjects.Add(waterfall);

            KrakenWaterfallAttack waterfallAttack = waterfall.GetComponent<KrakenWaterfallAttack>();

            if (waterfallAttack != null)
                waterfallAttack.SetLifetime(waterfallDuration);
        }

        yield return new WaitForSeconds(waterfallDuration);

        if (waterfall != null)
            Destroy(waterfall);

        foreach (GameObject rock in spawnedRocks)
        {
            if (rock != null)
                Destroy(rock);
        }

        isBusy = false;
    }

    private IEnumerator SweepAttack()
    {
        if (isBusy)
            yield break;

        isBusy = true;

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

        if (safeRockStartPoint == null || safeRockEndPoint == null)
            return positions;

        float minX = Mathf.Min(safeRockStartPoint.position.x, safeRockEndPoint.position.x);
        float maxX = Mathf.Max(safeRockStartPoint.position.x, safeRockEndPoint.position.x);

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
                safeRockStartPoint.position.y,
                safeRockStartPoint.position.z
            ));
        }

        return positions;
    }

    private Vector3 GetWaterfallPosition()
    {
        if (waterfallSpawnPoint != null)
            return waterfallSpawnPoint.position;

        if (eruptionStartPoint == null || eruptionEndPoint == null)
            return transform.position;

        float middleX = (GetMinX() + GetMaxX()) * 0.5f;

        return new Vector3(
            middleX,
            transform.position.y,
            transform.position.z
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

        waterfallUIIcon.gameObject.SetActive(true);
        waterfallUIIcon.anchoredPosition = Vector2.zero;
    }

    private void HideWaterfallUIIcon()
    {
        if (waterfallUIIcon != null)
            waterfallUIIcon.gameObject.SetActive(false);
    }

    public void TakeTentacleDamage()
    {
        if (!isInvincible || isDead || hasBeenDefeated)
            return;

        TakeKrakenDamage(2);
        Debug.Log("Kraken took 2 damage from destroyed tentacle.");
    }

    public void takeDamage(int damage)
    {
        if (isDead || isInvincible || hasBeenDefeated)
            return;

        TakeKrakenDamage(damage);
    }

    private void TakeKrakenDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        if (healthBarObject != null)
            healthBarObject.SetActive(true);

        Debug.Log("Kraken HP left: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
    }

    private void ResetBoss()
    {
        if (hasBeenDefeated)
            return;

        StopAllCoroutines();

        foreach (GameObject obj in spawnedFightObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedFightObjects.Clear();

        currentHealth = maxHealth;
        UpdateHealthBar();

        if (healthBarObject != null)
            healthBarObject.SetActive(false);

        isDead = false;
        isInvincible = false;
        isBusy = false;
        battleStarted = false;
        bossLoopStarted = false;

        transform.position = startPosition;
        transform.rotation = startRotation;

        normalY = startPosition.y;
        normalZ = startPosition.z;

        SetMoving(false);
        HideWaterfallUIIcon();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        Debug.Log("Kraken reset.");
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        hasBeenDefeated = true;

        StopAllCoroutines();

        foreach (GameObject obj in spawnedFightObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedFightObjects.Clear();

        SetMoving(false);
        HideWaterfallUIIcon();

        if (healthBarObject != null)
            healthBarObject.SetActive(false);

        if (animator != null)
            animator.SetTrigger(deathTriggerName);

        Destroy(gameObject, destroyAfterDeathDelay);
    }
}