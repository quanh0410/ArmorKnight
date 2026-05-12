using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    [Header("Kho đồ của bạn")]
    public List<ItemData> items = new List<ItemData>();

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    // ==========================================
    // TỐI ƯU 1: THÊM NHIỀU VẬT PHẨM CÙNG LÚC TỪ SHOP
    // ==========================================
    public void AddItem(ItemData newItem, int amount = 1)
    {
        if (newItem == null || amount <= 0) return;

        // Thêm nhanh vào danh sách
        for (int i = 0; i < amount; i++)
        {
            items.Add(newItem);
        }

        Debug.Log($"[TÚI ĐỒ] Đã thêm: {newItem.itemName} x{amount}");

        // Liên kết UI: Cập nhật lại kho đồ hiển thị trên màn hình ngay lập tức
        if (InventoryUIManager.instance != null)
        {
            InventoryUIManager.instance.RefreshInventoryFromSave();
        }
    }

    // ==========================================
    // TỐI ƯU 2: KIỂM TRA SỐ LƯỢNG
    // ==========================================
    public int GetItemCount(ItemData itemToSearch)
    {
        if (itemToSearch == null) return 0;

        int count = 0;
        foreach (ItemData item in items)
        {
            if (item == itemToSearch) count++;
        }
        return count;
    }

    // ==========================================
    // TỐI ƯU 3: XÓA NHIỀU VẬT PHẨM CÙNG LÚC AN TOÀN
    // ==========================================
    public bool RemoveItem(ItemData itemToRemove, int amount = 1)
    {
        if (itemToRemove == null || amount <= 0) return false;

        // Kiểm tra an toàn: Có đủ đồ để xóa không?
        if (GetItemCount(itemToRemove) < amount)
        {
            Debug.LogWarning($"[TÚI ĐỒ] Lỗi: Không đủ {itemToRemove.itemName} để tiêu thụ!");
            return false;
        }

        // Tiến hành xóa
        for (int i = 0; i < amount; i++)
        {
            items.Remove(itemToRemove);
        }

        Debug.Log($"[TÚI ĐỒ] Đã tiêu thụ: {itemToRemove.itemName} x{amount}");

        // Cập nhật lại UI kho đồ sau khi mất đồ
        if (InventoryUIManager.instance != null)
        {
            InventoryUIManager.instance.RefreshInventoryFromSave();
        }

        return true;
    }

    // ==========================================
    // LOAD GAME TỪ SAVE MANAGER
    // ==========================================
    public void LoadData(List<string> itemNames)
    {
        items.Clear();
        foreach (string name in itemNames)
        {
            ItemData data = SaveManager.instance.GetItemFromResources(name);
            if (data != null) items.Add(data);
        }
    }
}