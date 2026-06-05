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
    public bool isInvincible = false;

    // --- MỚI: Biến chặn hiệu ứng Hit/Knockback (Giáp siêu việt) ---
    public bool isUnstoppable = false;

    private Animator anim;
    private Rigidbody2D rb;
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
            AudioManager.instance.PlaySFX("EnemyHit");

            // CẢI TIẾN: Chỉ chạy hoạt ảnh Hit và Knockback nếu KHÔNG CÓ giáp siêu việt
            if (!isUnstoppable)
            {
                if (anim != null) anim.SetTrigger("Hit");

                if (attacker != null && rb != null)
                {
                    StartCoroutine(ApplyKnockback(attacker));
                }
            }
        }
        else
        {
            DieProcess();
        }
    }

    public void TakeStun(float stunDuration, Transform attacker, GameObject effectPrefab = null)
    {
        if (isDead) return;

        if (attacker != null && rb != null && !isUnstoppable) // Chặn knockback nếu có giáp
        {
            StartCoroutine(ApplyKnockback(attacker));
        }

        if (currentStunEffect != null)
        {
            currentStunEffect.SetActive(false);
        }

        if (effectPrefab != null)
        {
            currentStunEffect = ObjectPoolManager.Instance.Spawn(effectPrefab, transform.position, Quaternion.identity);
            currentStunEffect.transform.SetParent(transform);
            currentStunEffect.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        }

        StopCoroutine(nameof(StunRoutine));
        StartCoroutine(StunRoutine(stunDuration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        if (anim != null) anim.speed = 0f;

        yield return new WaitForSeconds(duration);

        if (!isDead)
        {
            isStunned = false;
            if (anim != null) anim.speed = 1f;
        }

        if (currentStunEffect != null)
        {
            currentStunEffect.SetActive(false);
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

        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in allColliders)
        {
            col.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}