using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NPCDialog))]
public class NPCQuestCollector : MonoBehaviour
{
    [Header("--- CẤU HÌNH NHIỆM VỤ ---")]
    public string questID;
    public ItemData targetItemData;
    public int targetQuantity = 10;
    public int targetGold = 200;

    [Header("--- PHẦN THƯỞNG ---")]
    public ItemData rewardItemData;
    public GameObject droppedItemPrefab;
    public Transform dropPoint;

    [Header("--- SỰ KIỆN ---")]
    public UnityEvent onQuestCompleted;

    private NPCDialog npcDialog;
    private bool isPlayerInRange = false;

    private void Start()
    {
        npcDialog = GetComponent<NPCDialog>();

        // 1. Khóa vĩnh viễn NPC nếu đã xong nhiệm vụ từ trước
        if (SaveManager.instance != null && SaveManager.instance.IsObjectInteracted(questID))
        {
            npcDialog.isManagedByQuest = true;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }

    private void Update()
    {
        // 2. GIÀNH QUYỀN TỪ LẦN NÓI CHUYỆN THỨ 2
        if (npcDialog.hasSpokenOnce && !SaveManager.instance.IsObjectInteracted(questID))
        {
            npcDialog.isManagedByQuest = true;

            // Xử lý phím S tại đây
            if (isPlayerInRange && Input.GetKeyDown(KeyCode.S))
            {
                HandleQuestInteraction();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            UpdateQuestUI();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            // Ẩn UI nếu đang nằm trong quyền quản lý
            if (npcDialog.isManagedByQuest && InteractionUI.instance != null) InteractionUI.instance.Hide();
        }
    }

    public void UpdateQuestUI()
    {
        // Chỉ cập nhật giao diện khi đã nói chuyện lần đầu và chưa trả nhiệm vụ
        if (!npcDialog.hasSpokenOnce || SaveManager.instance.IsObjectInteracted(questID)) return;

        if (InteractionUI.instance != null)
        {
            int currentItemCount = GetCurrentItemCount();
            int currentGold = GetCurrentGold();
            string message = $"[S] Giao vật phẩm: {currentItemCount}/{targetQuantity} {targetItemData.itemName} - {currentGold}/{targetGold} Vàng";
            InteractionUI.instance.Show(transform, message);
        }
    }

    private void HandleQuestInteraction()
    {
        int currentCount = GetCurrentItemCount();
        int currentGold = GetCurrentGold();

        // NẾU THIẾU ĐỒ/VÀNG -> Hiện chữ đỏ cảnh báo và DỪNG LẠI
        if (currentCount < targetQuantity || currentGold < targetGold)
        {
            InteractionUI.instance.Show(transform, "<color=red>Chưa đủ vật phẩm hoặc vàng!</color>");
            return;
        }

        // NẾU ĐỦ ĐỒ -> Thay số vào kịch bản và ra lệnh cho NPCDialog chạy hội thoại
        if (DialogUIManager.instance != null && !DialogUIManager.instance.isDialogActive)
        {
            InteractionUI.instance.Hide();
            DialogData.DialogLine[] processedLines = ProcessLines(npcDialog.npcData.defaultLines);
            npcDialog.TriggerDialog(processedLines);
        }
    }

    public DialogData.DialogLine[] ProcessLines(DialogData.DialogLine[] originalLines)
    {
        if (originalLines == null) return null;

        int currentCount = GetCurrentItemCount();
        DialogData.DialogLine[] newLines = new DialogData.DialogLine[originalLines.Length];

        for (int i = 0; i < originalLines.Length; i++)
        {
            newLines[i] = originalLines[i];
            newLines[i].sentence = newLines[i].sentence.Replace("{current}", currentCount.ToString());
            newLines[i].sentence = newLines[i].sentence.Replace("{target}", targetQuantity.ToString());
        }
        return newLines;
    }

    // Hàm này sẽ được gọi từ Unity Event
    public void CompleteQuestState()
    {
        if (SaveManager.instance != null) SaveManager.instance.SaveObjectState(questID, true);

        RemoveQuestItems();
        RemoveGold();
        SpawnReward();

        // Khóa tương tác vĩnh viễn
        if (InteractionUI.instance != null) InteractionUI.instance.Hide();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        onQuestCompleted?.Invoke();
    }

    // --- CÁC HÀM XỬ LÝ KHO ĐỒ ---
    private int GetCurrentItemCount()
    {
        int count = 0;
        if (InventoryManager.instance != null)
        {
            foreach (ItemData item in InventoryManager.instance.items)
                if (item != null && item == targetItemData) count++;
        }
        return count;
    }

    public int GetCurrentGold()
    {
        if (CoinManager.Instance != null) return CoinManager.Instance.totalCoins;
        return 0;
    }

    private void RemoveGold()
    {
        if (CoinManager.Instance != null) CoinManager.Instance.SpendCoins(targetGold);
    }

    private void RemoveQuestItems()
    {
        if (InventoryManager.instance != null)
        {
            int removedCount = 0;
            for (int i = InventoryManager.instance.items.Count - 1; i >= 0; i--)
            {
                if (InventoryManager.instance.items[i] == targetItemData)
                {
                    InventoryManager.instance.items.RemoveAt(i);
                    removedCount++;
                    if (removedCount >= targetQuantity) break;
                }
            }
        }
    }

    private void SpawnReward()
    {
        if (droppedItemPrefab != null)
        {
            Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position;
            GameObject loot = ObjectPoolManager.Instance.Spawn(droppedItemPrefab, spawnPos, Quaternion.identity);

            ItemPickup pickup = loot.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                if (rewardItemData != null) pickup.itemInfo = rewardItemData;
                pickup.itemID = questID + "_Reward";
            }

            Rigidbody2D rb = loot.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = new Vector2(Random.Range(-2f, 2f), 6f);
        }
    }
}