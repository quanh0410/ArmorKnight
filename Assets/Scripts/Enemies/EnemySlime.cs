using UnityEngine;

public class EnemySlime : EnemyBase
{
    [Header("Ranges")]
    public float chaseDistance = 7f;
    public float jumpDistance = 2.5f;

    // --- MỚI: THÔNG SỐ ĐÀ DI CHUYỂN ---
    [Header("Movement Dynamics")]
    public float acceleration = 12f; // Gia tốc đi bộ (rướn người)
    public float deceleration = 10f; // Quán tính phanh trượt

    [Header("Jump Attack Settings")]
    public Vector2 jumpForce = new Vector2(5f, 7f);
    public float jumpCooldown = 0.5f;
    private float lastJumpTime;

    [Header("Line of Sight")]
    public LayerMask obstacleLayer;

    [Header("Art Settings")]
    public bool isSpriteFlipped = false;

    private PlayerHealth targetHealth;
    private static readonly int IsWalkingParam = Animator.StringToHash("IsWalking");

    protected override void Awake()
    {
        base.Awake(); //
        if (player != null)
        {
            targetHealth = player.GetComponent<PlayerHealth>(); //
        }
    }

    protected override void ExecuteAI() //
    {
        if (player == null) return;

        if (targetHealth != null && targetHealth.currentHealth <= 0) //
        {
            StopSlime();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (HasLineOfSight())
        {
            if (distanceToPlayer <= jumpDistance)
            {
                anim.SetBool(IsWalkingParam, false);
                if (Time.time >= lastJumpTime + jumpCooldown)
                {
                    JumpAttack();
                }
                // QUAN TRỌNG: Không gọi SmoothStop() ở đây.
                // Để hệ thống vật lý tự do hoàn thành cú bay parabol!
            }
            else if (distanceToPlayer <= chaseDistance)
            {
                WalkTowardsPlayer();
            }
            else
            {
                StopSlime();
            }
        }
        else
        {
            StopSlime();
        }
    }

    private void WalkTowardsPlayer()
    {
        anim.SetBool(IsWalkingParam, true);
        float direction = Mathf.Sign(player.position.x - transform.position.x);

        // --- NÂNG CẤP: ĐI BỘ CÓ GIA TỐC TRỤC X ---
        float targetVelocityX = direction * moveSpeed; //
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX, acceleration * Time.deltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);

        // Truyền hướng đã tinh chỉnh nếu quái bị vẽ ngược
        Flip(isSpriteFlipped ? -direction : direction); //
    }

    // Hàm JumpAttack của bạn tính toán vật lý quá chuẩn, tôi giữ nguyên 100%!
    private void JumpAttack()
    {
        lastJumpTime = Time.time;
        float direction = Mathf.Sign(player.position.x - transform.position.x);
        Flip(isSpriteFlipped ? -direction : direction); //

        float distanceX = player.position.x - transform.position.x;
        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);

        float timeInAir = 1f;
        if (gravity > 0f) timeInAir = (2f * jumpForce.y) / gravity;

        float velocityX = distanceX / timeInAir;
        rb.linearVelocity = new Vector2(velocityX, jumpForce.y);
    }

    private bool HasLineOfSight()
    {
        if (player == null) return false;

        // --- NÂNG CẤP: CHUYỂN SANG LINECAST (ĐỒNG BỘ VỚI GOBLIN) ---
        RaycastHit2D hit = Physics2D.Linecast(transform.position, player.position, obstacleLayer);
        return hit.collider == null;
    }

    private void StopSlime()
    {
        anim.SetBool(IsWalkingParam, false);
        SmoothStop(); // Thay cho StopMovement() cũ
    }

    // --- MỚI: PHANH TRƯỢT TRÊN TRỤC X ---
    private void SmoothStop()
    {
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, deceleration * Time.deltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, jumpDistance);

        if (player != null)
        {
            // Vẽ đường Linecast giống hệt Goblin
            bool isBlocked = Physics2D.Linecast(transform.position, player.position, obstacleLayer);
            Gizmos.color = isBlocked ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}