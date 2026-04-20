using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IDropHandler
{
    public EquipmentType acceptedType = EquipmentType.Weapon;

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItemUI draggedItem = eventData.pointerDrag.GetComponent<InventoryItemUI>();

        if (draggedItem != null)
        {
            if (EquipmentManager.instance.currentWeapon != null) return;

            if (draggedItem.itemData.equipType == acceptedType && draggedItem.itemData.weaponStats != null)
            {
                // 1. Chuyển nhà
                draggedItem.transform.SetParent(transform);

                // --- KỸ THUẬT BÓNG MA: Ép tàng hình và phóng to bằng đúng khung chứa ---
                RectTransform rect = draggedItem.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; // Căng về 4 góc
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;

                // Tắt hình ảnh viền và icon để nhường chỗ cho thanh kiếm to hiện lên
                if (draggedItem.iconImage != null) draggedItem.iconImage.enabled = false;
                if (draggedItem.borderImage != null) draggedItem.borderImage.enabled = false;
                // ------------------------------------------------------------------------

                draggedItem.originalParent = transform;

                EquipmentManager.instance.EquipWeapon(draggedItem.itemData.weaponStats);

                // Ép cục bóng ma này nằm trên cùng để luôn bắt trúng tia click chuột
                draggedItem.transform.SetAsLastSibling();
            }
            else
            {
                ReturnToInventory(draggedItem);
            }
        }
    }

    private void ReturnToInventory(InventoryItemUI item)
    {
        item.transform.SetParent(item.originalParent);
        item.GetComponent<RectTransform>().localPosition = Vector3.zero;
    }
}