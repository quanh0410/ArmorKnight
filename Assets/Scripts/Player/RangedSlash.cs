using UnityEngine;
using System.Collections.Generic; // BẮT BUỘC THÊM: Để sử dụng HashSet

public class RangedSlash : MonoBehaviour
{
    [Header("Cài đặt va chạm")]
    public float hitRadius = 1f;
    public LayerMask enemyLayer;
    public LayerMask groundLayer;
    public LayerMask enviromentLayer; // --- THÊM: Layer của dây leo ---
    public GameObject hitEffectPrefab;

    private float moveDirection;
    private float moveSpeed;
    private int damageAmount;
    private float maxLifeTime;
    private float currentTimer;

    // ==========================================
    // TỐI ƯU HÓA: Cuốn sổ ghi nhớ những kẻ địch đã bị chém
    // ==========================================
    private HashSet<Collider2D> damagedEnemies = new HashSet<Collider2D>();

    public void Setup(float direction, float speed, int damage, float duration)
    {
        moveDirection = Mathf.Sign(direction);
        moveSpeed = speed;
        damageAmount = damage;
        maxLifeTime = duration;
        currentTimer = 0f;

        // QUAN TRỌNG: Phải xé bỏ cuốn sổ cũ mỗi khi kiếm khí được bắn ra lần mới
        damagedEnemies.Clear();

        transform.localScale = new Vector3(moveDirection * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    void Update()
    {
        transform.Translate(Vector2.right * moveDirection * moveSpeed * Time.deltaTime);

        currentTimer += Time.deltaTime;
        if (currentTimer >= maxLifeTime)
        {
            gameObject.SetActive(false);
            return;
        }

        CheckCollision();
    }

    private void CheckCollision()
    {
        // 1. QUÉT TRÚNG QUÁI (CƠ CHẾ XUYÊN THẤU)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, hitRadius, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            // KIỂM TRA SỔ: Quái này đã bị chém ở khung hình trước chưa?
            if (!damagedEnemies.Contains(enemy))
            {
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health != null && !health.isDead)
                {
                    health.TakeDamage(damageAmount, transform); // Gây sát thương 1 LẦN DUY NHẤT

                    if (hitEffectPrefab != null)
                    {
                        ObjectPoolManager.Instance.Spawn(hitEffectPrefab, transform.position, Quaternion.identity);
                    }

                    // GHI VÀO SỔ: Đánh dấu là đã chém con này rồi!
                    damagedEnemies.Add(enemy);
                }
            }
        }

        // 2. --- THÊM MỚI: Quét Môi trường (Dây leo) xuyên thấu ---
        Collider2D[] hitEnvs = Physics2D.OverlapCircleAll(transform.position, hitRadius, enviromentLayer);
        foreach (Collider2D env in hitEnvs)
        {
            // Kiểm tra xem đã chém sợi dây leo này chưa (dùng chung sổ với quái)
            if (!damagedEnemies.Contains(env))
            {
                VineInteraction vine = env.GetComponent<VineInteraction>();
                if (vine != null)
                {
                    vine.TakeRangedHit(); // Gọi hàm Cắt đứt
                    damagedEnemies.Add(env); // Ghi vào sổ để không quét lại ở khung hình sau
                }
            }
        }

        // 2. QUÉT TRÚNG TƯỜNG (KIẾM KHÍ SẼ VỠ VÀ BIẾN MẤT)
        Collider2D groundHit = Physics2D.OverlapCircle(transform.position, hitRadius, groundLayer);
        if (groundHit != null)
        {
            // Chạm tường thì nổ tung một cái cho đẹp rồi biến mất
            if (hitEffectPrefab != null)
            {
                ObjectPoolManager.Instance.Spawn(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}