using UnityEngine;

public class ParallaxForeground : MonoBehaviour
{
    [Header("Cài đặt Cuộn cảnh (Parallax)")]
    [Tooltip("Tỷ lệ dịch chuyển. Với Foreground, bạn có thể thử số lớn hơn 1 hoặc số âm (VD: -0.2 hoặc 1.2) để nó trôi nhanh hơn Camera")]
    public float parallaxMultiplierX = 1.2f;
    public float parallaxMultiplierY = 1.2f;

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    void Start()
    {
        // Tự động tìm và bám theo Camera chính (kể cả khi bạn dùng Cinemachine, nó vẫn sẽ điều khiển Camera chính)
        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        // 1. Tính toán xem khung hình này Camera đã đi được bao xa
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // 2. Tính toán vị trí mới cho Foreground dựa trên quãng đường Camera đi được nhân với tỷ lệ
        float targetPosX = transform.position.x + (deltaMovement.x * parallaxMultiplierX);
        float targetPosY = transform.position.y + (deltaMovement.y * parallaxMultiplierY);

        // 3. Dịch chuyển Foreground
        transform.position = new Vector3(targetPosX, targetPosY, transform.position.z);

        // 4. Lưu lại vị trí Camera để tính toán cho khung hình tiếp theo
        lastCameraPosition = cameraTransform.position;
    }
}