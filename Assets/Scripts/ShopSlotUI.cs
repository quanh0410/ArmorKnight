using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI priceText;
    private Button slotButton;
    private ShopItem myItemData;
    public TextMeshProUGUI quantityText;

    private void Awake()
    {
        slotButton = GetComponent<Button>();
    }

    // Hàm này được ShopUIManager gọi để bơm dữ liệu vào
    public void SetupSlot(ShopItem item)
    {
        myItemData = item;
        itemIcon.sprite = item.itemData.itemIcon;
        priceText.text = item.price.ToString();

        if (quantityText != null)
        {
            quantityText.text = item.quantity > 1 ? "x" + item.quantity.ToString() : "";
        }

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        if (ShopUIManager.instance != null)
        {
            ShopUIManager.instance.SelectItem(myItemData);
        }
    }
}