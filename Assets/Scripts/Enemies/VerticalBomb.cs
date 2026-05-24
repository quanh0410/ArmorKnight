using UnityEngine;

public class VerticalBomb : MonoBehaviour
{
    [Header("Cài đặt")]
    public float speed = 15f;
    public int damage = 1;
    public float lifeTime = 3f;
    public float hitRadius = 0.5f;

    [Header("Layer va chạm")]
    public LayerMask playerLayer;
    public LayerMask groundLayer;

    private float timer;
    private bool hasExploded = false;

    private Animator anim;
    private Rigidbody2D rb;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    // --- SỬA Ở ĐÂY: Không cần truyền direction (hướng) nữa vì đạn luôn rơi thẳng xuống ---
    public void Setup()
    {
        hasExploded = false;
        timer = 0f;
    }

    private void Update()
    {
        if (hasExploded) return;

        // --- SỬA Ở ĐÂY: Đổi Vector2.right thành Vector2.down để đạn rơi xuống ---
        transform.Translate(Vector2.down * speed * Time.deltaTime, Space.World);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Explode();
            return;
        }

        CheckCollision();
    }

    private void CheckCollision()
    {
        // 1. Chạm Player
        Collider2D playerHit = Physics2D.OverlapCircle(transform.position, hitRadius, playerLayer);
        if (playerHit != null)
        {
            PlayerHealth hp = playerHit.GetComponent<PlayerHealth>();
            if (hp != null) hp.TakeDamage(damage, transform);
            Explode();
            return;
        }

        // 2. Chạm Đất/Tường
        Collider2D groundHit = Physics2D.OverlapCircle(transform.position, hitRadius, groundLayer);
        if (groundHit != null)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;
        if (anim != null) anim.SetTrigger("Explode"); // Chạy Anim nổ
        AudioManager.instance.PlaySFX("Explode"); // Phát 1 lần duy nhất tại đây

    }

    // Gắn hàm này vào khung hình cuối của Animation Nổ (Animation Event)
    public void DestroyBomb()
    {
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.ReturnToPool(gameObject); // Dùng cho Pool
        else
            Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}