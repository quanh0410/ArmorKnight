using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class NPCDialog : MonoBehaviour
{
    [Header("Cài đặt Kích hoạt")]
    public bool autoTriggerOnEnter = false;

    [Header("Dữ liệu của NPC")]
    public DialogData npcData;

    [Header("Sự kiện tương ứng")]
    public UnityEvent onFirstDialogComplete;
    public UnityEvent onDefaultDialogComplete;

    public bool hasSpokenOnce { get; private set; } = false;
    private bool isPlayerInRange = false;

    // --- MỚI: Cờ hiệu để nhường quyền cho NPCQuestCollector ---
    [HideInInspector] public bool isManagedByQuest = false;

    private void Update()
    {
        if (isManagedByQuest) return; // Nếu bị chiếm quyền, không làm gì cả

        if (!autoTriggerOnEnter && isPlayerInRange && Input.GetKeyDown(KeyCode.S))
        {
            TriggerDialog();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (isManagedByQuest) return; // Nếu bị chiếm quyền, không tự bật UI

            if (autoTriggerOnEnter) TriggerDialog();
            else if (InteractionUI.instance != null) InteractionUI.instance.Show(transform, "[S] Nói chuyện");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (isManagedByQuest) return;

            if (InteractionUI.instance != null) InteractionUI.instance.Hide();
        }
    }

    // Đổi thành Public để NPCQuestCollector có thể truyền chữ đã thay số {current} vào
    // Thay thế toàn bộ hàm TriggerDialog cũ bằng hàm này:
    public void TriggerDialog(DialogData.DialogLine[] customLines = null)
    {
        if (DialogUIManager.instance != null && !DialogUIManager.instance.isDialogActive)
        {
            if (InteractionUI.instance != null) InteractionUI.instance.Hide();

            // 1. Xác định kịch bản sẽ chạy (Lần đầu hay Lần sau)
            DialogData.DialogLine[] linesToPlay = null;
            bool isFirstTime = false;

            if (!hasSpokenOnce && npcData.firstTimeLines != null && npcData.firstTimeLines.Length > 0)
            {
                linesToPlay = npcData.firstTimeLines;
                isFirstTime = true;
            }
            else
            {
                linesToPlay = customLines != null ? customLines : npcData.defaultLines;
            }

            // ==========================================
            // 2. MỚI: NHỜ QUEST COLLECTOR "DỊCH" BIẾN THÀNH SỐ
            // ==========================================
            NPCQuestCollector quest = GetComponent<NPCQuestCollector>();
            if (quest != null && linesToPlay != null)
            {
                linesToPlay = quest.ProcessLines(linesToPlay); // Dịch {current} và {target}
            }

            // 3. Phát hội thoại lên màn hình
            if (linesToPlay != null && linesToPlay.Length > 0)
            {
                DialogUIManager.instance.StartDialog(linesToPlay, () =>
                {
                    if (isFirstTime)
                    {
                        hasSpokenOnce = true;
                        onFirstDialogComplete?.Invoke();
                    }
                    else
                    {
                        onDefaultDialogComplete?.Invoke();
                    }
                });
            }
        }
    }
}