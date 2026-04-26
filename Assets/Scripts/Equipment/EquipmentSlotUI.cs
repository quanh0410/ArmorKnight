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
            // Chống lỗi thả lại vũ khí vào chính nó
            if (EquipmentManager.instance.currentWeapon == equipData.weaponStats)
            {
                draggedItem.isDropped = true;
                InventoryUIManager.instance.DelayedRefresh();
                return;
            }

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