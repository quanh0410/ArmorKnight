using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData itemInfo;
    private bool canPickup = false;

    private void Update()
    {
        if (canPickup && Input.GetKeyDown(KeyCode.S))
        {
            PickUpItem();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canPickup = true;

            // --- SỬA Ở ĐÂY: Gọi UI dùng chung hiện lên ---
            // Nối thêm tên vật phẩm cho chuyên nghiệp: "[S] Nhặt Kiếm Sắt"
            InteractionUI.instance.Show(transform, "[S] Nhặt " + itemInfo.itemName);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canPickup = false;

            // --- SỬA Ở ĐÂY: Gọi UI dùng chung ẩn đi ---
            InteractionUI.instance.Hide();
        }
    }

    private void PickUpItem()
    {
        InventoryManager.instance.AddItem(itemInfo);

        // Ẩn UI đi trước khi xóa vật phẩm (để tránh lỗi chữ bị kẹt lại)
        InteractionUI.instance.Hide();

        Destroy(gameObject);
    }
}