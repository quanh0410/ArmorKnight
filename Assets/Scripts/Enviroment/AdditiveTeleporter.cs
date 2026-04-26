using UnityEngine;
using UnityEngine.SceneManagement; // Bắt buộc để đọc thông tin Scene

public enum TeleporterType
{
    AutoZone,
    InteractableDoor
}

public class AdditiveTeleporter : MonoBehaviour
{
    [Header("Loại Chuyển Cảnh")]
    public TeleporterType type = TeleporterType.AutoZone;

    [Header("Cấu hình Chuyển Cảnh")]
    [Tooltip("Gõ chính xác tên Scene bạn muốn đi tới (VD: Level_2)")]
    public string sceneToLoad;

    // ĐÃ XÓA: public string sceneToUnload; (Giờ code sẽ tự động lo việc này)

    public int targetSpawnPointID = 0;

    private bool isPlayerInRange = false;
    private bool isTeleporting = false;
    private GameObject playerRef;

    private void Update()
    {
        if (type == TeleporterType.InteractableDoor && isPlayerInRange && !isTeleporting)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                StartTeleport(playerRef);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTeleporting)
        {
            playerRef = collision.gameObject;

            if (type == TeleporterType.AutoZone)
            {
                StartTeleport(playerRef);
            }
            else if (type == TeleporterType.InteractableDoor)
            {
                isPlayerInRange = true;
                if (InteractionUI.instance != null) InteractionUI.instance.Show(transform, "[S] Vào");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTeleporting)
        {
            if (type == TeleporterType.InteractableDoor)
            {
                isPlayerInRange = false;
                if (InteractionUI.instance != null) InteractionUI.instance.Hide();
                playerRef = null;
            }
        }
    }

    private void StartTeleport(GameObject player)
    {
        if (isTeleporting) return;
        isTeleporting = true;

        if (InteractionUI.instance != null) InteractionUI.instance.Hide();

        // --- NÂNG CẤP CHUYÊN NGHIỆP: TỰ ĐỘNG LẤY TÊN SCENE CHỨA CÁI CỬA NÀY ---
        string currentSceneToUnload = gameObject.scene.name;

        if (FadeManager.instance != null)
        {
            // Truyền tên Scene tự động lấy được sang cho FadeManager
            FadeManager.instance.StartTransition(sceneToLoad, currentSceneToUnload, targetSpawnPointID, player);
        }
        else
        {
            Debug.LogError("LỖI: Chưa có FadeManager trong Core_Scene!");
        }
    }
}