using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class NPCDialog : MonoBehaviour
{
    [Header("Cài đặt Kích hoạt")]
    public bool autoTriggerOnEnter = false;

    // --- MỚI: Định danh để lưu trữ vĩnh viễn ---
    [Tooltip("Điền ID duy nhất để ghi nhớ vĩnh viễn (VD: TruongBan_1). Nếu để trống, NPC sẽ quên khi chuyển Map.")]
    public string dialogSaveID;

    [Header("Dữ liệu của NPC")]
    public DialogData npcData;

    [Header("Sự kiện tương ứng")]
    public UnityEvent onFirstDialogComplete;
    public UnityEvent onDefaultDialogComplete;

    public bool hasSpokenOnce { get; private set; } = false;
    private bool isPlayerInRange = false;

    [HideInInspector] public bool isManagedByQuest = false;

    private void Start()
    {
        // Kiểm tra xem trong file Save đã có ghi chú về NPC này chưa
        if (!string.IsNullOrEmpty(dialogSaveID) && SaveManager.instance != null)
        {
            if (SaveManager.instance.IsObjectInteracted(dialogSaveID))
            {
                hasSpokenOnce = true;
            }
        }
    }

    private void Update()
    {
        if (isManagedByQuest) return;

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
            if (isManagedByQuest) return;

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

    public void TriggerDialog(DialogData.DialogLine[] customLines = null)
    {
        if (DialogUIManager.instance != null && !DialogUIManager.instance.isDialogActive)
        {
            if (InteractionUI.instance != null) InteractionUI.instance.Hide();

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

            NPCQuestCollector quest = GetComponent<NPCQuestCollector>();
            if (quest != null && linesToPlay != null)
            {
                linesToPlay = quest.ProcessLines(linesToPlay);
            }

            if (linesToPlay != null && linesToPlay.Length > 0)
            {
                DialogUIManager.instance.StartDialog(linesToPlay, () =>
                {
                    if (isFirstTime)
                    {
                        hasSpokenOnce = true;

                        // --- MỚI: Lưu vĩnh viễn trạng thái đã nói chuyện vào SaveManager ---
                        if (!string.IsNullOrEmpty(dialogSaveID) && SaveManager.instance != null)
                        {
                            SaveManager.instance.SaveObjectState(dialogSaveID, true);
                        }

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