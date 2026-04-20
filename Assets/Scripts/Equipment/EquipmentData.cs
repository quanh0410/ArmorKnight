using UnityEngine;
using System.Collections.Generic;

// 1. Định nghĩa các màu sắc của ô trang bị
public enum ItemColor
{
    Gray,
    Blue,
    Yellow,
    Red
}

public enum EquipmentType
{
    None,       // Dành cho Ngọc / Kỹ năng / Vật phẩm thường
    Weapon      // Dành cho Vũ khí
}

// 2. Dữ liệu cho từng Item
[CreateAssetMenu(fileName = "New Item", menuName = "Equipment/Item")]
public class ItemData : ScriptableObject
{
    [Header("--- THÔNG TIN CƠ BẢN ---")]
    public string itemName;

    // THÊM MỚI: Biến mô tả kèm thuộc tính TextArea giúp tạo ô nhập liệu nhiều dòng trong Inspector
    [TextArea(3, 5)]
    public string itemDescription;

    public Sprite itemIcon;

    [Header("--- HỆ THỐNG LẮP RÁP ---")]
    public ItemColor itemColor; // Màu của item này (Xanh, Vàng, Đỏ)

    [Tooltip("Tên của cơ chế/kỹ năng sẽ được mở khóa (VD: DoubleJump, DashStrike)")]
    public string mechanicToUnlock;

    [Header("--- PHÂN LOẠI TRANG BỊ ---")]
    public EquipmentType equipType = EquipmentType.None;

    [Tooltip("Nếu Item này là vũ khí, hãy kéo file WeaponData tương ứng vào đây")]
    public WeaponData weaponStats;
}

// 3. Cấu trúc của một Ô trang bị (Slot) trên vũ khí
[System.Serializable]
public class WeaponSlot
{
    public ItemColor allowedColor; // Màu yêu cầu của ô này
    public ItemData equippedItem;  // Item đang được gắn vào ô này (nếu có)
}

// 4. Dữ liệu cho Vũ khí
[CreateAssetMenu(fileName = "New Weapon", menuName = "Equipment/Weapon")]
public class WeaponData : ScriptableObject
{
    //[Header("--- THÔNG TIN VŨ KHÍ ---")]
    //public string weaponName;

    [Header("--- CÁC Ô NGỌC (SLOTS) ---")]
    [Tooltip("Danh sách các ô trang bị trên vũ khí này")]
    public List<WeaponSlot> slots = new List<WeaponSlot>();
    private void OnEnable()
    {
        // Quét qua tất cả các ô ngọc trên vũ khí này
        if (slots != null)
        {
            foreach (WeaponSlot slot in slots)
            {
                // Ép nó trở về trạng thái trống rỗng
                slot.equippedItem = null;
            }
        }
    }
}