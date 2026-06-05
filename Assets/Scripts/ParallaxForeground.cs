using UnityEngine;

public class ParallaxForeground : MonoBehaviour
{
    [Header("Cài đặt Cuộn cảnh (Parallax)")]
    public float parallaxMultiplierX = 1.2f;

    [Tooltip("Khuyên dùng: Nên để 0 hoặc số rất nhỏ (VD: 0.05). Nếu để quá lớn, khi Player nhảy lên, sương mù sẽ bay thốc lên che mất màn hình.")]
    public float parallaxMultiplierY = 0f;

    private Transform cameraTransform;

    // Lưu tọa độ tuyệt đối ban đầu
    private Vector3 startPosition;
    private Vector3 startCameraPosition;

    void Start()
    {
        cameraTransform = Camera.main.transform;

        // Chốt sổ vị trí gốc của cả Foreground và Camera ngay khi vừa mở Map
        startPosition = transform.position;
        startCameraPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        // 1. Tính toán tổng quãng đường Camera đã đi xa khỏi ĐIỂM XUẤT PHÁT
        Vector3 travelDistance = cameraTransform.position - startCameraPosition;

        // 2. Định vị lại Foreground một cách chính xác tuyệt đối, không có sai số cộng dồn
        float targetPosX = startPosition.x + (travelDistance.x * parallaxMultiplierX);
        float targetPosY = startPosition.y + (travelDistance.y * parallaxMultiplierY);

        // 3. Cập nhật vị trí
        transform.position = new Vector3(targetPosX, targetPosY, startPosition.z);
    }
}