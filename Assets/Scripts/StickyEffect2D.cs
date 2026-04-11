using UnityEngine;

public class StickyEffect2D : MonoBehaviour
{
    [Header("Offset Settings")]
    public Vector2 positionOffset = Vector2.zero;

    // Biến để lưu lại độ lớn Scale ban đầu của hiệu ứng
    private Vector3 initialScale;

    private void Awake()
    {
        // Lưu lại độ lớn Scale chuẩn khi Awake (trước khi bị Object Pool lôi ra xài lại)
        initialScale = new Vector3(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y), Mathf.Abs(transform.localScale.z));
    }

    public void SetTarget(Transform targetParent)
    {
        // 1. Gắn vào người mục tiêu
        // Dùng false để Unity tính toán vị trí tương đối ngay lập tức
        transform.SetParent(targetParent, false);

        // 2. Chỉnh lại vị trí tương đối theo offset
        transform.localPosition = positionOffset;

        // 3. SỬA LỖI Ở ĐÂY: Reset Local Scale về dương.
        // Khi Parent (player/quái) lật (localScale.x âm), con (hiệu ứng) sẽ TỰ ĐỘNG lật theo hình ảnh.
        // Ta cần đảm bảo Local Scale của con luôn dương để thừa hưởng chuẩn xác từ cha.
        transform.localScale = initialScale;
    }
}
