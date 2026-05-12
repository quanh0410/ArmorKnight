using UnityEngine;
using UnityEngine.SceneManagement;

public class LockedDoorTeleporter : MonoBehaviour
{
    [Header("--- ĐỊNH DANH (Bắt buộc) ---")]
    [Tooltip("ID duy nhất để lưu trạng thái cửa mở (VD: BossDoor_01)")]
    public string doorID;

    [Header("--- HỆ THỐNG KHÓA ---")]
    public bool isLocked = true;
    public ItemData requiredKey;
    public int keysNeeded = 1;

    [Header("--- CẤU HÌNH CHUYỂN CẢNH ---")]
    [Tooltip("Gõ chính xác tên Scene bạn muốn đi tới (VD: Level_2)")]
    public string sceneToLoad;
    public int targetSpawnPointID = 0;

    [Header("--- HÌNH ẢNH ---")]
    [Tooltip("Kéo thả hình ảnh cửa đang mở vào đây để thay đổi khi mở khóa")]
    public Sprite unlockedDoorSprite;

    private bool isPlayerInRange = false;
    private bool isTeleporting = false;
    private GameObject playerRef;

    private void Start()
    {
        // Kiểm tra dữ liệu SaveGame xem cửa này đã từng được mở khóa chưa
        if (SaveManager.instance != null && SaveManager.instance.IsObjectInteracted(doorID))
        {
            UnlockDoorSilently();
        }
    }

    private void Update()
    {
        // Khi người chơi đứng trong vùng cửa, chưa chuyển cảnh và nhấn phím S
        if (isPlayerInRange && !isTeleporting && Input.GetKeyDown(KeyCode.S))
        {
            if (isLocked)
            {
                TryUnlockDoor(); // Thử dùng chìa khóa mở cửa
            }
            else
            {
                StartTeleport(playerRef); // Nếu đã mở, tiến hành chuyển cảnh
            }
        }
    }

    // Hàm xử lý việc mở khóa
    private void TryUnlockDoor()
    {
        int keyCount = InventoryManager.instance.GetItemCount(requiredKey);

        if (keyCount >= keysNeeded)
        {
            // 1. Tiêu thụ chìa khóa
            InventoryManager.instance.RemoveItem(requiredKey, keysNeeded);

            // 2. Mở khóa và Lưu trạng thái
            isLocked = false;
            if (SaveManager.instance != null)
            {
                SaveManager.instance.SaveObjectState(doorID);
            }

            // 3. Thay đổi hình ảnh cửa
            UpdateDoorVisuals();

            // 4. Đổi ngay thông báo UI thành chữ "Vào"
            if (InteractionUI.instance != null) InteractionUI.instance.Show(transform, "[S] Vào");

            Debug.Log("Đã mở khóa cửa thành công!");
        }
        else
        {
            // Báo lỗi thiếu chìa
            InteractionUI.instance.Show(transform, "<color=red>Thiếu chìa khóa!</color>");
        }
    }

    // Hàm mở khóa âm thầm ngay khi bắt đầu Game (dành cho tính năng Load Game)
    private void UnlockDoorSilently()
    {
        isLocked = false;
        UpdateDoorVisuals();
    }

    // Hàm cập nhật hình ảnh cửa
    private void UpdateDoorVisuals()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && unlockedDoorSprite != null)
        {
            sr.sprite = unlockedDoorSprite;
        }

        // Tắt Animator nếu bạn có Animation cửa đang đóng
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
    }

    // --- CÁC HÀM XỬ LÝ VA CHẠM VÀ CHUYỂN CẢNH ---

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTeleporting)
        {
            isPlayerInRange = true;
            playerRef = collision.gameObject;

            UpdateUI(); // Cập nhật lại dòng chữ hướng dẫn
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTeleporting)
        {
            isPlayerInRange = false;
            playerRef = null;

            if (InteractionUI.instance != null) InteractionUI.instance.Hide();
        }
    }

    private void UpdateUI()
    {
        if (InteractionUI.instance != null)
        {
            if (isLocked)
            {
                int currentKeys = InventoryManager.instance.GetItemCount(requiredKey);
                InteractionUI.instance.Show(transform, $"[S] Mở Cửa ({currentKeys}/{keysNeeded})");
            }
            else
            {
                InteractionUI.instance.Show(transform, "[S] Vào");
            }
        }
    }

    private void StartTeleport(GameObject player)
    {
        if (isTeleporting) return;
        isTeleporting = true;

        if (InteractionUI.instance != null) InteractionUI.instance.Hide();

        string currentSceneToUnload = gameObject.scene.name;

        if (FadeManager.instance != null)
        {
            FadeManager.instance.StartTransition(sceneToLoad, currentSceneToUnload, targetSpawnPointID, player);
        }
        else
        {
            Debug.LogError("LỖI: Chưa có FadeManager trong Core_Scene!");
        }
    }
}