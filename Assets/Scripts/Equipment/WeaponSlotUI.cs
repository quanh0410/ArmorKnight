using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponSlotUI : MonoBehaviour, IDropHandler
{
    public ItemColor slotColor;
    public int slotIndex;

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItemUI draggedItem = eventData.pointerDrag.GetComponent<InventoryItemUI>();
        if (draggedItem != null && draggedItem.itemData is EquipmentData equipData)
        {
            // 1. CHỐNG LỖI KÉO THẢ TẠI CHỖ (Cầm ngọc trong ô và lỡ tay thả lại vào chính ô đó)
            if (EquipmentManager.instance.currentWeapon.slots[slotIndex].equippedItem == equipData)
            {
                draggedItem.isDropped = true; // Cắm cờ an toàn
                InventoryUIManager.instance.DelayedRefresh();
                return;
            }

            // 2. Lắp ngọc mới
            if (equipData.itemColor == slotColor)
            {
                bool success = EquipmentManager.instance.TryEquipItem(equipData, slotIndex);
                if (success)
                {
                    draggedItem.isDropped = true; // Cắm cờ an toàn
                    Destroy(draggedItem.gameObject);
                    InventoryUIManager.instance.DelayedRefresh();
                    Debug.Log($"<color=green>Đã lắp {equipData.itemName} vào ô số {slotIndex} thành công!</color>");
                }
                else
                {
                    Debug.LogError($"<color=red>Lỗi Logic:</color> Ô số {slotIndex} báo bận! (Occupied: {EquipmentManager.instance.currentWeapon.slots[slotIndex].isOccupied})");
                }
            }
        }
    }
}