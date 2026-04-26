using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Cần thêm cái này ở trên cùng
public enum InventoryTab { Equipment, Material }
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

    [Header("Panel Thông tin")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoNameText;
    public TextMeshProUGUI infoMechanicText;

    [Header("Khu vực Vũ khí (ĐÃ SỬA CHUẨN)")]
    // Đã thay Image thành Transform để sinh ra Prefab vũ khí
    public Transform weaponSlotContainer;
    public Transform weaponSlotsContainer;
    public GameObject slotColorIndicatorPrefab;

    public static InventoryUIManager instance;

    [Header("Kho Hình Ảnh Viền (Borders)")]
    public Sprite noneBorder;
    public Sprite weaponGrayBorder;
    public Sprite redGemBorder;
    public Sprite blueGemBorder;
    public Sprite yellowGemBorder;

    [Header("Hệ thống Side Tabs")]
    public InventoryTab currentTab = InventoryTab.Equipment;

    public GameObject equipmentPage;
    public GameObject materialPage;
    public Transform materialGridContainer;

    private bool justOpenedInfo = false;

    private void Awake() { if (instance == null) instance = this; }
    private void OnEnable() { EquipmentManager.OnWeaponChanged += UpdateWeaponView; }
    private void OnDisable() { EquipmentManager.OnWeaponChanged -= UpdateWeaponView; }

    private void Start()
    {
        if (mainInventoryPanel != null) mainInventoryPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
        isInventoryOpen = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) || (Input.GetKeyDown(KeyCode.Escape) && isInventoryOpen))
        {
            // Lấy trạng thái của Player
            PlayerController pc = FindObjectOfType<PlayerController>();

            // NẾU ĐANG CHƠI (CHƯA MỞ UI) MÀ ĐÒI MỞ KHI CHƯA NGỒI -> TỪ CHỐI
            if (!isInventoryOpen && pc != null && !pc.isResting)
            {
                Debug.Log("Chỉ được mở túi đồ khi đang ngồi nghỉ ngơi!");
                return; // Ngắt ngang, không cho mở
            }

            ToggleInventory();
        }

        if (isInventoryOpen && infoPanel != null && infoPanel.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (justOpenedInfo) { justOpenedInfo = false; return; }
                RectTransform panelRect = infoPanel.GetComponent<RectTransform>();
                if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition))
                    infoPanel.SetActive(false);
            }
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        mainInventoryPanel.SetActive(isInventoryOpen);

        if (isInventoryOpen)
        {
            Time.timeScale = 0f;
            RefreshInventoryUI();
            UpdateWeaponView(EquipmentManager.instance.currentWeapon);
        }
        else
        {
            Time.timeScale = 1f;
            infoPanel.SetActive(false);
        }
    }

    public void SwitchTab(int tabIndex)
    {
        currentTab = (InventoryTab)tabIndex;
        if (equipmentPage != null) equipmentPage.SetActive(currentTab == InventoryTab.Equipment);
        if (materialPage != null) materialPage.SetActive(currentTab == InventoryTab.Material);
        if (infoPanel != null) infoPanel.SetActive(false);
        RefreshInventoryUI();
    }

    public void RefreshInventoryUI()
    {
        ClearRow(redItemRow); ClearRow(blueItemRow); ClearRow(yellowItemRow); ClearRow(grayItemRow);
        if (materialGridContainer != null) ClearRow(materialGridContainer);

        if (InventoryManager.instance == null) return;

        List<ItemData> drawnMaterials = new List<ItemData>();

        foreach (ItemData item in InventoryManager.instance.items)
        {
            // Nếu đồ đã mặc (Vũ khí hoặc Ngọc), bỏ qua không vẽ vào Kho đồ nữa!
            if (IsItemEquipped(item)) continue;

            if (currentTab == InventoryTab.Equipment)
            {
                if (item is EquipmentData)
                {
                    Transform targetRow = GetRowForItem(item);
                    if (targetRow != null)
                    {
                        GameObject newItemObj = Instantiate(inventoryItemPrefab, targetRow);
                        newItemObj.GetComponent<InventoryItemUI>().SetupItem(item);
                    }
                }
            }
            else if (currentTab == InventoryTab.Material)
            {
                if (!(item is EquipmentData))
                {
                    if (drawnMaterials.Contains(item)) continue;
                    drawnMaterials.Add(item);
                    int count = InventoryManager.instance.GetItemCount(item);
                    if (materialGridContainer != null)
                    {
                        GameObject newItemObj = Instantiate(inventoryItemPrefab, materialGridContainer);
                        newItemObj.GetComponent<InventoryItemUI>().SetupItem(item, count);
                    }
                }
            }
        }
    }

    private void ClearRow(Transform row)
    {
        if (row == null) return;
        foreach (Transform child in row)
        {
            // Tắt hiển thị ngay lập tức để ngắt kết nối khỏi hệ thống Layout
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public void UpdateWeaponView(WeaponData weapon)
    {
        ClearRow(weaponSlotContainer);
        ClearRow(weaponSlotsContainer);

        if (weapon == null)
        {
            if (weaponSlotsContainer != null) weaponSlotsContainer.gameObject.SetActive(false);
            return;
        }

        if (weaponSlotsContainer != null) weaponSlotsContainer.gameObject.SetActive(true);

        ItemData equippedItemData = null;
        if (InventoryManager.instance != null)
        {
            foreach (ItemData item in InventoryManager.instance.items)
            {
                if (item is EquipmentData equipData && equipData.weaponStats == weapon)
                {
                    equippedItemData = item;
                    break;
                }
            }
        }

        // TẠO PREFAB VŨ KHÍ TẠI ĐÂY
        if (equippedItemData != null && weaponSlotContainer != null)
        {
            GameObject weaponObj = Instantiate(inventoryItemPrefab, weaponSlotContainer);
            weaponObj.GetComponent<InventoryItemUI>().SetupItem(equippedItemData);
        }

        // THAY VÒNG LẶP FOREACH BẰNG VÒNG LẶP FOR NÀY
        for (int i = 0; i < weapon.slots.Count; i++)
        {
            WeaponSlot slot = weapon.slots[i];
            GameObject indicator = Instantiate(slotColorIndicatorPrefab, weaponSlotsContainer);
            Image indicatorImage = indicator.GetComponent<Image>();
            WeaponSlotUI slotScript = indicator.GetComponent<WeaponSlotUI>();

            slotScript.slotColor = slot.allowedColor;

            // Gán chết vị trí Index, tuyệt đối không bao giờ sai lệch
            slotScript.slotIndex = i;
            indicatorImage.color = Color.white;

            if (slot.allowedColor == ItemColor.Red) indicatorImage.sprite = redGemBorder;
            else if (slot.allowedColor == ItemColor.Blue) indicatorImage.sprite = blueGemBorder;
            else if (slot.allowedColor == ItemColor.Yellow) indicatorImage.sprite = yellowGemBorder;

            if (slot.isOccupied && slot.equippedItem != null)
            {
                GameObject equippedItemObj = Instantiate(inventoryItemPrefab, indicator.transform);
                equippedItemObj.GetComponent<InventoryItemUI>().SetupItem(slot.equippedItem);
            }
        }
    }

    public Transform GetRowForItem(ItemData item)
    {
        if (item is EquipmentData equipData)
        {
            if (equipData.equipType == EquipmentType.Weapon) return grayItemRow;
            if (equipData.itemColor == ItemColor.Red) return redItemRow;
            if (equipData.itemColor == ItemColor.Blue) return blueItemRow;
            if (equipData.itemColor == ItemColor.Yellow) return yellowItemRow;
        }
        return grayItemRow;
    }

    // FIX LỖI NHÂN BẢN: Đã dạy hệ thống cách nhận diện Ngọc đang khảm
    private bool IsItemEquipped(ItemData item)
    {
        if (item is EquipmentData equipData && EquipmentManager.instance.currentWeapon != null)
        {
            // 1. Nếu là Vũ khí đang mặc
            if (equipData.weaponStats == EquipmentManager.instance.currentWeapon) return true;

            // 2. Nếu là Ngọc đang khảm
            foreach (WeaponSlot slot in EquipmentManager.instance.currentWeapon.slots)
            {
                if (slot.isOccupied && slot.equippedItem == equipData) return true;
            }
        }
        return false;
    }

    public void ShowItemInfo(ItemData item)
    {
        if (infoPanel == null) return;
        infoPanel.SetActive(true);
        justOpenedInfo = true;
        if (infoNameText != null) infoNameText.text = item.itemName;
        if (infoMechanicText != null)
        {
            string desc = item.itemDescription;
            //if (item is EquipmentData equipData && !string.IsNullOrEmpty(equipData.mechanicToUnlock))
            //    desc += "\n\n<color=yellow>Kỹ năng: " + equipData.mechanicToUnlock + "</color>";
            infoMechanicText.text = desc;
        }
    }

    public void RefreshInventoryFromSave()
    {
        RefreshInventoryUI();
        if (EquipmentManager.instance != null) UpdateWeaponView(EquipmentManager.instance.currentWeapon);
    }

    public void DelayedRefresh()
    {
        // Bỏ qua Invoke, dùng Coroutine chuyên nghiệp hơn
        StartCoroutine(RefreshRoutine());
    }

    private IEnumerator RefreshRoutine()
    {
        // 1. Đợi đến cuối khung hình để chắc chắn các object bị Destroy đã biến mất hoàn toàn
        yield return new WaitForEndOfFrame();

        // 2. Vẽ lại dữ liệu
        RefreshInventoryFromSave();

        // 3. Ép Unity UI cập nhật lại toàn bộ khung hình ngay lập tức
        Canvas.ForceUpdateCanvases();

        if (weaponSlotsContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(weaponSlotsContainer.GetComponent<RectTransform>());

        if (weaponSlotContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(weaponSlotContainer.GetComponent<RectTransform>());
    }
}