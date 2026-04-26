using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Trạng thái hiện tại")]
    public WeaponData currentWeapon;

    public static EquipmentManager instance;
    public static event Action<WeaponData> OnWeaponChanged;

    private void Awake() { if (instance == null) instance = this; }

    private void Start()
    {
        if (currentWeapon != null) EquipWeapon(currentWeapon);
        else UnequipWeapon();
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        currentWeapon = newWeapon;
        OnWeaponChanged?.Invoke(currentWeapon);
    }

    public void UnequipWeapon()
    {
        if (currentWeapon != null)
        {
            foreach (WeaponSlot slot in currentWeapon.slots)
            {
                slot.equippedItem = null;
                slot.isOccupied = false;
            }
        }
        currentWeapon = null;
        OnWeaponChanged?.Invoke(null);
    }

    // --- CÁC HÀM PHỤC VỤ LOGIC GAME (FIX LỖI PLAYERCONTROLLER & UI) ---

    // Khôi phục hàm HasMechanic để Player có thể Dash/WallSlide
    public bool HasMechanic(string mechanicName)
    {
        if (currentWeapon == null) return false;
        foreach (WeaponSlot slot in currentWeapon.slots)
        {
            if (slot.isOccupied && slot.equippedItem != null && slot.equippedItem.mechanicToUnlock == mechanicName)
                return true;
        }
        return false;
    }

    // Khôi phục hàm TryEquipItem để kéo thả ngọc trên UI
    public bool TryEquipItem(EquipmentData equipData, int slotIndex)
    {
        if (currentWeapon == null) return false;
        if (slotIndex < 0 || slotIndex >= currentWeapon.slots.Count) return false;

        WeaponSlot targetSlot = currentWeapon.slots[slotIndex];

        // Nếu ô đã có đồ hoặc sai màu thì từ chối
        if (targetSlot.isOccupied || targetSlot.allowedColor != equipData.itemColor) return false;

        targetSlot.equippedItem = equipData;
        targetSlot.isOccupied = true;

        UpdateWeaponStats();
        return true;
    }


    // --- CÁC HÀM PHỤC VỤ SAVE/LOAD (FIX LỖI SAVEMANAGER) ---

    public void LoadData(string weaponName)
    {
        // Load trực tiếp WeaponData từ Resources/Weapons
        WeaponData weapon = Resources.Load<WeaponData>("Weapons/" + weaponName);
        if (weapon != null) EquipWeapon(weapon);
        else UnequipWeapon();
    }

    public List<string> GetSocketedGemIDs()
    {
        List<string> gemIDs = new List<string>();
        if (currentWeapon != null)
        {
            foreach (var slot in currentWeapon.slots)
            {
                // Lấy itemID của viên ngọc
                gemIDs.Add(slot.isOccupied && slot.equippedItem != null ? slot.equippedItem.itemID : "");
            }
        }
        return gemIDs;
    }

    // Cập nhật lại tên tham số
    // Thay thế hàm LoadGems cũ bằng hàm này
    public void LoadGemsFromInventory(List<string> gemIDs)
    {
        if (currentWeapon == null || gemIDs == null) return;

        for (int i = 0; i < gemIDs.Count; i++)
        {
            if (i >= currentWeapon.slots.Count) break;

            string targetID = gemIDs[i];
            if (string.IsNullOrEmpty(targetID)) continue;

            // TÌM NGỌC TRONG TÚI ĐỒ
            EquipmentData gemFromInventory = null;
            foreach (ItemData item in InventoryManager.instance.items)
            {
                if (item is EquipmentData equip && equip.itemID == targetID)
                {
                    gemFromInventory = equip;
                    break;
                }
            }

            if (gemFromInventory != null)
            {
                currentWeapon.slots[i].equippedItem = gemFromInventory;
                currentWeapon.slots[i].isOccupied = true;
            }
        }
        UpdateWeaponStats();
    }
    private void UpdateWeaponStats()
    {
        Debug.Log("Đã cập nhật sức mạnh vũ khí dựa trên các viên ngọc mới.");
    }

    public void ResetWeaponSlots(WeaponData weapon)
    {
        if (weapon == null) return;
        foreach (var slot in weapon.slots)
        {
            slot.equippedItem = null;
            slot.isOccupied = false;
        }
    }
}