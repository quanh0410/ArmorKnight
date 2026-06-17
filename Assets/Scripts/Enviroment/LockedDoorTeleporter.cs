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
        if (SaveManager.instance != null && SaveManager.instance.IsObjectInteracted(doorID))
        {
            UnlockDoorSilently();
        }
    }

    private void Update()
    {
        if (isPlayerInRange && !isTeleporting && Input.GetKeyDown(KeyCode.S))
        {
            if (isLocked)
            {
                TryUnlockDoor();
            }
            else
            {
                StartTeleport(playerRef); 
            }
        }
    }

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
            InteractionUI.instance.Show(transform, "<color=red>Thiếu chìa khóa!</color>");
        }
    }

    private void UnlockDoorSilently()
    {
        isLocked = false;
        UpdateDoorVisuals();
    }

    private void UpdateDoorVisuals()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && unlockedDoorSprite != null)
        {
            sr.sprite = unlockedDoorSprite;
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTeleporting)
        {
            isPlayerInRange = true;
            playerRef = collision.gameObject;

            UpdateUI(); 
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