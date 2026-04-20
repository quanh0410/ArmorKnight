using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Bắt buộc phải có thư viện này

// Khai báo 3 Interface Kéo, Đang Kéo, và Thả
public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public ItemData itemData;

    [Header("Thành phần UI")]
    public Image iconImage;
    public Image borderImage;

    [HideInInspector] public Transform originalParent; // Nhớ nhà để quay về nếu thả trượt
    private CanvasGroup canvasGroup; // Dùng để làm xuyên thấu UI

    private void Awake()
    {
        // Tự động thêm CanvasGroup nếu bạn quên gắn ngoài Unity
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetupItem(ItemData data)
    {
        itemData = data;

        // Cài đặt Icon vũ khí / ngọc
        if (iconImage != null) iconImage.sprite = data.itemIcon;

        // --- CÀI ĐẶT VIỀN (BORDER) ---
        if (borderImage != null)
        {
            // BẮT BUỘC: Reset màu nền về trắng. Nếu để màu đỏ, ảnh viền xám đắp lên sẽ biến thành màu đỏ xám!
            borderImage.color = Color.white;

            // Nếu là Vũ Khí -> Lấy viền xám
            if (data.equipType == EquipmentType.Weapon)
            {
                borderImage.sprite = InventoryUIManager.instance.weaponGrayBorder;
            }
            // Nếu là Ngọc/Kỹ năng -> Lấy viền theo màu
            else
            {
                switch (data.itemColor)
                {
                    case ItemColor.Red:
                        borderImage.sprite = InventoryUIManager.instance.redGemBorder;
                        break;
                    case ItemColor.Blue:
                        borderImage.sprite = InventoryUIManager.instance.blueGemBorder;
                        break;
                    case ItemColor.Yellow:
                        borderImage.sprite = InventoryUIManager.instance.yellowGemBorder;
                        break;
                }
            }
        }
    }

    // 1. KHI VỪA NHẤP CHUỘT VÀ KÉO
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;

        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;

        // Hiện nguyên hình icon
        if (iconImage != null) iconImage.enabled = true;
        if (borderImage != null) borderImage.enabled = true;

        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(100, 100); // Kích thước icon khi cầm trên chuột

        // --- BỔ SUNG: TẠM ẨN THANH KIẾM TO NẾU ĐANG NHẤC RA TỪ Ô TRANG BỊ ---
        if (originalParent.GetComponent<EquipmentSlotUI>() != null)
        {
            InventoryUIManager uiManager = FindObjectOfType<InventoryUIManager>();
            if (uiManager != null && uiManager.weaponImage != null)
            {
                uiManager.weaponImage.enabled = false;
            }
        }
    }

    // 2. TRONG LÚC ĐANG KÉO (Cập nhật vị trí liên tục)
    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition; // Đi theo con trỏ chuột
    }

    // 3. KHI BUÔNG CHUỘT RA
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == transform.root)
        {
            transform.SetParent(originalParent);

            EquipmentSlotUI equipSlot = originalParent.GetComponent<EquipmentSlotUI>();
            if (equipSlot != null)
            {
                // Biến lại thành bóng ma tàng hình
                RectTransform rect = GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;

                if (iconImage != null) iconImage.enabled = false;
                if (borderImage != null) borderImage.enabled = false;

                // --- BỔ SUNG: BẬT LẠI THANH KIẾM TO VÌ BẠN ĐÃ THẢ TRƯỢT ---
                InventoryUIManager uiManager = FindObjectOfType<InventoryUIManager>();
                if (uiManager != null && uiManager.weaponImage != null)
                {
                    uiManager.weaponImage.enabled = true;
                }
            }
            else
            {
                // Trả về kho đồ
                GetComponent<RectTransform>().localPosition = Vector3.zero;
            }
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // Chỉ hiện thông tin khi người chơi bấm Chuột Trái
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Vì ở bài trước ta đã tạo biến instance (Singleton) cho InventoryUIManager
            // Nên giờ ta có thể gọi thẳng hàm ShowItemInfo cực kỳ dễ dàng!
            InventoryUIManager.instance.ShowItemInfo(itemData);

            Debug.Log("Đã xem thông tin: " + itemData.itemName);
        }
    }
}