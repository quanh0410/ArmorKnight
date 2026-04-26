using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    public int coinValue = 1;

    private Animator anim;
    private Rigidbody2D rb; // Caching Rigidbody
    private bool isCollected = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>(); // Lưu sẵn từ đầu để tối ưu hiệu năng
    }

    // Nếu bạn dùng Object Pool, phải reset trạng thái khi đồng xu được bật lại
    private void OnEnable()
    {
        isCollected = false;
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic; // Trả lại vật lý bình thường
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            isCollected = true;

            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(coinValue);
            }

            if (anim != null) anim.SetTrigger("PickUp");

            // Dùng biến rb đã lưu sẵn, không tốn chi phí tìm kiếm nữa
            if (rb != null) rb.bodyType = RigidbodyType2D.Static;
        }
    }

 
    // Hàm này được gọi từ Animation Event ở frame cuối của anim "PickUp"
    private void Collected()
    {
        // Kiểm tra xem có Object Pool trong Scene không để tránh lỗi Null
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            // Code phòng hờ nếu bạn quên kéo Manager vào Scene
            Destroy(gameObject);
        }
    }
}