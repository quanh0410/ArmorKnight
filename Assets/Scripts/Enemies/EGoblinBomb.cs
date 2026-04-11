using UnityEngine;
using System.Collections; // Thêm thư viện này để dùng Coroutine

public class EGoblinBomb : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    [SerializeField] private int damage = 1;
    [SerializeField] private float timeDestroy = 3f;

    [Header("Deflect Settings")]
    public float deflectForceX = 10f;
    public float deflectForceY = 5f;
    public bool isDeflected { get; private set; }

    private Animator animator;
    private Rigidbody2D rb;
    private bool hasExploded = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    // --- HÀM NÀY CHẠY MỖI KHI ĐẠN ĐƯỢC LẤY RA TỪ POOL ---
    private void OnEnable()
    {
        // "Rửa ly": Đặt lại toàn bộ trạng thái như lúc mới sinh ra
        hasExploded = false;
        isDeflected = false;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // Trả lại vật lý bình thường
            rb.linearVelocity = Vector2.zero;
        }

        // Bắt đầu đếm ngược thời gian nổ tự động
        StartCoroutine(ReturnToPoolAfterTime());
    }

    private void Update()
    {
        if (!hasExploded)
        {
            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
            if (isGrounded)
            {
                Explode();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasExploded) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") || collision.CompareTag("Player"))
        {
            if (isDeflected) return;

            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, transform);
            }
            Explode();
        }
    }

    public void Deflect(Transform attacker)
    {
        if (hasExploded) return;
        isDeflected = true;
        float direction = attacker.position.x < transform.position.x ? 1f : -1f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.linearVelocity = new Vector2(direction * deflectForceX, deflectForceY);
        }
    }

    private void Explode()
    {
        hasExploded = true;
        if (animator != null) animator.SetTrigger("Explode");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    // Animation Event sẽ gọi hàm này ở cuối clip Explode
    public void DestroyBomb()
    {
        // Trả bom về Siêu Tủ Chứa dùng chung
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject); // Đề phòng lỗi quên bỏ ObjectPoolManager vào Scene
        }
    }

    // Coroutine thay thế cho Destroy(gameObject, timeDestroy) trong Start cũ
    private IEnumerator ReturnToPoolAfterTime()
    {
        yield return new WaitForSeconds(timeDestroy);
        if (!hasExploded) // Nếu chưa nổ mà hết giờ thì tự cho nổ
        {
            Explode();
        }
    }
}