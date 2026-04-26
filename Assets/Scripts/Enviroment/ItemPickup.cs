using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Định danh (Bắt buộc cho đồ rớt trên map)")]
    public string itemID;

    public ItemData itemInfo;
    private bool canPickup = false;

    private void Start()
    {
        // Vừa vào map, hỏi SaveManager xem đồ này đã bị nhặt ở quá khứ chưa?
        if (SaveManager.instance != null && SaveManager.instance.IsObjectInteracted(itemID))
        {
            Destroy(gameObject); // Đã nhặt rồi thì hủy luôn
        }
    }

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
            InteractionUI.instance.Show(transform, "[S] Nhặt " + itemInfo.itemName);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canPickup = false;
            InteractionUI.instance.Hide();
        }
    }

    private void PickUpItem()
    {
        InventoryManager.instance.AddItem(itemInfo);
        InteractionUI.instance.Hide();

        // Ghi vào sổ: Vật phẩm nhặt xong là mất vĩnh viễn (true)
        if (SaveManager.instance != null && !string.IsNullOrEmpty(itemID))
        {
            SaveManager.instance.SaveObjectState(itemID, true);
        }

        Destroy(gameObject);
    }
}