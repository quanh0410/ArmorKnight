using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Định danh (Bắt buộc cho đồ rớt trên map)")]
    public string itemID;

    public ItemData itemInfo;
    private bool canPickup = false;

    private void Start()
    {
        if (SaveManager.instance != null && SaveManager.instance.IsObjectInteracted(itemID))
        {
            Destroy(gameObject); 
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
        AudioManager.instance.PlaySFX("ItemPickup");

        if (SaveManager.instance != null && !string.IsNullOrEmpty(itemID))
        {
            SaveManager.instance.SaveObjectState(itemID, true);
        }

        Destroy(gameObject);
    }
}