using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // Biến Singleton để dễ dàng gọi từ các script khác (như khi nhặt đồ)
    public static InventoryManager instance;

    [Header("Kho đồ của bạn")]
    public List<ItemData> items = new List<ItemData>(); // Danh sách các item đang có

    private void Awake()
    {
        // Khởi tạo Singleton
        if (instance == null) instance = this;
    }

    // Hàm gọi khi người chơi nhặt được 1 item mới
    public void AddItem(ItemData newItem)
    {
        items.Add(newItem);
        Debug.Log("Đã nhặt được: " + newItem.itemName);
    }

    // Kiểm tra xem trong túi có bao nhiêu vật phẩm loại này
    public int GetItemCount(ItemData itemToSearch)
    {
        int count = 0;
        foreach (ItemData item in items)
        {
            if (item == itemToSearch) count++;
        }
        return count;
    }

    // Xóa một vật phẩm cụ thể khỏi túi (dùng khi tiêu thụ chìa khóa)
    public void RemoveItem(ItemData itemToRemove)
    {
        if (items.Contains(itemToRemove))
        {
            items.Remove(itemToRemove);
            Debug.Log("Đã tiêu thụ: " + itemToRemove.itemName);
        }
    }
    // Trong InventoryManager.cs
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