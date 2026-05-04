using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment", menuName = "Inventory/Equipment")]
public class EquipmentData : ItemData
{
    [Header("--- HỆ THỐNG LẮP RÁP ---")]
    public ItemColor itemColor;
    public string mechanicToUnlock;
    public EquipmentType equipType = EquipmentType.None;
    public WeaponData weaponStats;

    public override bool UseItem()
    {
        Debug.Log($"Trang bị {itemName} cần được kéo thả để mặc!");
        return false;
    }
}
