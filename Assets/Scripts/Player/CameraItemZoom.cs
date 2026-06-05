using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraItemZoom : MonoBehaviour
{
    private CinemachineCamera cam;
    private CinemachineConfiner2D confiner;

    [Header("Cài đặt Lens")]
    [Tooltip("Thông số Lens gốc (VD: 3.34)")]
    public float defaultLensSize = 3.34f;

    [Tooltip("Thông số Lens khi đeo Item (Càng to nhìn càng rộng)")]
    public float expandedLensSize = 5f;

    [Tooltip("Tốc độ zoom mượt mà")]
    public float zoomSpeed = 3f;

    void Start()
    {
        cam = GetComponent<CinemachineCamera>();
        confiner = GetComponent<CinemachineConfiner2D>(); // Lấy Confiner chung GameObject

        // Tự động lấy kích thước Lens hiện tại làm mặc định lúc mới vào game
        if (cam != null)
        {
            defaultLensSize = cam.Lens.OrthographicSize;
        }
    }

    void Update()
    {
        if (cam == null) return;

        // KIỂM TRA ITEM
        bool hasZoomItem = false;
        if (EquipmentManager.instance != null && EquipmentManager.instance.HasMechanic("ExpandView"))
        {
            hasZoomItem = true;
        }

        // Quyết định mục tiêu Lens cần đạt đến
        float targetLens = hasZoomItem ? expandedLensSize : defaultLensSize;

        var lens = cam.Lens;
        float currentSize = lens.OrthographicSize;

        // Bỏ qua nếu camera đã đạt đúng kích thước mục tiêu (Tiết kiệm hiệu năng)
        if (Mathf.Approximately(currentSize, targetLens)) return;

        // Làm mượt (Lerp) giá trị từ hiện tại tiến tới mục tiêu
        float newSize = Mathf.Lerp(currentSize, targetLens, Time.deltaTime * zoomSpeed);

        // SNAP (Làm tròn): Nếu tiến rất gần đến đích, ép bằng đích luôn để dừng Lerp
        if (Mathf.Abs(targetLens - newSize) < 0.01f)
        {
            newSize = targetLens;
        }

        // Gán ngược Lens đã cập nhật
        lens.OrthographicSize = newSize;
        cam.Lens = lens;

        // BẮT BUỘC: Ép Confiner tính toán lại mép va chạm với kích thước Camera mới
        if (confiner != null)
        {
            confiner.InvalidateBoundingShapeCache();
        }
    }
}