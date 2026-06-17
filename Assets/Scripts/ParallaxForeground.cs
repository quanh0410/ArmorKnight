using UnityEngine;
using System.Collections; // BẮT BUỘC THÊM ĐỂ DÙNG COROUTINE

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

    // --- MỚI: Cờ kiểm tra xem Camera đã ổn định chưa ---
    private bool isReady = false;

    private IEnumerator Start()
    {
        cameraTransform = Camera.main.transform;

        yield return null;
        yield return null;
        startPosition = transform.position;
        startCameraPosition = cameraTransform.position;
        isReady = true;
    }

    void LateUpdate()
    {

        // --- MỚI: Nếu chưa khởi tạo xong thì không làm gì cả ---
        if (!isReady || cameraTransform == null) return;
        // 1. Tính toán tổng quãng đường Camera đã đi xa khỏi ĐIỂM XUẤT PHÁT
        Vector3 travelDistance = cameraTransform.position - startCameraPosition;

        // 2. Định vị lại Foreground một cách chính xác tuyệt đối, không có sai số cộng dồn
        float targetPosX = startPosition.x + (travelDistance.x * parallaxMultiplierX);
        float targetPosY = startPosition.y + (travelDistance.y * parallaxMultiplierY);

        // 3. Cập nhật vị trí
        transform.position = new Vector3(targetPosX, targetPosY, startPosition.z);
    }
}