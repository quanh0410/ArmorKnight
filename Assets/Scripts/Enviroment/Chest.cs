using UnityEngine;
using System.Collections.Generic;

public class ChestController : MonoBehaviour
{
    [Header("Yêu cầu")]
    public ItemData requiredKey;
    public int keysNeeded = 1;

    [Header("Vật phẩm bên trong")]
    public List<ItemData> itemsInside;
    public GameObject itemPickupPrefab;

    [Header("Cài đặt hiệu ứng")]
    public float popForce = 5f;

    private bool isPlayerInside = false;
    private bool isOpened = false;

    private void Update()
    {
        if (isPlayerInside && !isOpened && Input.GetKeyDown(KeyCode.S))
        {
            CheckKeyAndOpen();
        }
    }

    private void CheckKeyAndOpen()
    {
        int keyCount = InventoryManager.instance.GetItemCount(requiredKey);

        if (keyCount >= keysNeeded)
        {
            OpenChest();
        }
        else
        {
            InteractionUI.instance.Show(transform, "<color=red>Thiếu chìa khóa!</color>");
        }
    }

    private void OpenChest()
    {
        isOpened = true;
        GetComponent<Animator>().SetTrigger("Open");
        // 1. Tiêu thụ chìa khóa
        for (int i = 0; i < keysNeeded; i++)
        {
            InventoryManager.instance.RemoveItem(requiredKey);
        }

        // 2. Ẩn UI ngay lập tức
        InteractionUI.instance.Hide();

        // 3. Bung vật phẩm ra ngoài
        foreach (ItemData data in itemsInside)
        {
            PopOutItem(data);
        }

        Debug.Log("Rương đã mở!");
    }

    private void PopOutItem(ItemData data)
    {
        GameObject newItem = Instantiate(itemPickupPrefab, transform.position, Quaternion.identity);
        ItemPickup pickup = newItem.GetComponent<ItemPickup>();
        if (pickup != null) pickup.itemInfo = data;

        Rigidbody2D rb = newItem.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), 1.5f).normalized;
            rb.AddForce(randomDirection * popForce, ForceMode2D.Impulse);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isOpened)
        {
            isPlayerInside = true;

            // --- GỌI UI DÙNG CHUNG ---
            int currentKeys = InventoryManager.instance.GetItemCount(requiredKey);
            string message = $"[S] Mở Rương ({currentKeys}/{keysNeeded})";
            InteractionUI.instance.Show(transform, message);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;

            // --- ẨN UI DÙNG CHUNG ---
            InteractionUI.instance.Hide();
        }
    }
}