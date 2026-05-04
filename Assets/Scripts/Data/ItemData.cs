using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 1. CLASS GỐC (ĐƯA LÊN TRÊN CÙNG)
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

    public virtual bool UseItem()
    {
        Debug.Log("Không thể sử dụng trực tiếp!");
        return false;
    }
}

// --- CÁC ĐỊNH NGHĨA CHUNG (ĐƯA XUỐNG DƯỚI CÙNG) ---
public enum ItemColor { Gray, Blue, Yellow, Red }
public enum EquipmentType { None, Weapon }
public enum ItemCategory { Equipment, Material }