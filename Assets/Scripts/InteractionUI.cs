using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI instance; // Biến Singleton để gọi từ mọi nơi

    [Header("Cấu hình Giao diện")]
    public GameObject popupPanel;      // Khung chứa chữ
    public TextMeshProUGUI promptText; // Chữ hiển thị
    public Vector3 offset = new Vector3(0, 1.5f, 0); // Độ cao của chữ so với vật phẩm

    private void Awake()
    {
        // Khởi tạo Singleton
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // Mặc định ẩn UI khi mới vào game
        Hide();
    }

    // Hàm gọi để hiện chữ và dịch chuyển đến mục tiêu
    public void Show(Transform targetTransform, string message)
    {
        // Dịch chuyển UI đến vị trí vật phẩm + nhích lên trên một chút
        transform.position = targetTransform.position + offset;

        // Đổi nội dung chữ (Ví dụ: "[S] Nhặt Kiếm")
        if (promptText != null) promptText.text = message;

        popupPanel.SetActive(true);
    }

    // Hàm gọi để ẩn chữ đi
    public void Hide()
    {
        popupPanel.SetActive(false);
    }
}