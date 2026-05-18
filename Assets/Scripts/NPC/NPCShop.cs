using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ShopItem
{
    [Header("--- LƯU TRỮ ---")]
    public string uniqueBuyID;

    [Header("--- THÔNG TIN ---")]
    public ItemData itemData;
    public int price;
    public int quantity = 1;
}

public class NPCShop : MonoBehaviour
{
    [Header("Danh sách hàng hóa")]
    public List<ShopItem> itemsForSale;

    // --- MỚI: Đổi thành hàm Public để NPCDialog gọi qua sự kiện ---
    public void OpenShopWindow()
    {
        if (ShopUIManager.instance != null)
        {
            List<ShopItem> availableItems = new List<ShopItem>();

            foreach (ShopItem item in itemsForSale)
            {
                if (SaveManager.instance != null && !SaveManager.instance.IsObjectInteracted(item.uniqueBuyID))
                {
                    availableItems.Add(item);
                }
            }

            // Gọi mở giao diện UI Shop
            ShopUIManager.instance.OpenShop(availableItems);

            // Tạm ẩn chữ "[S] Nói chuyện" đi khi đang mua đồ
            if (InteractionUI.instance != null) InteractionUI.instance.Hide();
        }
    }

    // Vẫn giữ OnTriggerExit để tự động đóng shop khi người chơi bỏ đi xa
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (ShopUIManager.instance != null) ShopUIManager.instance.CloseShop();
        }
    }
}