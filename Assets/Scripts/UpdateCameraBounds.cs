using UnityEngine;
using Unity.Cinemachine;

public class UpdateCameraBounds : MonoBehaviour
{
    private void Start()
    {
        // 1. Lấy khung giới hạn của phòng hiện tại 
        Collider2D myBounds = GetComponent<Collider2D>();

        // 2. Tìm cái Camera ở Core_Scene
        CinemachineConfiner2D confiner = FindObjectOfType<CinemachineConfiner2D>();

        // 3. Tròng khung giới hạn mới vào Camera
        if (confiner != null && myBounds != null)
        {
            confiner.BoundingShape2D = myBounds;
            confiner.InvalidateBoundingShapeCache();

            // --- THÊM LOGIC CHỐNG GIẬT CAMERA TẠI ĐÂY ---
            // Lấy component Camera chính của Cinemachine (chung GameObject với Confiner)
            CinemachineCamera cineCam = confiner.GetComponent<CinemachineCamera>();
            if (cineCam != null)
            {
                // Lệnh tối thượng này nói với Cinemachine: 
                // "Hãy quên khung hình cũ đi, đừng cố lia máy mượt nữa, snap ngay lập tức!"
                cineCam.PreviousStateIsValid = false;
            }
        }
    }
}