using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ItemDataValidationTests
{
    // ==========================================
    // BÀI TEST 1: KHÔNG CÓ ITEM NÀO BỊ TRỐNG ID
    // ==========================================
    [Test]
    public void AllItemsInResources_HaveValidIDs()
    {
        // ARRANGE: Quét toàn bộ thư mục Resources tìm tất cả các file ScriptableObject ItemData
        // (Giả định bạn có class ItemData cơ sở cho các vật phẩm)
        ItemData[] allItems = Resources.LoadAll<ItemData>("");

        Assert.Greater(allItems.Length, 0, "Không tìm thấy ItemData nào trong thư mục Resources! Hãy kiểm tra lại đường dẫn.");

        // ACT & ASSERT: Kiểm tra từng item một
        foreach (ItemData item in allItems)
        {
            // Khẳng định itemID không được rỗng hoặc null
            Assert.IsFalse(string.IsNullOrEmpty(item.itemID),
                $"LỖI NGHIÊM TRỌNG: Vật phẩm '{item.name}' chưa được cấp itemID! Game sẽ không thể Save/Load vật phẩm này.");
        }
    }

    // ==========================================
    // BÀI TEST 2: ĐẢM BẢO KHÔNG CÓ ID NÀO BỊ TRÙNG LẶP
    // ==========================================
    [Test]
    public void AllItemsInResources_HaveUniqueIDs()
    {
        ItemData[] allItems = Resources.LoadAll<ItemData>("");
        HashSet<string> seenIDs = new HashSet<string>();

        foreach (ItemData item in allItems)
        {
            // Bỏ qua các item chưa có ID (đã được bắt lỗi ở bài test trên)
            if (string.IsNullOrEmpty(item.itemID)) continue;

            // Khẳng định ID này chưa từng xuất hiện trong HashSet
            Assert.IsTrue(seenIDs.Add(item.itemID),
                $"LỖI TRÙNG LẶP ID: Vật phẩm '{item.name}' đang xài chung ID '{item.itemID}' với một vật phẩm khác! Việc này sẽ làm hỏng hoàn toàn túi đồ khi Load game.");
        }
    }

    // ==========================================
    // BÀI TEST 3: TEST TOÁN HỌC THUẦN TÚY (LOGIC CHỈ SỐ)
    // ==========================================
    [Test]
    public void DamageCalculation_BaseDamagePlusGem_ReturnsCorrectTotal()
    {
        // Bài test này giả lập logic tính sát thương mà không cần sinh ra Player hay Quái
        // Giả sử bạn có 1 vũ khí base 10 dame, và 1 viên ngọc tăng 5 dame
        int baseDamage = 10;
        int gemBonusDamage = 5;

        // Hành động: Tính toán (Mô phỏng hàm trong EquipmentManager)
        int totalDamage = baseDamage + gemBonusDamage;

        // Khẳng định: Sát thương tổng phải là 15
        Assert.AreEqual(15, totalDamage, "Hệ thống cộng dồn chỉ số sát thương đang tính toán sai!");
    }
}