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
            // 0. CHẶN ĐỨNG VŨ KHÍ: Không cho chui vào ô khảm ngọc
            if (equipData.equipType == EquipmentType.Weapon) return;

            // 1. CHỐNG LỖI KÉO THẢ TẠI CHỖ (ĐÃ FIX LỖI BÓNG MA)
            if (EquipmentManager.instance.currentWeapon != null &&
                EquipmentManager.instance.currentWeapon.slots[slotIndex].equippedItem == equipData)
            {
                draggedItem.isDropped = true;
                Destroy(draggedItem.gameObject); // Phải tiêu diệt icon đang cầm!
                InventoryUIManager.instance.DelayedRefresh();
                return;
            }

            // 2. Lắp ngọc mới
            if (equipData.itemColor == slotColor)
            {
                bool success = EquipmentManager.instance.TryEquipItem(equipData, slotIndex);
                if (success)
                {
                    draggedItem.isDropped = true;
                    Destroy(draggedItem.gameObject);
                    InventoryUIManager.instance.DelayedRefresh();
                    Debug.Log($"<color=green>Đã lắp {equipData.itemName} vào ô {slotIndex}!</color>");
                }
            }
        }
    }
}