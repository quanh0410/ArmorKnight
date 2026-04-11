using UnityEngine;

public class EnemyBat : EnemyBase
{
    private enum BatState { Idle, Chase }
    private BatState currentState;

    [Header("Bat Settings")]
    public float attackRange = 7f;
    public LayerMask groundLayer;

    // --- THÊM CÁC BIẾN TẠO ĐÀ BAY ---
    [Header("Flight Dynamics (Đà bay)")]
    public float acceleration = 8f;  // Gia tốc khi bắt đầu bay (Càng nhỏ lượn càng mượt, càng to thì cua càng gắt)
    public float deceleration = 5f;  // Quán tính phanh lại (Càng nhỏ thì lúc mất mục tiêu nó sẽ trượt trớn càng xa)

    private PlayerHealth targetHealth;

    protected override void Awake()
    {
        base.Awake(); //
        rb.gravityScale = 0f;

        if (player != null)
        {
            targetHealth = player.GetComponent<PlayerHealth>();
        }

        currentState = BatState.Idle;
    }

    protected override void ExecuteAI()
    {
        // 1. Kiểm tra mục tiêu
        if (player == null || (targetHealth != null && targetHealth.currentHealth <= 0))
        {
            currentState = BatState.Idle;
            HandleIdle();
            return;
        }

        // 2. Đo khoảng cách và tầm nhìn
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool canSeePlayer = false;

        if (distanceToPlayer <= attackRange)
        {
            RaycastHit2D hit = Physics2D.Linecast(transform.position, player.position, groundLayer);
            canSeePlayer = (hit.collider == null);
        }

        // 3. Quyết định trạng thái
        if (canSeePlayer)
        {
            currentState = BatState.Chase;
            HandleChase();
        }
        else
        {
            currentState = BatState.Idle;
            HandleIdle();
        }
    }

    private void HandleIdle()
    {
        anim.SetBool("isAttacking", false);

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, deceleration * Time.deltaTime);
    }

    private void HandleChase()
    {
        anim.SetBool("isAttacking", true);

        Vector2 direction = (player.position - transform.position).normalized;

        Vector2 targetVelocity = direction * moveSpeed;

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.deltaTime);

        if (Mathf.Abs(player.position.x - transform.position.x) > 0.1f)
        {
            Flip(player.position.x - transform.position.x);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (player != null)
        {
            bool isBlocked = Physics2D.Linecast(transform.position, player.position, groundLayer);
            Gizmos.color = isBlocked ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}