using UnityEngine;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth { get; private set; }

    public static event Action<int, int> OnHealthChanged;

    [Header("Hit Stop Settings")]
    public float hitStopDuration = 0.5f;
    public GameObject hitEffectPrefab;

    [Header("Invincibility (I-Frames)")]
    public float iFrameDuration = 1.5f;
    private bool isInvincible = false;

    [Header("Knockback Settings")]
    public float knockbackForceX = 6f;
    public float knockbackForceY = 4f;
    public float knockbackDuration = 0.2f;

    [Header("Trap Respawn Settings")]
    public float safePositionDelay = 0.2f;
    public LayerMask noSaveLayer;
    public LayerMask trapLayer;
    private Vector2 lastSafePosition;
    private float groundedTimer;

    private Rigidbody2D rb;
    private PlayerAnimator playerAnimator;
    private PlayerController playerController;
    private SpriteRenderer sr;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerController = GetComponent<PlayerController>();
        sr = GetComponent<SpriteRenderer>();

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        if (playerController == null) return;

        bool isNearTrap = Physics2D.OverlapCircle(transform.position, 2.5f, trapLayer);
        bool inNoSaveZone = Physics2D.OverlapCircle(transform.position, 0.5f, noSaveLayer);

        if (playerController.IsGrounded() && !playerController.IsWalled() && !isInvincible && !isNearTrap && !inNoSaveZone)
        {
            groundedTimer += Time.deltaTime;

            if (groundedTimer >= safePositionDelay)
            {
                lastSafePosition = transform.position;
            }
        }
        else
        {
            groundedTimer = 0f;
        }
    }

    public void TakeDamage(int damageAmount, Transform enemyTransform)
    {
        if (isInvincible || currentHealth <= 0) return;

        if (playerController != null)
        {
            playerController.InterruptDashAndActions();
        }

        currentHealth -= damageAmount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        CinemachineShake.Instance.ShakeCamera(0.2f);

        PlayerEnergy energyObj = GetComponent<PlayerEnergy>();
        if (energyObj != null) energyObj.CancelHeal(true);

        if (currentHealth > 0)
        {
            playerAnimator.PlayHitAnimation();
            if (hitEffectPrefab != null && playerController != null && playerController.wallCheckPoint != null)
            {
                GameObject effectObj = ObjectPoolManager.Instance.Spawn(hitEffectPrefab, playerController.wallCheckPoint.position, Quaternion.identity);
                effectObj.GetComponent<StickyEffect2D>().SetTarget(transform);
            }
            StartCoroutine(DamageRoutine(enemyTransform));
        }
        else
        {
            Die();
        }
    }

    private IEnumerator DamageRoutine(Transform enemyTransform)
    {
        isInvincible = true;
        playerController.enabled = false;
        rb.linearVelocity = Vector2.zero;

        float faceDirection = (enemyTransform.position.x > transform.position.x) ? 1f : -1f;
        transform.localScale = new Vector3(faceDirection, 1f, 1f);

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;

        float pushDirection = -faceDirection;
        rb.linearVelocity = new Vector2(pushDirection * knockbackForceX, knockbackForceY);

        yield return new WaitForSeconds(knockbackDuration);

        playerController.enabled = true;

        float elapsedTime = 0f;
        while (elapsedTime < iFrameDuration)
        {
            sr.color = new Color(1f, 1f, 1f, 0.5f);
            yield return new WaitForSeconds(0.1f);
            sr.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.2f;
        }

        sr.color = new Color(1f, 1f, 1f, 1f);
        isInvincible = false;
    }

    public void Heal(int healAmount)
    {
        if (currentHealth >= maxHealth || currentHealth <= 0) return;

        currentHealth += healAmount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void FullHeal()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        isInvincible = true;
        playerController.enabled = false;

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;

        playerAnimator.PlayDieAnimation();
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.bodyType = RigidbodyType2D.Dynamic;

        yield return new WaitForSeconds(2f);

        if (FadeManager.instance != null) yield return StartCoroutine(FadeManager.instance.FadeOut(1f));

        if (SaveManager.instance != null)
        {
            // Lấy ID dạng String
            string targetScene = SaveManager.instance.currentSaveData.respawnSceneName;
            string targetBench = SaveManager.instance.currentSaveData.respawnBenchID;

            SaveManager.instance.ResetNormalEnemies();

            string currentMapScene = SceneManager.GetActiveScene().name;
            if (currentMapScene != "Core_Scene")
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(currentMapScene);
                while (!unload.isDone) yield return null;
            }

            AsyncOperation load = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
            while (!load.isDone) yield return null;

            Scene loadedScene = SceneManager.GetSceneByName(targetScene);
            SceneManager.SetActiveScene(loadedScene);

            // Tìm kiếm cô lập Checkpoint
            bool foundBench = false;
            GameObject[] rootObjects = loadedScene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                Checkpoint[] benchesInScene = root.GetComponentsInChildren<Checkpoint>(true);
                foreach (Checkpoint bench in benchesInScene)
                {
                    if (bench.benchID == targetBench)
                    {
                        transform.position = bench.transform.position;
                        foundBench = true;
                        break;
                    }
                }
                if (foundBench) break;
            }
        }

        FullHeal();

        // Bật lại script điều khiển trước
        playerController.enabled = true;

        // GỌI HÀM ÉP NGỒI TRỰC TIẾP LÊN GHẾ
        playerController.SnapToRest();

        isInvincible = false;
        sr.color = new Color(1f, 1f, 1f, 1f);

        if (FadeManager.instance != null) yield return StartCoroutine(FadeManager.instance.FadeIn(1f));

        Debug.Log("ĐÃ HỒI SINH TẠI CHECKPOINT!");
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (collider.gameObject.layer == LayerMask.NameToLayer("EnemyDamage"))
        {
            TakeDamage(1, collider.transform);
        }
    }

    public void TakeTrapDamage(int damageAmount)
    {
        if (isInvincible || currentHealth <= 0) return;
        if (playerController != null) playerController.InterruptDashAndActions();

        currentHealth -= damageAmount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        CinemachineShake.Instance.ShakeCamera(0.3f);

        if (hitEffectPrefab != null && playerController != null)
        {
            GameObject effectObj = ObjectPoolManager.Instance.Spawn(hitEffectPrefab, transform.position, Quaternion.identity);
            effectObj.GetComponent<StickyEffect2D>().SetTarget(transform);
        }

        if (currentHealth > 0)
        {
            playerAnimator.PlayHitAnimation();
            StartCoroutine(TrapRespawnRoutine());
        }
        else
        {
            Die();
        }
    }

    private IEnumerator TrapRespawnRoutine()
    {
        isInvincible = true;
        playerController.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;

        yield return new WaitForSeconds(0.2f);

        transform.position = lastSafePosition;

        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        playerController.enabled = true;

        float elapsedTime = 0f;
        while (elapsedTime < iFrameDuration)
        {
            sr.color = new Color(1f, 1f, 1f, 0.5f);
            yield return new WaitForSeconds(0.1f);
            sr.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.2f;
        }
        sr.color = new Color(1f, 1f, 1f, 1f);
        isInvincible = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2.5f);
    }
}