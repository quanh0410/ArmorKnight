using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "2D/Tiles/Advanced Rule Tile")]
public class AdvancedRuleTile : RuleTile<AdvancedRuleTile.Neighbor>
{
    [Header("Kéo thả các Tile 'anh em' vào đây:")]
    public TileBase[] siblingTiles;

    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        public const int Sibling = 3;
    }

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        switch (neighbor)
        {
            case Neighbor.This:
                return base.RuleMatch(neighbor, tile);

            // --- BẢN NÂNG CẤP: DẤU X ĐỎ NAY ĐÃ THÔNG MINH HƠN ---
            case Neighbor.NotThis:
                // 1. Nếu là chính nó -> Dấu X Đỏ báo Sai (False)
                if (tile == this) return false;

                // 2. Nếu là Người nhà -> Dấu X Đỏ cũng báo Sai luôn!
                if (siblingTiles != null)
                {
                    foreach (TileBase sibling in siblingTiles)
                    {
                        if (tile == sibling) return false;
                    }
                }

                // 3. Chỉ khi đó thực sự là khoảng không hoặc vật thể lạ thì mới vẽ (True)
                return true;

            case Neighbor.Sibling:
                if (tile == this) return true;
                if (siblingTiles != null)
                {
                    foreach (TileBase sibling in siblingTiles)
                    {
                        if (tile == sibling) return true;
                    }
                }
                return false;
        }
        return base.RuleMatch(neighbor, tile);
    }
}