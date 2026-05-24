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

        // 1. Quét RƯƠNG
        ChestController[] chests = Object.FindObjectsOfType<ChestController>();
        foreach (var chest in chests) { if (string.IsNullOrEmpty(chest.chestID)) { chest.chestID = System.Guid.NewGuid().ToString(); EditorUtility.SetDirty(chest); count++; } }

        // 2. Quét VẬT PHẨM
        ItemPickup[] items = Object.FindObjectsOfType<ItemPickup>();
        foreach (var item in items) { if (string.IsNullOrEmpty(item.itemID)) { item.itemID = System.Guid.NewGuid().ToString(); EditorUtility.SetDirty(item); count++; } }

        // 3. Quét KẺ ĐỊCH
        EnemyHealth[] enemies = Object.FindObjectsOfType<EnemyHealth>();
        foreach (var enemy in enemies) { if (string.IsNullOrEmpty(enemy.enemyID)) { enemy.enemyID = System.Guid.NewGuid().ToString(); EditorUtility.SetDirty(enemy); count++; } }

        // 4. Quét GHẾ ĐÁ
        Checkpoint[] benches = Object.FindObjectsOfType<Checkpoint>();
        foreach (var bench in benches) { if (string.IsNullOrEmpty(bench.benchID)) { bench.benchID = System.Guid.NewGuid().ToString(); EditorUtility.SetDirty(bench); count++; } }

        // 5. Quét CỬA CÓ KHÓA
        LockedDoorTeleporter[] lockedDoors = Object.FindObjectsOfType<LockedDoorTeleporter>();
        foreach (var door in lockedDoors) { if (string.IsNullOrEmpty(door.doorID)) { door.doorID = System.Guid.NewGuid().ToString(); EditorUtility.SetDirty(door); count++; } }

        // ==========================================
        // MỚI: QUÉT NPC, CÔNG TẮC VÀ DÂY LEO
        // ==========================================

        // 6. Quét HỘI THOẠI NPC
        NPCDialog[] npcs = Object.FindObjectsOfType<NPCDialog>();
        foreach (var npc in npcs)
        {
            if (string.IsNullOrEmpty(npc.dialogSaveID))
            {
                npc.dialogSaveID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(npc);
                count++;
            }
        }

        // 7. Quét CÔNG TẮC (Switch)
        SwitchController[] switches = Object.FindObjectsOfType<SwitchController>();
        foreach (var sw in switches)
        {
            if (string.IsNullOrEmpty(sw.switchID))
            {
                sw.switchID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(sw);
                count++;
            }
        }

        // 8. Quét DÂY LEO (Vine)
        VineInteraction[] vines = Object.FindObjectsOfType<VineInteraction>();
        foreach (var vine in vines)
        {
            if (string.IsNullOrEmpty(vine.vineID))
            {
                vine.vineID = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(vine);
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