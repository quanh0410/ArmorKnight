using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    public int damage = 1; // Tha hồ chỉnh sát thương cho từng loại bẫy ngoài Inspector

    // Cái bẫy tự động chờ Player giẫm vào
    private void OnTriggerStay2D(Collider2D collision)
    {
        // Kiểm tra xem thứ vừa giẫm vào bẫy có phải là Player không (nhớ tag Player nhé)
        if (collision.CompareTag("Player"))
        {
            // Tìm máu của Player và gọi hàm trừ máu do bẫy
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeTrapDamage(damage);
            }
        }
    }
}