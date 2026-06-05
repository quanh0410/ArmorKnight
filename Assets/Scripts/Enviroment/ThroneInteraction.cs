using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ThroneInteraction : MonoBehaviour
{
    [Header("--- CÀI ĐẶT KIỂM TRA ---")]
    [Tooltip("ID của vương miện phải trùng khớp với ID trong SaveManager/Inventory")]
    public string crownItemID = "VươngMiệnHoàngGia";

    [Header("--- KỊCH BẢN OUTRO ---")]
    [Tooltip("Danh sách ảnh câu chuyện kết thúc (giống intro lúc New Game)")]
    public List<StoryFrame> outroStoryFrames;
    public string mainMenuSceneName = "MainMenu";

    private bool isPlayerInRange = false;
    private bool isEndingTriggered = false;

    private void Update()
    {
        if (isEndingTriggered || !isPlayerInRange) return;

        // --- SỬA ĐỔI: CHỈ cho phép ấn S nếu thực sự có Vương Miện trong người ---
        if (Input.GetKeyDown(KeyCode.S) && CheckHasCrown())
        {
            StartCoroutine(SitOnThroneRoutine());
        }
    }

    // Hàm bổ trợ kiểm tra sự tồn tại của Vương Miện trong file Save/Túi đồ
    private bool CheckHasCrown()
    {
        if (SaveManager.instance != null && SaveManager.instance.currentSaveData != null)
        {
            return SaveManager.instance.currentSaveData.inventoryItemIDs.Contains(crownItemID);
        }
        return false;
    }

    private IEnumerator SitOnThroneRoutine()
    {
        isEndingTriggered = true;

        if (InteractionUI.instance != null) InteractionUI.instance.Hide();

        // 1. Khóa chặt người chơi và ép thực hiện hành động ngồi nghỉ trên ngai vàng
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.isInputLocked = true;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetFloat("Speed", 0f);
            }
        }

        // Chờ 1.5 giây để người chơi tận hưởng khoảnh khắc vinh quang
        yield return new WaitForSeconds(1.5f);

        // 2. Màn hình tối dần đều (Fade Out)
        if (FadeManager.instance != null)
        {
            yield return StartCoroutine(FadeManager.instance.FadeOut(1.5f));
        }

        // ======================================================================
        // --- VÁ LỖI PHẦN 1: ẨN NGAY PLAYER VÀ NGAI VÀNG KHI MÀN HÌNH ĐÃ TỐI ĐEN ---
        // ======================================================================
        if (player != null)
        {
            // TẮT HOÀN TOÀN PLAYER: Giúp nhân vật biến mất khỏi camera, tắt vật lý, chống rơi tự do vào khoảng không!
            player.gameObject.SetActive(false);
        }

        // ẨN HÌNH ẢNH NGAI VÀNG: Tắt SpriteRenderer để nó không đè giao diện lên màn hình chính Main Menu
        SpriteRenderer throneSr = GetComponent<SpriteRenderer>();
        if (throneSr != null) throneSr.enabled = false;

        // Nhấc Ngai Vàng vào vùng bất tử DontDestroyOnLoad để giữ mạng cho Coroutine chạy tiếp
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // 3. Xóa dữ liệu Save cũ (Để lần sau người chơi bấm New Game sẽ được chơi lại từ đầu)
        if (SaveManager.instance != null)
        {
            SaveManager.instance.ClearSaveData();
        }

        // ======================================================================
        // --- VÁ LỖI PHẦN 2: SỬA LỖI TRÔI HOẠT ẢNH OUTRO ---
        // ======================================================================
        if (StoryManager.instance != null && outroStoryFrames != null && outroStoryFrames.Count > 0)
        {
            StoryManager.instance.PlayStory(outroStoryFrames);

            // ĐIỂM MẤU CHỐT: Nghỉ 1 frame để StoryManager kịp setup và đưa Time.timeScale về 0f!
            yield return null;

            // Bây giờ vòng lặp sẽ chờ chuẩn xác cho đến khi người chơi đọc hết Outro truyện
            while (Time.timeScale == 0f) yield return null;
        }

        // Tối màn hình nhè nhẹ một nhịp ngắn sau khi xem xong Outro truyện kết thúc
        if (FadeManager.instance != null)
        {
            yield return StartCoroutine(FadeManager.instance.FadeOut(0.5f));
        }

        // ======================================================================
        // --- VÁ LỖI PHẦN 3: DỌN SẠCH MAP CŨ VÀ BÀN GIAO CHO MAIN MENU ---
        // ======================================================================
        List<string> scenesToUnload = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.name != "Core_Scene" && s.name != mainMenuSceneName && s.name != "DontDestroyOnLoad")
            {
                scenesToUnload.Add(s.name);
            }
        }

        // Tải Main Menu theo chế độ Additive (Tải thêm), giúp Core_Scene được bảo vệ 100%
        AsyncOperation loadMenu = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
        while (!loadMenu.isDone) yield return null;

        Scene menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (menuScene.IsValid()) SceneManager.SetActiveScene(menuScene);

        // Tiến hành xóa toàn bộ các Scene Map cũ ra khỏi RAM để làm sạch Hierarchy
        foreach (string mapName in scenesToUnload)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(mapName);
            while (unload != null && !unload.isDone) yield return null;
        }

        // Kéo rèm làm sáng màn hình khi đã ra tới sảnh chính Main Menu sạch sẽ, gọn gàng!
        if (FadeManager.instance != null)
        {
            yield return StartCoroutine(FadeManager.instance.FadeIn(1f));
        }

        // Xong việc, tự hủy cái Ngai Vàng ngầm này để dọn rác bộ nhớ hoàn toàn
        Destroy(gameObject);
    }

    #region XỬ LÝ VA CHẠM TRIGGER
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;

            // --- SỬA ĐỔI: CHỈ HIỂN THỊ UI NẾU PLAYER CÓ VƯƠNG MIỆN ---
            if (CheckHasCrown())
            {
                if (InteractionUI.instance != null)
                {
                    InteractionUI.instance.Show(transform, "[S] Ngồi vào ngai vàng");
                }
            }
            else
            {
                // Nếu đi tay không: Im lặng hoàn toàn, không cho tương tác.
                Debug.Log("<color=red>Throne: Player không có vương miện, từ chối tương tác.</color>");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (InteractionUI.instance != null)
            {
                InteractionUI.instance.Hide();
            }
        }
    }
    #endregion
}