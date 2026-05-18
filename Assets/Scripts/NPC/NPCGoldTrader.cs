using UnityEngine;

[RequireComponent(typeof(NPCDialog))]
public class NPCGoldTrader : MonoBehaviour
{
    [Header("--- CẤU HÌNH GIAO DỊCH ---")]
    public string questID = "GoldTrader_1";
    public int requiredGold = 150;

    [Header("--- PHẦN THƯỞNG ---")]
    public GameObject rewardPrefab;
    public Transform dropPoint;

    private NPCDialog npcDialog;
    private bool isPlayerInRange = false;

    private void Start()
    {
        npcDialog = GetComponent<NPCDialog>();

        // Khóa vĩnh viễn NPC nếu đã mua đồ từ trước
        if (SaveManager.instance != null && SaveManager.instance.IsObjectInteracted(questID))
        {
            npcDialog.isManagedByQuest = true;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }

    private void Update()
    {
        // GIÀNH QUYỀN KIỂM SOÁT TỪ LẦN TƯƠNG TÁC THỨ 2 TRỞ ĐI
        if (npcDialog.hasSpokenOnce && !SaveManager.instance.IsObjectInteracted(questID))
        {
            npcDialog.isManagedByQuest = true;

            if (isPlayerInRange && Input.GetKeyDown(KeyCode.S))
            {
                HandleTrade();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            UpdateUI();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (npcDialog.isManagedByQuest && InteractionUI.instance != null) InteractionUI.instance.Hide();
        }
    }

    // ==========================================
    // HÀM NÀY GỌI KHI HỘI THOẠI LẦN ĐẦU KẾT THÚC
    // ==========================================
    public void OnFirstTalkDone()
    {
        // Bắt buộc hiện UI mua bán ngay lập tức khi chữ vừa tắt
        if (isPlayerInRange) UpdateUI();
    }

    public void UpdateUI()
    {
        if (!npcDialog.hasSpokenOnce || SaveManager.instance.IsObjectInteracted(questID)) return;

        if (InteractionUI.instance != null)
        {
            int currentGold = CoinManager.Instance != null ? CoinManager.Instance.totalCoins : 0;
            string message = $"[S] Mua vật phẩm: {currentGold}/{requiredGold} Vàng";
            InteractionUI.instance.Show(transform, message);
        }
    }

    private void HandleTrade()
    {
        int currentGold = CoinManager.Instance != null ? CoinManager.Instance.totalCoins : 0;

        // KIỂM TRA VÀNG
        if (currentGold < requiredGold)
        {
            if (InteractionUI.instance != null)
            {
                InteractionUI.instance.Show(transform, "<color=red>Chưa đủ vàng!</color>");
            }
            return;
        }

        // ĐỦ VÀNG -> Phát hội thoại cám ơn
        if (DialogUIManager.instance != null && !DialogUIManager.instance.isDialogActive)
        {
            if (InteractionUI.instance != null) InteractionUI.instance.Hide();
            npcDialog.TriggerDialog(npcDialog.npcData.defaultLines);
        }
    }

    // ==========================================
    // HÀM NÀY GỌI KHI HỘI THOẠI CÁM ƠN KẾT THÚC
    // ==========================================
    public void CompleteTrade()
    {
        if (CoinManager.Instance != null) CoinManager.Instance.SpendCoins(requiredGold);
        if (SaveManager.instance != null) SaveManager.instance.SaveObjectState(questID, true);

        if (rewardPrefab != null)
        {
            Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position;
            GameObject loot = ObjectPoolManager.Instance.Spawn(rewardPrefab, spawnPos, Quaternion.identity);

            Rigidbody2D rb = loot.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = new Vector2(Random.Range(-2f, 2f), 6f);
        }

        if (InteractionUI.instance != null) InteractionUI.instance.Hide();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }
}