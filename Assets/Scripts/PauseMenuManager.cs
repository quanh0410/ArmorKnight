using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // --- MỚI: Bắt buộc phải có để dùng Coroutine ---
using System.Collections.Generic; // --- MỚI: Thêm thư viện để dùng List ---

public class PauseMenuManager : MonoBehaviour
{
    [Header("--- GIAO DIỆN PAUSE ---")]
    public GameObject pauseMenuUI;

    [Tooltip("Điền chính xác tên Scene Menu của bạn (VD: MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    private void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // ========================================================
        // --- MỚI: TÌM PLAYER VÀ KHÓA ĐIỀU KHIỂN BÀN PHÍM ---
        // ========================================================
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.isInputLocked = true; // Khóa chặt không cho nhận phím xoay người/di chuyển
        }
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // ========================================================
        // --- MỚI: MỞ KHÓA ĐIỀU KHIỂN CHO PLAYER KHI CHƠI TIẾP ---
        // ========================================================
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.isInputLocked = false; // Trả lại quyền điều khiển cho người chơi
        }
    }

    // ==========================================
    // --- ĐÃ SỬA: CHUYỂN HÀM THOÁT THÀNH GỌI COROUTINE ---
    // ==========================================
    public void SaveAndQuitToMenu()
    {
        // Giao việc cho Coroutine để có thể chờ màn hình tối đi
        StartCoroutine(SaveAndQuitRoutine());
    }

    private IEnumerator SaveAndQuitRoutine()
    {
        // 1. Lưu game và ẩn UI
        if (SaveManager.instance != null) SaveManager.instance.SaveGame();
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        // 2. Làm tối màn hình
        if (FadeManager.instance != null) yield return StartCoroutine(FadeManager.instance.FadeOut(0.5f));

        Time.timeScale = 1f;
        isPaused = false;

        // 3. THUẬT TOÁN TÌM RÁC: Quét tất cả các Scene đang mở trong hệ thống
        List<string> scenesToUnload = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            // Đưa tất cả vào danh sách tử hình, NGOẠI TRỪ Core_Scene và MainMenu
            if (s.name != "Core_Scene" && s.name != mainMenuSceneName && s.name != "DontDestroyOnLoad")
            {
                scenesToUnload.Add(s.name);
            }
        }

        // 4. BỌC THÉP CHO SCRIPT: 
        // Nếu script Pause này đang nằm trong 1 Map sắp bị xóa, nó sẽ bị bốc hơi giữa chừng.
        // Ta phải nhấc nó ra và cho nó quyền bất tử tạm thời để nó có thể chạy nốt đoạn code bên dưới!
        bool wasMovedToSafety = false;
        if (scenesToUnload.Contains(gameObject.scene.name))
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            wasMovedToSafety = true;
        }

        // 5. Tải Main Menu (Additive để bảo vệ Core_Scene)
        AsyncOperation loadMenu = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
        while (!loadMenu.isDone) yield return null;

        Scene menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (menuScene.IsValid()) SceneManager.SetActiveScene(menuScene);

        // 6. XÓA SẠCH SẼ TẤT CẢ CÁC MAP CŨ BẰNG DANH SÁCH ĐÃ QUÉT
        foreach (string mapName in scenesToUnload)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(mapName);
            // Kiểm tra null vì nếu Scene đã bị xóa từ trước thì hàm sẽ trả về null
            while (unload != null && !unload.isDone) yield return null;
        }

        // 7. Mở rèm (Sáng màn hình khi đã ra tới Menu)
        if (FadeManager.instance != null) yield return StartCoroutine(FadeManager.instance.FadeIn(0.5f));

        // 8. TỰ HỦY: Nếu lúc nãy ta đã nhấc PauseManager ra khỏi Map, thì giờ xong việc phải tự hủy nó đi (tránh rác bộ nhớ)
        if (wasMovedToSafety)
        {
            Destroy(gameObject);
        }
    }
}