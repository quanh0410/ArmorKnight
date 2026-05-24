using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("Lưu Trạng Thái (Save/Load)")]
    public bool isBossOrUnique = false;
    public string enemyID;

    [Header("Health Settings")]
    public int maxHealth = 30;

    public int currentHealth { get; private set; }
    public bool isDead { get; private set; }
    public bool isKnockedBack { get; private set; }

    public bool isStunned { get; private set; }

    [Header("Knockback Settings")]
    public float knockbackForce = 4f;
    public float knockbackDuration = 0.2f;

    [Header("--- TRẠNG THÁI ---")]
    public bool isInvincible = false; // Cờ chặn mọi sát thương
    private Animator anim;
    private Rigidbody2D rb;

    // --- MỚI: Biến lưu trữ Effect để tắt đi khi hết choáng ---
    private GameObject currentStunEffect;

    void Start()
    {
        if (SaveManager.instance != null && !string.IsNullOrEmpty(enemyID) && SaveManager.instance.IsObjectInteracted(enemyID))
        {
            Destroy(gameObject);
            return;
        }

        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int damageAmount, Transform attacker)
    {

        if (isInvincible) return;
        if (isDead) return;

        currentHealth -= damageAmount;

        if (currentHealth > 0)
        {
            if (anim != null) anim.SetTrigger("Hit");
            AudioManager.instance.PlaySFX("EnemyHit"); // Phát 1 lần duy nhất tại đây


            if (attacker != null && rb != null)
            {
                StartCoroutine(ApplyKnockback(attacker));
            }
        }
        else
        {
            DieProcess();
        }
    }

    // ==========================================
    // CẬP NHẬT: Nhận Prefab thay vì Object đã sinh ra
    // ==========================================
    public void TakeStun(float stunDuration, Transform attacker, GameObject effectPrefab = null)
    {
        if (isDead) return;

        if (attacker != null && rb != null)
        {
            StartCoroutine(ApplyKnockback(attacker));
        }

        // 1. Tắt effect cũ nếu quái đang bị choáng mà bị đánh bồi thêm
        if (currentStunEffect != null)
        {
            currentStunEffect.SetActive(false);
        }

        // 2. TỰ ĐỘNG SINH EFFECT VÀ QUẢN LÝ
        if (effectPrefab != null)
        {
            // Sinh effect ngay tại vị trí của quái
            currentStunEffect = ObjectPoolManager.Instance.Spawn(effectPrefab, transform.position, Quaternion.identity);

            // Ép Effect làm con của quái để nó bay theo khi quái bị đẩy lùi
            currentStunEffect.transform.SetParent(transform);

            // (Tùy chọn) Nhấc effect lên cao một chút cho ngay đỉnh đầu
            currentStunEffect.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        }

        // 3. Reset lại tiến trình đếm ngược
        StopCoroutine(nameof(StunRoutine));
        StartCoroutine(StunRoutine(stunDuration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        // Chỉ đóng băng hoạt ảnh, ĐÃ XÓA HIỆU ỨNG MÀU VÀNG
        if (anim != null) anim.speed = 0f;

        yield return new WaitForSeconds(duration);

        if (!isDead)
        {
            isStunned = false;
            if (anim != null) anim.speed = 1f;
        }

        // --- MỚI: Hết thời gian choáng -> Tắt Effect đi ---
        if (currentStunEffect != null)
        {
            currentStunEffect.SetActive(false); // Dùng SetActive(false) để Object Pool tự thu hồi
            currentStunEffect = null;
        }
    }

    private IEnumerator ApplyKnockback(Transform attacker)
    {
        isKnockedBack = true;

        float direction = attacker.position.x < transform.position.x ? 1f : -1f;

        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = new Vector2(direction * knockbackForce, rb.linearVelocity.y);

        yield return new WaitForSeconds(knockbackDuration);

        if (!isDead)
        {
            rb.linearVelocity = Vector2.zero;
            isKnockedBack = false;
        }
    }

    private void DieProcess()
    {
        isDead = true;
        isKnockedBack = false;
        isStunned = false;

        if (currentStunEffect != null)
        {
            currentStunEffect.SetActive(false);
            currentStunEffect = null;
        }

        if (SaveManager.instance != null && !string.IsNullOrEmpty(enemyID))
        {
            SaveManager.instance.SaveObjectState(enemyID, isBossOrUnique);
        }

        if (anim != null)
        {
            anim.speed = 1f;
            anim.SetTrigger("Die");
        }

        // =========================================================
        // SỬA TẠI ĐÂY: TẮT TẤT CẢ CÁC COLLIDER TRÊN QUÁI VÀ OBJECT CON
        // =========================================================
        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in allColliders)
        {
            col.enabled = false;
        }

        // Cố định cái xác để nó không rơi xuyên địa hình
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // Tắt hoàn toàn tương tác vật lý thay vì Kinematic
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}