using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Weapon", menuName = "Equipment/Weapon")]
public class WeaponData : ScriptableObject
{
    public string itemID;
    [Header("--- CÁC Ô NGỌC (SLOTS) ---")]
    public List<WeaponSlot> slots = new List<WeaponSlot>();

    private void OnEnable()
    {
        if (slots != null)
            foreach (WeaponSlot slot in slots)
            {
                slot.equippedItem = null;
                slot.isOccupied = false;
            }
    }
}

// CLASS PHỤ (ĐƯA XUỐNG DƯỚI CÙNG)
[System.Serializable]
public class WeaponSlot
{
    public ItemColor allowedColor;
    public EquipmentData equippedItem;
    public bool isOccupied;
}