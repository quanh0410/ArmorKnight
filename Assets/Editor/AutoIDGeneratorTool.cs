#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AutoIDGeneratorTool
{
    [MenuItem("Tools/🔥 Quét và Tạo ID Tự Động Toàn Bản Đồ")]
    public static void GenerateAllMissingIDs()
    {
        int count = 0;

        // 1. Quét toàn bộ RƯƠNG trên màn hình
        ChestController[] chests = Object.FindObjectsOfType<ChestController>();
        foreach (var chest in chests)
        {
            if (string.IsNullOrEmpty(chest.chestID))
            {
                chest.chestID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(chest);
                count++;
            }
        }

        // 2. Quét toàn bộ VẬT PHẨM (Máu, Chìa khóa, Tiền...)
        ItemPickup[] items = Object.FindObjectsOfType<ItemPickup>();
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.itemID))
            {
                item.itemID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(item);
                count++;
            }
        }

        // 3. Quét toàn bộ KẺ ĐỊCH (Boss, Quái thường...)
        EnemyHealth[] enemies = Object.FindObjectsOfType<EnemyHealth>();
        foreach (var enemy in enemies)
        {
            if (string.IsNullOrEmpty(enemy.enemyID))
            {
                enemy.enemyID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(enemy);
                count++;
            }
        }

        // 4. --- NÂNG CẤP: Quét toàn bộ GHẾ ĐÁ (Checkpoint) ---
        Checkpoint[] benches = Object.FindObjectsOfType<Checkpoint>();
        foreach (var bench in benches)
        {
            if (string.IsNullOrEmpty(bench.benchID))
            {
                bench.benchID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(bench);
                count++;
            }
        }

        // KẾT LUẬN VÀ LƯU LẠI
        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=green><b>[THÀNH CÔNG]</b></color> Đã quét và tự động cấp ID cho <b>{count}</b> Object trên bản đồ!");
        }
        else
        {
            Debug.Log("<color=yellow><b>[THÔNG BÁO]</b></color> Bản đồ hoàn hảo! Mọi Object đều đã có ID, không cần tạo thêm.");
        }
    }
}
#endif