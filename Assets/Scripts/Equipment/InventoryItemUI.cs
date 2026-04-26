using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public ItemData itemData;

    [Header("Thành phần UI")]
    public Image iconImage;
    public Image borderImage;
    public TextMeshProUGUI quantityText;

    [HideInInspector] public Transform originalParent;
    [HideInInspector] public bool isDropped = false; // --- CỜ AN TOÀN MỚI ---

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetupItem(ItemData data, int amount = 1)
    {
        itemData = data;
        if (iconImage != null) iconImage.sprite = data.itemIcon;

        if (borderImage != null)
        {
            borderImage.color = Color.white;
            if (data is EquipmentData equipData)
            {
                if (equipData.equipType == EquipmentType.Weapon) borderImage.sprite = InventoryUIManager.instance.weaponGrayBorder;
                else
                {
                    switch (equipData.itemColor)
                    {
                        case ItemColor.Red: borderImage.sprite = InventoryUIManager.instance.redGemBorder; break;
                        case ItemColor.Blue: borderImage.sprite = InventoryUIManager.instance.blueGemBorder; break;
                        case ItemColor.Yellow: borderImage.sprite = InventoryUIManager.instance.yellowGemBorder; break;
                    }
                }
            }
            else borderImage.sprite = InventoryUIManager.instance.noneBorder;
        }

        if (quantityText != null)
        {
            if (amount > 1) { quantityText.text = "x" + amount.ToString(); quantityText.gameObject.SetActive(true); }
            else quantityText.gameObject.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!(itemData is EquipmentData)) return;

        isDropped = false;
        originalParent = transform.parent;

        // --- ĐÃ SỬA: Ép nó nằm trong Canvas, tuyệt đối không dùng transform.root ---
        Canvas mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas != null)
        {
            transform.SetParent(mainCanvas.transform);
        }
        else
        {
            // Dự phòng nếu không tìm thấy Canvas
            transform.SetParent(InventoryUIManager.instance.mainInventoryPanel.transform);
        }

        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;

        if (iconImage != null) iconImage.enabled = true;
        if (borderImage != null) borderImage.enabled = true;

        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(100, 100);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!(itemData is EquipmentData)) return;
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!(itemData is EquipmentData equipData)) return;
        if (isDropped) return; // Đã thả trúng ô Ngọc thì dừng lại

        canvasGroup.blocksRaycasts = true;

        // KIỂM TRA: Có phải đang kéo đồ TỪ TRÊN NGƯỜI ném ra ngoài không?
        bool isUnequipping = false;

        if (EquipmentManager.instance.currentWeapon != null)
        {
            // 1. Kiểm tra tháo Vũ Khí
            if (equipData.weaponStats != null && equipData.weaponStats == EquipmentManager.instance.currentWeapon)
            {
                EquipmentManager.instance.UnequipWeapon();
                isUnequipping = true;
            }
            // 2. Kiểm tra tháo Ngọc
            else
            {
                foreach (var slot in EquipmentManager.instance.currentWeapon.slots)
                {
                    if (slot.isOccupied && slot.equippedItem == equipData)
                    {
                        slot.equippedItem = null;
                        slot.isOccupied = false;
                        isUnequipping = true;
                        break;
                    }
                }
            }
        }

        // --- ĐÃ SỬA CHUẨN: HỦY LUÔN ICON NÀY VÀ VẼ LẠI TOÀN BỘ UI ---
        // Bất kể là bạn tháo đồ thành công hay ném trượt, việc hủy Icon đang kéo
        // và gọi Refresh sẽ đảm bảo UI luôn đồng bộ 100% với Dữ liệu gốc trong túi đồ.
        Destroy(gameObject);
        InventoryUIManager.instance.DelayedRefresh();

        if (isUnequipping)
        {
            Debug.Log("<color=yellow>Đã tháo đồ về túi thành công!</color>");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) InventoryUIManager.instance.ShowItemInfo(itemData);
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            bool isConsumed = itemData.UseItem();
            if (isConsumed) { InventoryManager.instance.RemoveItem(itemData); InventoryUIManager.instance.RefreshInventoryUI(); }
        }
    }
}