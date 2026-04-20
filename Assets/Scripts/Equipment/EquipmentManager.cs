using System;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Trạng thái hiện tại")]
    public WeaponData currentWeapon;

    public static EquipmentManager instance;

    // --- SỰ KIỆN CỐT LÕI: Phát loa mỗi khi Vũ khí bị thay đổi ---
    public static event Action<WeaponData> OnWeaponChanged;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        // Khởi đầu không có vũ khí, ép phát loa null để UI dọn dẹp sạch sẽ
        if (currentWeapon != null) EquipWeapon(currentWeapon);
        else UnequipWeapon();
    }

    // LẮP VŨ KHÍ
    public void EquipWeapon(WeaponData newWeapon)
    {
        currentWeapon = newWeapon;

        // Phát loa thông báo cho UI vẽ vũ khí ra
        OnWeaponChanged?.Invoke(currentWeapon);
    }

    // THÁO VŨ KHÍ
    public void UnequipWeapon()
    {
        if (currentWeapon != null)
        {
            // --- BỔ SUNG: Dọn dẹp toàn bộ ngọc đang cắm trên vũ khí trước khi tháo ---
            foreach (WeaponSlot slot in currentWeapon.slots)
            {
                slot.equippedItem = null;
            }
        }

        currentWeapon = null;
        Debug.Log("Đã tháo vũ khí và dọn dẹp các ô item!");

        // Phát loa thông báo null để UI dọn dẹp
        OnWeaponChanged?.Invoke(null);
    }

    public bool TryEquipItem(ItemData itemToEquip, int slotIndex)
    {
        if (currentWeapon == null) return false;
        if (slotIndex < 0 || slotIndex >= currentWeapon.slots.Count) return false;

        WeaponSlot targetSlot = currentWeapon.slots[slotIndex];

        if (targetSlot.equippedItem != null) return false;

        if (targetSlot.allowedColor != itemToEquip.itemColor)
        {
            Debug.Log("Sai màu!");
            return false;
        }

        targetSlot.equippedItem = itemToEquip;
        return true;
    }

    public bool HasMechanic(string mechanicName)
    {
        if (currentWeapon == null) return false;
        foreach (WeaponSlot slot in currentWeapon.slots)
        {
            if (slot.equippedItem != null && slot.equippedItem.mechanicToUnlock == mechanicName)
                return true;
        }
        return false;
    }
}