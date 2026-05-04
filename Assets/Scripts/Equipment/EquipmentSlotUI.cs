using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IDropHandler
{
    public EquipmentType acceptedType = EquipmentType.Weapon;

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItemUI draggedItem = eventData.pointerDrag.GetComponent<InventoryItemUI>();
        if (draggedItem != null && draggedItem.itemData is EquipmentData equipData)
        {
            // 1. Chống lỗi thả lại vũ khí vào chính nó (ĐÃ FIX LỖI BÓNG MA)
            if (EquipmentManager.instance.currentWeapon == equipData.weaponStats)
            {
                draggedItem.isDropped = true;
                Destroy(draggedItem.gameObject); // Phải tiêu diệt icon đang cầm!
                InventoryUIManager.instance.DelayedRefresh();
                return;
            }

            // 2. Lắp vũ khí mới
            if (equipData.equipType == acceptedType && equipData.weaponStats != null)
            {
                draggedItem.isDropped = true;
                EquipmentManager.instance.EquipWeapon(equipData.weaponStats);
                Destroy(draggedItem.gameObject);
                InventoryUIManager.instance.DelayedRefresh();
            }
        }
    }
}