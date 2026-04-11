using UnityEngine;

public class EnemyBomberGoblin : EnemyBase
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float shotDelay = 1.5f;
    [SerializeField] private Transform firePos;
    [SerializeField] private GameObject bombPrefab;

    // --- MỚI: TẦM NHÌN VÀ QUÁN TÍNH ---
    [Header("Line of Sight & Dynamics")]
    [SerializeField] private LayerMask obstacleLayer; // MỚI: Tường cản tầm nhìn (thay cho groundLayer cũ)
    [SerializeField] private float deceleration = 10f; // MỚI: Quán tính trượt khi tiếp đất

    [Header("Flee (Jump Back) Settings")]
    [SerializeField] private float fleeDistance = 2.5f;
    [SerializeField] private Vector2 fleeJumpForce = new Vector2(8f, 5f);
    [SerializeField] private float fleeCooldown = 2f;
    [SerializeField] private float fleeJumpDuration = 0.5f;

    private float lastFleeTime;
    private float nextShot;
    private PlayerHealth targetHealth;

    protected override void Awake()
    {
        base.Awake();
        if (player != null)
        {
            targetHealth = player.GetComponent<PlayerHealth>();
        }
    }

    protected override void ExecuteAI()
    {
        if (player == null) return;
        if (targetHealth != null && targetHealth.currentHealth <= 0) return;

        // Đang trong thời gian bay lùi thì không can thiệp vật lý, để nó bay tự nhiên
        if (Time.time < lastFleeTime + fleeJumpDuration) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // --- MỚI: KIỂM TRA TẦM NHÌN ---
        bool canSeePlayer = HasLineOfSight();

        // 1. Nếu bị áp sát VÀ nhìn thấy Player -> Nhảy lùi
        if (distance <= fleeDistance && canSeePlayer)
        {
            if (Time.time >= lastFleeTime + fleeCooldown)
            {
                FleeAndAttack();
            }
            else
            {
                SmoothStop(); // Đang đợi Cooldown thì đứng phanh lại
            }
        }
        // 2. Nếu ở khoảng cách an toàn VÀ nhìn thấy Player -> Ném bom
        else if (distance <= attackRange && canSeePlayer)
        {
            float faceDirection = player.position.x > transform.position.x ? 1f : -1f;
            Flip(faceDirection);
            SmoothStop(); // Trượt nhẹ lại để đứng vững trước khi ném
            Shoot();
        }
        // 3. Ngoài tầm hoặc bị tường chắn -> Đứng chơi
        else
        {
            SmoothStop();
        }
    }

    // --- MỚI: HÀM PHANH TRƯỢT TRỤC X ---
    private void SmoothStop()
    {
        // Kéo vận tốc trục X về 0 từ từ (giúp pha tiếp đất sau khi nhảy lùi cực kỳ mượt)
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, deceleration * Time.deltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    private bool HasLineOfSight()
    {
        if (player == null) return false;
        // Bắn tia Linecast từ vị trí ném bom đến người chơi
        RaycastHit2D hit = Physics2D.Linecast(firePos.position, player.position, obstacleLayer);
        return hit.collider == null; // Trả về true nếu không có tường chắn
    }

    private void FleeAndAttack()
    {
        lastFleeTime = Time.time;
        float faceDirection = player.position.x > transform.position.x ? 1f : -1f;
        Flip(faceDirection);

        // Bật nhảy lùi
        rb.linearVelocity = new Vector2(-faceDirection * fleeJumpForce.x, fleeJumpForce.y);

        // Ném bom khẩn cấp ngay trên không
        nextShot = Time.time + shotDelay;
        anim.SetTrigger("isShooting");
    }

    private void Shoot()
    {
        if (Time.time > nextShot)
        {
            nextShot = Time.time + shotDelay;
            anim.SetTrigger("isShooting");
        }
    }

    // Hàm FireArrow tính toán quỹ đạo parabol giữ nguyên hoàn toàn
    public void FireArrow()
    {
        if (player == null || health.isDead) return;

        Vector2 firePosition = firePos.position;
        Vector2 targetPosition = player.position;
        float dx = targetPosition.x - firePosition.x;
        float dy = targetPosition.y - firePosition.y;
        float angleDeg = 45f;
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float gravity = Mathf.Abs(Physics2D.gravity.y);
        float cos = Mathf.Cos(angleRad);
        float sin = Mathf.Sin(angleRad);

        float absDx = Mathf.Abs(dx);
        float denominator = 2 * (absDx * Mathf.Tan(angleRad) - dy) * cos * cos;

        if (Mathf.Approximately(denominator, 0f) || denominator < 0f) return;

        float v0Squared = (gravity * absDx * absDx) / denominator;
        float v0 = Mathf.Sqrt(v0Squared);
        float vx = v0 * cos * Mathf.Sign(dx);
        float vy = v0 * sin;

        GameObject bomb = ObjectPoolManager.Instance.Spawn(bombPrefab, firePosition, Quaternion.identity);
        Rigidbody2D bombRb = bomb.GetComponent<Rigidbody2D>();
        if (bombRb != null)
        {
            bombRb.linearVelocity = new Vector2(vx, vy);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeDistance);

        if (player != null && firePos != null)
        {
            // Vẽ đường Linecast trên Scene để dễ debug
            bool isBlocked = Physics2D.Linecast(firePos.position, player.position, obstacleLayer);
            Gizmos.color = isBlocked ? Color.red : Color.green;
            Gizmos.DrawLine(firePos.position, player.position);
        }
    }
}