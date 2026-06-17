using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager instance;

    [Header("--- KHUNG BÊN TRÁI (DANH SÁCH) ---")]
    public GameObject shopPanel;
    public Transform contentContainer;
    public GameObject shopSlotPrefab;

    [Header("--- KHUNG BÊN PHẢI (CHI TIẾT) ---")]
    public TextMeshProUGUI descriptionText;
    public Button mainBuyButton;

    [Header("--- BẢNG XÁC NHẬN MUA ---")]
    public GameObject confirmPopupPanel;
    public TextMeshProUGUI confirmMessageText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private ShopItem currentSelectedItem;
    public Button closeShopButton;
    private GameObject currentSelectedSlot; // Lưu cái ô UI đang chọn
    private List<ShopItem> currentShopSource; // Lưu danh sách đồ của NPC đang mở

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        CloseShop(); // Đảm bảo UI luôn tắt khi mới vào game

        if (closeShopButton != null)
        {
            closeShopButton.onClick.AddListener(CloseShop);
        }

        mainBuyButton.onClick.AddListener(ShowConfirmPopup);
        confirmYesButton.onClick.AddListener(ConfirmPurchase);
        confirmNoButton.onClick.AddListener(CancelPurchase);
    }


    public void OpenShop(List<ShopItem> items)
    {
        currentShopSource = items;
        shopPanel.SetActive(true);
        confirmPopupPanel.SetActive(false);
        ClearSelection();

        if (InteractionUI.instance != null) InteractionUI.instance.Hide();

        if (UIManager.instance != null) UIManager.instance.ForceShowCoinUI(true);
        Time.timeScale = 0f;

        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (ShopItem shopItem in items)
        {
            GameObject newSlot = Instantiate(shopSlotPrefab, contentContainer);
            ShopSlotUI slotUI = newSlot.GetComponent<ShopSlotUI>();
            if (slotUI != null)
            {
                slotUI.SetupSlot(shopItem);
                Button btn = newSlot.GetComponent<Button>();
                btn.onClick.AddListener(() => { currentSelectedSlot = newSlot; });
            }
        }
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        confirmPopupPanel.SetActive(false);
        Time.timeScale = 1f;

        if (UIManager.instance != null) UIManager.instance.ForceShowCoinUI(false);
    }

    public void SelectItem(ShopItem item)
    {
        currentSelectedItem = item;
        descriptionText.text = $"<b>{item.itemData.itemName} x{item.quantity}</b>\n\n{item.itemData.itemDescription}";
        mainBuyButton.interactable = true;
    }

    private void ClearSelection()
    {
        currentSelectedItem = null;
        descriptionText.text = "Hãy chọn một món đồ để xem chi tiết.";
        mainBuyButton.interactable = false;
    }

    private void ShowConfirmPopup()
    {
        if (currentSelectedItem == null) return;

        confirmMessageText.text = $"Bạn có chắc chắn muốn mua\n<b>{currentSelectedItem.itemData.itemName} x{currentSelectedItem.quantity}</b>\nvới giá <color=#FFD700>{currentSelectedItem.price} Vàng</color> không?";
        confirmPopupPanel.SetActive(true);
    }

    private void CancelPurchase()
    {
        confirmPopupPanel.SetActive(false);
    }

    private void ConfirmPurchase()
    {
        if (currentSelectedItem == null) return;

        if (CoinManager.Instance.SpendCoins(currentSelectedItem.price))
        {
            InventoryManager.instance.AddItem(currentSelectedItem.itemData, currentSelectedItem.quantity);

            if (currentShopSource != null)
            {
                currentShopSource.Remove(currentSelectedItem);
            }

            if (currentSelectedSlot != null)
            {
                Destroy(currentSelectedSlot);
            }

            // 4. Lưu vào SaveManager để không bao giờ hiện lại nữa (NẾU CÓ ID)
            if (SaveManager.instance != null && !string.IsNullOrEmpty(currentSelectedItem.uniqueBuyID))
            {
                SaveManager.instance.SaveObjectState(currentSelectedItem.uniqueBuyID);
                SaveManager.instance.SaveGame();
            }

            // 5. Reset giao diện
            ClearSelection();
            confirmPopupPanel.SetActive(false);
        }
        else
        {
            confirmMessageText.text = "<color=red>Giao dịch thất bại!</color>\nBạn không có đủ tiền.";
            confirmYesButton.gameObject.SetActive(false);

            StartCoroutine(ResetConfirmButtonRoutine());
        }
    }

    private System.Collections.IEnumerator ResetConfirmButtonRoutine()
    {
        yield return new WaitForSecondsRealtime(2f);
        confirmYesButton.gameObject.SetActive(true);
        confirmPopupPanel.SetActive(false);
    }
}