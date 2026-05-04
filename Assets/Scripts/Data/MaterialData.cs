using UnityEngine;

[CreateAssetMenu(fileName = "New Material", menuName = "Inventory/Material")]
public class MaterialData : ItemData
{
    public override bool UseItem()
    {
        return true;
    }
}
