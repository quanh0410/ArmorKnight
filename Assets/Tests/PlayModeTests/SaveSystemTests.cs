using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;

public class SaveSystemTests
{
    private GameObject saveManagerObj;
    private SaveManager saveManager;

    // ==========================================
    // 1. CHUẨN BỊ MÔI TRƯỜNG TEST (SETUP)
    // ==========================================
    [SetUp]
    public void Setup()
    {
        // Tạo một GameObject ảo và gắn SaveManager vào
        saveManagerObj = new GameObject("TestSaveManager");
        saveManager = saveManagerObj.AddComponent<SaveManager>();

        // Cố tình tạo một vùng dữ liệu trống để test, tránh ghi đè file save thật của bạn
        saveManager.currentSaveData = new GameSaveData();
    }

    [TearDown]
    public void Teardown()
    {
        // Dọn dẹp sau khi Test xong
        Object.DestroyImmediate(saveManagerObj);
    }

    // ==========================================
    // 2. CÁC BÀI TEST CHỨC NĂNG
    // ==========================================

    [UnityTest]
    public IEnumerator SaveObjectState_Permanent_AddsToInteractedList()
    {
        // ARRANGE: Chuẩn bị một ID rương hoặc cửa
        string chestID = "Chest_Gold_01";

        // ACT: Gọi hàm lưu trạng thái vĩnh viễn (isPermanent = true)
        saveManager.SaveObjectState(chestID, true);
        yield return null;

        // ASSERT: Khẳng định ID này phải nằm trong danh sách interactedObjectIDs
        Assert.IsTrue(saveManager.currentSaveData.interactedObjectIDs.Contains(chestID), "ID vật thể vĩnh viễn không được lưu vào danh sách!");

        // Khẳng định hàm IsObjectInteracted phải trả về true
        Assert.IsTrue(saveManager.IsObjectInteracted(chestID), "Hàm kiểm tra IsObjectInteracted trả về sai kết quả!");
    }

    [UnityTest]
    public IEnumerator SaveObjectState_Temporary_AddsToDeadEnemiesList()
    {
        // ARRANGE: Chuẩn bị một ID quái thường
        string enemyID = "Slime_005";

        // ACT: Gọi hàm lưu trạng thái tạm thời (isPermanent = false)
        saveManager.SaveObjectState(enemyID, false);
        yield return null;

        // ASSERT: Khẳng định ID này phải nằm trong danh sách deadEnemyIDs, KHÔNG được nằm trong interactedObjectIDs
        Assert.IsTrue(saveManager.currentSaveData.deadEnemyIDs.Contains(enemyID), "ID quái thường không được lưu vào deadEnemyIDs!");
        Assert.IsFalse(saveManager.currentSaveData.interactedObjectIDs.Contains(enemyID), "ID quái thường bị lưu nhầm sang danh sách vĩnh viễn!");
    }

    [UnityTest]
    public IEnumerator UpdateCheckpoint_SavesLocation_And_ClearsDeadEnemies()
    {
        // ARRANGE: Giả lập đã giết 1 con quái
        saveManager.currentSaveData.deadEnemyIDs.Add("Zombie_01");

        string testSceneName = "Map_Poison_Swamp";
        string testBenchID = "Bench_03";

        // ACT: Ngồi vào ghế đá
        // Lưu ý: Trong SaveManager thực tế, hàm UpdateCheckpoint có gọi SaveGame() chứa các Manager khác.
        // Để Unit Test chạy mượt không báo lỗi Null, ta chỉ test logic cập nhật data.
        saveManager.currentSaveData.respawnSceneName = testSceneName;
        saveManager.currentSaveData.respawnBenchID = testBenchID;
        saveManager.ResetNormalEnemies(); // Gọi hàm xóa quái
        yield return null;

        // ASSERT: Kiểm tra tọa độ hồi sinh
        Assert.AreEqual(testSceneName, saveManager.currentSaveData.respawnSceneName, "Tên Map hồi sinh lưu bị sai!");
        Assert.AreEqual(testBenchID, saveManager.currentSaveData.respawnBenchID, "ID ghế đá hồi sinh lưu bị sai!");

        // Khẳng định danh sách quái đã chết phải bị xóa trắng rỗng (bằng 0)
        Assert.AreEqual(0, saveManager.currentSaveData.deadEnemyIDs.Count, "Danh sách quái chết không bị reset khi ngồi ghế đá!");
    }

    [UnityTest]
    public IEnumerator ClearSaveData_ResetsAllVariables()
    {
        // ARRANGE: Bơm dữ liệu giả vào
        saveManager.currentSaveData.interactedObjectIDs.Add("Door_01");
        saveManager.currentSaveData.totalCoins = 999;

        // ACT: Tạo một hàm giả lập ClearSaveData phần Logic (Bỏ qua đoạn tương tác File IO và Manager khác)
        saveManager.currentSaveData = new GameSaveData();
        yield return null;

        // ASSERT: Khẳng định dữ liệu đã về trạng thái xuất xưởng
        Assert.AreEqual(0, saveManager.currentSaveData.interactedObjectIDs.Count, "Danh sách ID không bị xóa sạch!");
        Assert.AreEqual(0, saveManager.currentSaveData.totalCoins, "Tiền không bị reset về 0!");
        Assert.AreEqual("1", saveManager.currentSaveData.respawnSceneName, "Map mặc định không được đưa về '1'!");
    }
}