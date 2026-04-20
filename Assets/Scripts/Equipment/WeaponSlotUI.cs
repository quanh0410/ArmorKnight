using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Khai báo Interface IDropHandler để nhận vật bị thả vào
public class WeaponSlotUI : MonoBehaviour, IDropHandler
{
    public ItemColor slotColor; // Màu của ô này
    public int slotIndex;       // Vị trí ô (0, 1, 2...)

    // Hàm này tự động chạy khi có 1 vật thể UI bị THẢ CHUỘT ngay trên đầu nó
    public void OnDrop(PointerEventData eventData)
    {
        // 1. Kiểm tra xem thứ vừa bị thả xuống có phải là InventoryItem không?
        GameObject droppedObj = eventData.pointerDrag;
        InventoryItemUI draggedItem = droppedObj.GetComponent<InventoryItemUI>();

        if (draggedItem != null)
        {

            // 2. CHECK MÀU: Màu của Item có khớp với màu của Ô không?
            if (draggedItem.itemData.itemColor == slotColor)
            {
                // Gọi tới não bộ để lưu dữ liệu
                bool success = EquipmentManager.instance.TryEquipItem(draggedItem.itemData, slotIndex);

                if (success)
                {
                    // Lắp thành công! Gắn Item vào làm con của Ô này
                    draggedItem.transform.SetParent(transform);

                    // --- SỬA ĐOẠN NÀY: Ép vào chính giữa và reset kích thước ---
                    RectTransform rect = draggedItem.GetComponent<RectTransform>();
                    rect.localPosition = Vector3.zero;
                    rect.localScale = Vector3.one;

                    Debug.Log("Lắp cái cạch! Vừa khít!");
                }
                else
                {
                    // Nếu EquipmentManager từ chối (ví dụ đã lắp item khác rồi)
                    ReturnToInventory(draggedItem);
                }
            }
            else
            {
                Debug.Log($"Sai màu! Ô này cần màu {slotColor} nhưng bạn lại nhét màu {draggedItem.itemData.itemColor}");
                ReturnToInventory(draggedItem);
            }
        }
    }

    // Hàm đuổi Item về chỗ cũ nếu lắp sai
    private void ReturnToInventory(InventoryItemUI item)
    {
        item.transform.SetParent(item.originalParent);
    }
}