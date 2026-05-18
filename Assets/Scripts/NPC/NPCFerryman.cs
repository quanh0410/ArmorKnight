using UnityEngine;

[RequireComponent(typeof(NPCDialog))]
public class NPCFerryman : MonoBehaviour
{
    [Header("--- ĐIỀU KIỆN KHỞI HÀNH ---")]
    [Tooltip("Gõ chính xác ID của 3 Boss lưu trong SaveManager để kiểm tra")]
    public string[] requiredBossIDs;

    [Header("--- CẤU HÌNH CHUYỂN MAP ---")]
    public string sceneToLoad;
    public int targetSpawnPointID = 0;

    private NPCDialog npcDialog;
    private bool isPlayerInRange = false;
    private GameObject playerRef;
    private bool isTeleporting = false;

    private void Start()
    {
        npcDialog = GetComponent<NPCDialog>();

        // GIÀNH QUYỀN KIỂM SOÁT TỪ NPCDIALOG:
        // Chặn không cho NPCDialog tự động bật chữ [S] Nói chuyện khi người chơi lại gần.
        // Script Ferryman này sẽ tự quyết định chữ hiển thị.
        if (npcDialog != null)
        {
            npcDialog.isManagedByQuest = true;
        }
    }

    private void Update()
    {
        // Lắng nghe phím S khi người chơi ở trong vùng và chưa bắt đầu chuyển cảnh
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.S) && !isTeleporting)
        {
            HandleInteraction();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTeleporting)
        {
            isPlayerInRange = true;
            playerRef = collision.gameObject;
            UpdateUI(); // Cập nhật lại UI mỗi lần bước vào vùng
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTeleporting)
        {
            isPlayerInRange = false;
            playerRef = null;

            // Ẩn UI khi người chơi bỏ đi
            if (InteractionUI.instance != null) InteractionUI.instance.Hide();
        }
    }

    // ==========================================
    // KIỂM TRA TRẠNG THÁI 3 BOSS
    // ==========================================
    private bool AreAllBossesDead()
    {
        if (SaveManager.instance == null) return false;

        // Quét qua danh sách ID Boss. Chỉ cần 1 con chưa chết (chưa được tương tác) -> Trả về false
        foreach (string bossID in requiredBossIDs)
        {
            if (!SaveManager.instance.IsObjectInteracted(bossID))
            {
                return false;
            }
        }
        return true; // Tất cả đã chết
    }

    // ==========================================
    // CẬP NHẬT GIAO DIỆN (UI)
    // ==========================================
    private void UpdateUI()
    {
        if (InteractionUI.instance != null)
        {
            if (AreAllBossesDead())
            {
                // Nếu xong điều kiện -> Hiện chữ thông báo chuyển Map
                InteractionUI.instance.Show(transform, $"[S] Di chuyển");
            }
            else
            {
                // Nếu chưa xong -> Hiện chữ Nói chuyện thông thường
                InteractionUI.instance.Show(transform, "[S] Nói chuyện");
            }
        }
    }

    // ==========================================
    // XỬ LÝ KHI BẤM PHÍM S
    // ==========================================
    private void HandleInteraction()
    {
        // Chặn tương tác nếu đang có khung hội thoại mở
        if (DialogUIManager.instance != null && DialogUIManager.instance.isDialogActive) return;

        // Ẩn chữ [S] trên đầu NPC đi để chuẩn bị cho hành động
        if (InteractionUI.instance != null) InteractionUI.instance.Hide();

        if (AreAllBossesDead())
        {
            // --- HÀNH ĐỘNG 1: CHUYỂN MAP ---
            StartTeleport();
        }
        else
        {
            // --- HÀNH ĐỘNG 2: PHÁT HỘI THOẠI ĐUỔI ĐI ---
            // Gọi lệnh TriggerDialog() của NPCDialog. 
            // Kịch bản thoại sẽ là những gì bạn điền ở First Time Lines và Default Lines trên Inspector.
            if (npcDialog != null)
            {
                npcDialog.TriggerDialog();
            }
        }
    }

    // ==========================================
    // GỌI FADEMANAGER ĐỂ CHUYỂN CẢNH
    // ==========================================
    private void StartTeleport()
    {
        if (isTeleporting) return;
        isTeleporting = true;

        if (InteractionUI.instance != null) InteractionUI.instance.Hide();

        // Tự động đọc tên Scene mà ông Lái đò đang đứng để gửi lệnh Unload
        string currentSceneToUnload = gameObject.scene.name;

        if (FadeManager.instance != null)
        {
            // Truyền 4 tham số khớp hoàn toàn với hàm StartTransition trong FadeManager.cs
            FadeManager.instance.StartTransition(sceneToLoad, currentSceneToUnload, targetSpawnPointID, playerRef);
        }
        else
        {
            Debug.LogError("LỖI: Chưa có FadeManager trong Scene để thực hiện chuyển map!");
        }
    }
}