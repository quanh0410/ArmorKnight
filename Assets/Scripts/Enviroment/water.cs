using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))] // ??m b?o luôn có BoxCollider2D ?i kèm
public class water : MonoBehaviour
{
    [Header("--- CÀI ??T N??C ---")]
    [Tooltip("T?c ?? ?i b? c?a nhân v?t khi ? d??i n??c")]
    [SerializeField] private float waterMoveSpeed = 2f;

    // Bi?n này dùng ?? ghi nh? t?c ?? g?c c?a ng??i ch?i tr??c khi xu?ng n??c
    private float originalSpeed;

    // HÀM ???C G?I KHI NHÂN V?T V?A CH?M VÀO N??C
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ki?m tra xem v?t th? ch?m vào có script PlayerController không
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            // 1. L?u l?i t?c ?? hi?n t?i c?a ng??i ch?i (tr??c khi thay ??i)
            originalSpeed = player.moveSpeed;

            // 2. ??i t?c ?? c?a ng??i ch?i thành t?c ?? d??i n??c
            player.moveSpeed = waterMoveSpeed;
        }
    }

    // HÀM ???C G?I KHI NHÂN V?T R?I KH?I N??C
    private void OnTriggerExit2D(Collider2D other)
    {
        // Tìm l?i script PlayerController c?a nhân v?t v?a r?i ?i
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            // 3. Tr? l?i t?c ?? g?c ?ã ???c ghi nh? cho ng??i ch?i
            player.moveSpeed = originalSpeed;
        }
    }
}