using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI instance; 

    [Header("Cấu hình Giao diện")]
    public GameObject popupPanel;      
    public TextMeshProUGUI promptText; 
    public Vector3 offset = new Vector3(0, 1.5f, 0); 

    private void Awake()
    {
        // Khởi tạo Singleton
        if (instance == null) instance = this;
        else Destroy(gameObject);

        Hide();
    }

    public void Show(Transform targetTransform, string message)
    {
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