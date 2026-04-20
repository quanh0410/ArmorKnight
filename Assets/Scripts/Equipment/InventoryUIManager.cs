using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Bảng Giao Diện Chính")]
    public GameObject mainInventoryPanel;
    private bool isInventoryOpen = false;

    [Header("Các hàng chứa Item (Bên phải)")]
    public Transform grayItemRow;
    public Transform redItemRow;
    public Transform blueItemRow;
    public Transform yellowItemRow;

    [Header("Prefab")]
    public GameObject inventoryItemPrefab;

    [Header("Panel Thông tin (Đang ẩn)")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoNameText;
    public TextMeshProUGUI infoMechanicText;

    [Header("Khu vực Vũ khí (Bên trái)")]
    public Image weaponImage;
    public Transform weaponSlotsContainer;
    public GameObject slotColorIndicatorPrefab;

    public static InventoryUIManager instance;

    [Header("Kho Hình Ảnh Viền (Borders)")]
    public Sprite weaponGrayBorder; // Viền xám cho Vũ khí
    public Sprite redGemBorder;     // Viền cho Ngọc Đỏ
    public Sprite blueGemBorder;    // Viền cho Ngọc Xanh
    public Sprite yellowGemBorder;  // Viền cho Ngọc Vàng

    private bool justOpenedInfo = false;
    private void Awake()
    {
        if (instance == null) instance = this;
    }
    // --- ĐĂNG KÝ LẮNG NGHE SỰ KIỆN KHI BẬT/TẮT SCRIPT ---
    private void OnEnable()
    {
        EquipmentManager.OnWeaponChanged += UpdateWeaponView;
    }
    private void OnDisable()
    {
        EquipmentManager.OnWeaponChanged -= UpdateWeaponView;
    }

    private void Start()
    {
        if (mainInventoryPanel != null) mainInventoryPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        isInventoryOpen = false;
    }


    private void Update()
    {
        // Code bật/tắt túi đồ cũ giữ nguyên
        if (Input.GetKeyDown(KeyCode.E) || (Input.GetKeyDown(KeyCode.Escape) && isInventoryOpen))
        {
            ToggleInventory();
        }

        // --- BỔ SUNG LỆNH QUÉT CLICK CHUỘT ---
        if (isInventoryOpen && infoPanel != null && infoPanel.activeSelf)
        {
            // Nếu người chơi bấm chuột trái
            if (Input.GetMouseButtonDown(0))
            {
                // Nếu bảng vừa mới được mở ở ngay frame này, bỏ qua không xét tắt để tránh xung đột
                if (justOpenedInfo)
                {
                    justOpenedInfo = false;
                    return;
                }

                // Đo tọa độ khung Info Panel
                RectTransform panelRect = infoPanel.GetComponent<RectTransform>();

                // Nếu chuột KHÔNG nằm trong khung Info Panel -> Tắt nó đi
                if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition))
                {
                    infoPanel.SetActive(false);
                }
            }
        }
    }

    private void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        mainInventoryPanel.SetActive(isInventoryOpen);

        if (isInventoryOpen)
        {
            Time.timeScale = 0f;
            RefreshInventoryUI();

            // Ép vẽ lại vũ khí hiện tại (nếu có)
            UpdateWeaponView(EquipmentManager.instance.currentWeapon);
        }
        else
        {
            Time.timeScale = 1f;
            infoPanel.SetActive(false);
        }
    }

    public void RefreshInventoryUI()
    {
        ClearRow(redItemRow);
        ClearRow(blueItemRow);
        ClearRow(yellowItemRow);
        ClearRow(grayItemRow);

        if (InventoryManager.instance == null) return;

        foreach (ItemData item in InventoryManager.instance.items)
        {
            if (IsItemEquipped(item)) continue;

            Transform targetRow = GetRowForItem(item); // Dùng hàm phụ cho gọn
            if (targetRow != null)
            {
                GameObject newItemObj = Instantiate(inventoryItemPrefab, targetRow);
                newItemObj.GetComponent<InventoryItemUI>().SetupItem(item);
            }
        }
    }

    private void ClearRow(Transform row)
    {
        foreach (Transform child in row) Destroy(child.gameObject);
    }

    // --- HÀM VẼ VŨ KHÍ TỰ ĐỘNG ĐƯỢC GỌI BỞI SỰ KIỆN ---
    public void UpdateWeaponView(WeaponData weapon)
    {
        // 1. Luôn xóa sạch các Object cũ trong container
        ClearRow(weaponSlotsContainer);

        // TRƯỜNG HỢP 1: KHÔNG CÓ VŨ KHÍ
        if (weapon == null)
        {
            if (weaponImage != null)
            {
                weaponImage.sprite = null;
                weaponImage.enabled = false;
            }

            // --- BỔ SUNG: Ẩn luôn cái container để chắc chắn không hiện gì ---
            if (weaponSlotsContainer != null) weaponSlotsContainer.gameObject.SetActive(false);

            return;
        }

        // TRƯỜNG HỢP 2: CÓ VŨ KHÍ
        // TRƯỜNG HỢP 2: CÓ VŨ KHÍ
        if (weaponSlotsContainer != null) weaponSlotsContainer.gameObject.SetActive(true); // Hiện lại

        // --- BẮT ĐẦU SỬA TỪ ĐÂY ---
        // 1. Tìm lớp vỏ ItemData đang chứa cái lõi WeaponData này
        ItemData equippedItemData = null;
        if (InventoryManager.instance != null)
        {
            foreach (ItemData item in InventoryManager.instance.items)
            {
                if (item.weaponStats == weapon)
                {
                    equippedItemData = item;
                    break;
                }
            }
        }

        // 2. Lấy hình ảnh từ lớp vỏ ItemData đó để vẽ lên UI
        if (weaponImage != null && equippedItemData != null && equippedItemData.itemIcon != null)
        {
            weaponImage.enabled = true;
            weaponImage.sprite = equippedItemData.itemIcon;
        }
        // --- KẾT THÚC ĐOẠN SỬA ---

        foreach (WeaponSlot slot in weapon.slots)
        {
            GameObject indicator = Instantiate(slotColorIndicatorPrefab, weaponSlotsContainer);
            Image indicatorImage = indicator.GetComponent<Image>();
            WeaponSlotUI slotScript = indicator.GetComponent<WeaponSlotUI>();

            slotScript.slotColor = slot.allowedColor;
            slotScript.slotIndex = weapon.slots.IndexOf(slot);

            // --- SỬA ĐOẠN NÀY: Dùng ảnh Sprite thay vì đổi màu Color ---
            indicatorImage.color = Color.white; // Reset về màu trắng tinh để ảnh không bị ám màu

            if (slot.allowedColor == ItemColor.Red) indicatorImage.sprite = redGemBorder;
            else if (slot.allowedColor == ItemColor.Blue) indicatorImage.sprite = blueGemBorder;
            else if (slot.allowedColor == ItemColor.Yellow) indicatorImage.sprite = yellowGemBorder;

            if (slot.equippedItem != null)
            {
                GameObject equippedItemObj = Instantiate(inventoryItemPrefab, indicator.transform);
                equippedItemObj.GetComponent<InventoryItemUI>().SetupItem(slot.equippedItem);
            }
        }
    }

    // Hàm phụ: Giúp tìm dòng cho Item
    public Transform GetRowForItem(ItemData item)
    {
        if (item.equipType == EquipmentType.Weapon) return grayItemRow; // Tạm xếp Vũ khí vào hàng xám, hoặc bạn tạo hàng riêng
        if (item.itemColor == ItemColor.Red) return redItemRow;
        if (item.itemColor == ItemColor.Blue) return blueItemRow;
        if (item.itemColor == ItemColor.Yellow) return yellowItemRow;
        return null;
    }

    private bool IsItemEquipped(ItemData item)
    {
        // Nếu item chính là vũ khí đang cầm -> Tính là đã mặc
        if (EquipmentManager.instance.currentWeapon != null && item.weaponStats == EquipmentManager.instance.currentWeapon) return true;

        if (EquipmentManager.instance.currentWeapon == null) return false;
        foreach (WeaponSlot slot in EquipmentManager.instance.currentWeapon.slots)
        {
            if (slot.equippedItem == item) return true;
        }
        return false;
    }

    // --- BỔ SUNG: HÀM HIỂN THỊ BẢNG THÔNG TIN ---
    public void ShowItemInfo(ItemData item)
    {
        if (infoPanel == null) return;

        infoPanel.SetActive(true);
        justOpenedInfo = true;


        // 1. Lấy Tên từ ItemData
        if (infoNameText != null) infoNameText.text = item.itemName;

        // 2. Lấy Mô tả từ ItemData
        if (infoMechanicText != null)
        {
            string desc = item.itemDescription;

            //// Nếu là Ngọc có kèm kỹ năng thì nối thêm dòng chữ Kỹ năng vào dưới mô tả
            //if (!string.IsNullOrEmpty(item.mechanicToUnlock))
            //{
            //    desc += "\n\n<color=yellow>Kỹ năng: " + item.mechanicToUnlock + "</color>";
            //}

            infoMechanicText.text = desc;
        }
    }
}