using UnityEngine;
using Unity.Cinemachine; // BẮT BUỘC dùng thư viện này cho Cinemachine bản mới

[RequireComponent(typeof(CinemachineCamera))]
public class CameraItemZoom : MonoBehaviour
{
    private CinemachineCamera cam;

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

        // Tự động lấy kích thước Lens hiện tại làm mặc định lúc mới vào game
        if (cam != null)
        {
            defaultLensSize = cam.Lens.OrthographicSize;
        }
    }

    void Update()
    {
        if (cam == null) return;

        // KIỂM TRA ITEM: Xem có đang đeo đồ có chữ "ExpandView" không?
        bool hasZoomItem = false;
        if (EquipmentManager.instance != null && EquipmentManager.instance.HasMechanic("ExpandView"))
        {
            hasZoomItem = true;
        }

        // Quyết định mục tiêu Lens cần đạt đến
        float targetLens = hasZoomItem ? expandedLensSize : defaultLensSize;

        // Lấy struct Lens hiện tại của Cinemachine
        var lens = cam.Lens;

        // Làm mượt (Lerp) giá trị từ hiện tại tiến tới mục tiêu
        lens.OrthographicSize = Mathf.Lerp(lens.OrthographicSize, targetLens, Time.deltaTime * zoomSpeed);

        // Gán ngược Lens đã cập nhật trở lại cho Camera
        cam.Lens = lens;
    }
}