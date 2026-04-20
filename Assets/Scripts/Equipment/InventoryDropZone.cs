using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        InventoryItemUI draggedItem = eventData.pointerDrag.GetComponent<InventoryItemUI>();
        if (draggedItem == null) return;

        InventoryUIManager uiManager = FindObjectOfType<InventoryUIManager>();

        // KIỂM TRA 1: Tháo Ngọc (từ WeaponSlotUI)
        WeaponSlotUI gemSlot = draggedItem.originalParent.GetComponent<WeaponSlotUI>();
        if (gemSlot != null)
        {
            // 1. Cập nhật Não bộ (Data)
            EquipmentManager.instance.currentWeapon.slots[gemSlot.slotIndex].equippedItem = null;

            // 2. Hủy bỏ cục UI đang cầm trên chuột để tránh lỗi hiển thị
            Destroy(draggedItem.gameObject);

            // 3. Ra lệnh vẽ lại toàn bộ kho đồ (Ngọc sẽ tự động hiện ra ở đúng hàng)
            uiManager.RefreshInventoryUI();
            return;
        }

        // KIỂM TRA 2: Tháo Vũ khí (từ EquipmentSlotUI)
        EquipmentSlotUI equipSlot = draggedItem.originalParent.GetComponent<EquipmentSlotUI>();
        if (equipSlot != null)
        {
            // 1. Não bộ tháo vũ khí (và tự động tháo cả ngọc)
            EquipmentManager.instance.UnequipWeapon();

            // 2. Hủy bỏ "bóng ma" tàng hình đang cầm trên tay
            Destroy(draggedItem.gameObject);

            // 3. Vẽ lại kho đồ (Cả Vũ khí và Ngọc bị rớt ra sẽ cùng xuất hiện lại)
            uiManager.RefreshInventoryUI();
            return;
        }
    }
}