using UnityEngine;
using System.Collections;
using UnityEngine.Events; // Thư viện cực mạnh để tạo Event trong Unity

public class EnemyHealth : MonoBehaviour
{
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