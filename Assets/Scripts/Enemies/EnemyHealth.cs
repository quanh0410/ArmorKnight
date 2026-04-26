using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("Lưu Trạng Thái (Save/Load)")]
    [Tooltip("Tích chọn nếu đây là Boss (Giết là mất vĩnh viễn). Bỏ tích nếu là quái thường (Sẽ hồi sinh khi ngồi ghế).")]
    public bool isBossOrUnique = false;
    public string enemyID;

    [Header("Health Settings")]
    public int maxHealth = 30;

    public int currentHealth { get; private set; }
    public bool isDead { get; private set; }
    public bool isKnockedBack { get; private set; }

    [Header("Knockback Settings")]
    public float knockbackForce = 4f;
    public float knockbackDuration = 0.2f;

    private Animator anim;
    private Rigidbody2D rb;

    void Start()
    {
        // KHI VỪA SINH RA: Hỏi SaveManager xem con quái này có trong danh sách chết chưa?
        // (SaveManager sẽ tự động check cả danh sách Vĩnh viễn lẫn Tạm thời)
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
        if (isDead) return;

        currentHealth -= damageAmount;

        if (currentHealth > 0)
        {
            if (anim != null) anim.SetTrigger("Hit");

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

        // KHI CHẾT: Báo cho SaveManager biết. 
        // Nó sẽ tự biết nhét vào danh sách nào dựa trên biến isBossOrUnique
        if (SaveManager.instance != null && !string.IsNullOrEmpty(enemyID))
        {
            SaveManager.instance.SaveObjectState(enemyID, isBossOrUnique);
        }

        if (anim != null) anim.SetTrigger("Die");

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}