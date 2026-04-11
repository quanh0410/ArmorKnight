using UnityEngine;
using System.Collections;
using System; // Bắt buộc phải thêm thư viện này để dùng Action

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth { get; private set; }

    // --- TẠO SỰ KIỆN (Trạm phát thanh) ---
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
    public float safePositionDelay = 0.2f; // Thời gian phải đứng trên đất để được tính là an toàn
    public LayerMask noSaveLayer; // --- 1. THÊM BIẾN NÀY ---
    public LayerMask trapLayer; // --- THÊM DÒNG NÀY (Để nhận diện Layer bẫy) ---
    private Vector2 lastSafePosition; // Tọa độ an toàn cuối cùng
    private float groundedTimer; // Bộ đếm thời gian

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

        // --- PHÁT TÍN HIỆU lần đầu khi game bắt đầu ---
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
        Debug.Log("Player bị quái chạm! Máu còn: " + currentHealth);

        // --- PHÁT TÍN HIỆU khi bị mất máu ---
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        CinemachineShake.Instance.ShakeCamera(0.2f); // Rung mạnh

        PlayerEnergy energyObj = GetComponent<PlayerEnergy>();
        if (energyObj != null) energyObj.CancelHeal(true); // true = Bị ngắt do ăn đòn

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

    // Thay thế hàm Die() cũ
    private void Die()
    {
        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        isInvincible = true;
        playerController.enabled = false; //

        // 1. Dừng hình một chút (Hit Stop) để cảm nhận độ nặng của đòn chí mạng
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;

        // 2. Chuyển sang hoạt ảnh Die một cách dứt khoát (không bị đè bởi lệnh Hit nữa)
        playerAnimator.PlayDieAnimation(); //

        // 3. Xử lý Vật lý của cái xác
 
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Đảm bảo không dùng Kinematic nữa (Xóa dòng Kinematic cũ của bạn)
        rb.bodyType = RigidbodyType2D.Dynamic;

        Debug.Log("GAME OVER!"); //
    }

    public void Heal(int healAmount)
    {
        if (currentHealth >= maxHealth || currentHealth <= 0) return;

        currentHealth += healAmount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        // --- PHÁT TÍN HIỆU khi được hồi máu ---
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (collider.gameObject.layer == LayerMask.NameToLayer("EnemyDamage"))
        {
            TakeDamage(1, collider.transform);
        }
    }

    // Cơ chế xử lý khi chạm bẫy
    // --- CẬP NHẬT TRONG PLAYERHEALTH.CS ---

    public void TakeTrapDamage(int damageAmount)
    {
        if (isInvincible || currentHealth <= 0) return; //

        if (playerController != null) playerController.InterruptDashAndActions(); //

        currentHealth -= damageAmount; //
        OnHealthChanged?.Invoke(currentHealth, maxHealth); //
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
            Die(); //
        }
    }

    private IEnumerator TrapRespawnRoutine()
    {
        isInvincible = true;
        playerController.enabled = false; // Khóa điều khiển

        // Đảm bảo đứng im hoàn toàn tại chỗ trúng bẫy
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // Tạm thời tắt vật lý để tránh bị bẫy đẩy đi tiếp

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration); // Dừng hình
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