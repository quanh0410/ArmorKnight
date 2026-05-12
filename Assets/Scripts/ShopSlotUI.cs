using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI priceText;

    // Nút bấm bao quanh cả ô vật phẩm
    private Button slotButton;
    private ShopItem myItemData;

    // --- MỚI: Thêm ô Text hiển thị số lượng ---
    public TextMeshProUGUI quantityText;

    private void Awake()
    {
        // Lấy component Button gắn trên chính Object này
        slotButton = GetComponent<Button>();
    }

    // Hàm này được ShopUIManager gọi để bơm dữ liệu vào
    public void SetupSlot(ShopItem item)
    {
        myItemData = item;
        itemIcon.sprite = item.itemData.itemIcon;
        priceText.text = item.price.ToString();

        // --- MỚI: Nếu số lượng > 1 thì hiện chữ "x5", nếu bán 1 cái thì ẩn đi cho gọn ---
        if (quantityText != null)
        {
            quantityText.text = item.quantity > 1 ? "x" + item.quantity.ToString() : "";
        }

        // Xóa sự kiện cũ và gắn sự kiện mới: Khi bấm vào ô này thì báo cho Manager biết
        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        // Gửi dữ liệu của món đồ này sang khung bên phải
        if (ShopUIManager.instance != null)
        {
            ShopUIManager.instance.SelectItem(myItemData);
        }
    }
}