using UnityEngine;
using System.Collections.Generic;

// --- CÁC ĐỊNH NGHĨA CHUNG ---
public enum ItemColor { Gray, Blue, Yellow, Red }
public enum EquipmentType { None, Weapon }
public enum ItemCategory { Equipment,  Material }

// ==========================================
// 1. CLASS GỐC (Dành cho mọi vật phẩm)
// ==========================================
public class ItemData : ScriptableObject
{
    [Header("--- THÔNG TIN CƠ BẢN ---")]
    [Tooltip("Mã ID tiếng Anh viết liền (VD: Sword_01). BẮT BUỘC TRÙNG VỚI TÊN FILE .asset!")]
    public string itemID;
    public string itemName;
    [TextArea(3, 5)] public string itemDescription;
    public Sprite itemIcon;
    public ItemCategory category;

    [Header("--- CƠ CHẾ CỘNG DỒN ---")]
    public bool isStackable;
    public int maxStackSize = 99;

    // Hàm Ảo để các class con tự định nghĩa cách sử dụng
    public virtual bool UseItem()
    {
        Debug.Log("Không thể sử dụng trực tiếp!");
        return false;
    }
}

// ==========================================
// 2. CLASS CON: TRANG BỊ (Vũ khí, Ngọc)
// ==========================================
[CreateAssetMenu(fileName = "New Equipment", menuName = "Inventory/Equipment")]
public class EquipmentData : ItemData
{
    [Header("--- HỆ THỐNG LẮP RÁP ---")]
    public ItemColor itemColor;
    public string mechanicToUnlock;
    public EquipmentType equipType = EquipmentType.None;
    public WeaponData weaponStats;

    public override bool UseItem()
    {
        Debug.Log($"Trang bị {itemName} cần được kéo thả để mặc!");
        return false;
    }
}

// ==========================================
// 3. CLASS CON: VẬT PHẨM TIÊU THỤ (Máu, Mana)
// ==========================================
[CreateAssetMenu(fileName = "New Material", menuName = "Inventory/Material")]
public class MaterialData : ItemData
{
    public override bool UseItem()
    {
        return true;
    }
}

// ==========================================
// 4. DỮ LIỆU CỦA VŨ KHÍ & Ô NGỌC
// ==========================================
[System.Serializable]
public class WeaponSlot
{
    public ItemColor allowedColor;
    public EquipmentData equippedItem;
    public bool isOccupied; // Thêm dòng này để fix lỗi CS1061
}

// --- Thêm vào phần WeaponData trong ItemData.cs ---
[CreateAssetMenu(fileName = "New Weapon", menuName = "Equipment/Weapon")]
public class WeaponData : ScriptableObject
{
    public string itemID; // Thêm dòng này để SaveManager gọi được
    [Header("--- CÁC Ô NGỌC (SLOTS) ---")]
    public List<WeaponSlot> slots = new List<WeaponSlot>();

    private void OnEnable()
    {
        if (slots != null)
            foreach (WeaponSlot slot in slots)
            {
                slot.equippedItem = null;
                slot.isOccupied = false; // Reset trạng thái khi bắt đầu
            }
    }
}
