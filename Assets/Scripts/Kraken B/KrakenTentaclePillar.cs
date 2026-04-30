using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class KrakenTentaclePillar : MonoBehaviour, IDamage
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private GameObject healthBarObject;

    [Header("Kraken Damage")]
    [SerializeField] private KrakenBoss krakenBoss;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 5f;

    [Header("Spawn Eruption")]
    [SerializeField] private float riseDistance = 8f;
    [SerializeField] private float riseTime = 0.35f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private string playerTag = "Player";

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 25f;
    [SerializeField] private float upwardForce = 5f;

    [Header("Hit Flash")]
    [SerializeField] private Renderer tentacleRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashTime = 0.25f;

    [Header("Death Sink")]
    [SerializeField] private Transform sinkRoot;
    [SerializeField] private float sinkDistance = 8f;
    [SerializeField] private float sinkTime = 0.5f;

    private int currentHealth;
    private bool isDead;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (sinkRoot == null)
            sinkRoot = transform;

        if (tentacleRenderer == null)
            tentacleRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        if (healthBarObject != null)
            healthBarObject.SetActive(true);

        StartCoroutine(SpawnRoutine());
        StartCoroutine(LifetimeRoutine());
    }

    public void SetKrakenBoss(KrakenBoss boss)
    {
        krakenBoss = boss;
    }

    private IEnumerator SpawnRoutine()
    {
        Vector3 shownPosition = sinkRoot.position;
        Vector3 hiddenPosition = shownPosition + Vector3.down * riseDistance;

        sinkRoot.position = hiddenPosition;

        float timer = 0f;

        while (timer < riseTime)
        {
            timer += Time.deltaTime;
            sinkRoot.position = Vector3.Lerp(hiddenPosition, shownPosition, timer / riseTime);
            yield return null;
        }

        sinkRoot.position = shownPosition;
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);

        if (!isDead)
            StartCoroutine(DeathRoutine(false));
    }

    public void takeDamage(int damageAmount)
    {
        if (isDead)
            return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        Debug.Log("Tentacle took damage: " + damageAmount + " | HP left: " + currentHealth);

        FlashRed();

        if (currentHealth <= 0)
            StartCoroutine(DeathRoutine(true));
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = (float)currentHealth / maxHealth;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;

        if (!other.CompareTag(playerTag))
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
            player = other.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        player.takeDamage(damage);

        Vector3 direction = (other.transform.position - transform.position).normalized;
        direction.y = 0f;

        player.ApplyKnockback(direction * knockbackForce + Vector3.up * upwardForce);
    }

    private void FlashRed()
    {
        if (tentacleRenderer == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Material[] materials = tentacleRenderer.materials;

        foreach (Material mat in materials)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", hitColor * 5f);
        }

        yield return new WaitForSeconds(flashTime);

        if (!isDead)
        {
            foreach (Material mat in materials)
                mat.SetColor("_EmissionColor", Color.black);
        }
    }

    private IEnumerator DeathRoutine(bool killedByPlayer)
    {
        if (isDead)
            yield break;

        isDead = true;

        if (killedByPlayer && krakenBoss != null)
            krakenBoss.TakeTentacleDamage();

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (healthBarObject != null)
            healthBarObject.SetActive(false);

        Vector3 start = sinkRoot.position;
        Vector3 end = start + Vector3.down * sinkDistance;

        float timer = 0f;

        while (timer < sinkTime)
        {
            timer += Time.deltaTime;
            sinkRoot.position = Vector3.Lerp(start, end, timer / sinkTime);
            yield return null;
        }

        sinkRoot.position = end;

        if (sinkRoot != null)
            Destroy(sinkRoot.gameObject);
        else
            Destroy(gameObject);
    }
}