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

        if (Input.GetKeyDown(KeyCode.S) && CheckHasCrown())
        {
            StartCoroutine(SitOnThroneRoutine());
        }
    }

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

        yield return new WaitForSeconds(1.5f);

        // 2. Màn hình tối dần đều (Fade Out)
        if (FadeManager.instance != null)
        {
            yield return StartCoroutine(FadeManager.instance.FadeOut(1.5f));
        }

        if (player != null)
        {
            player.gameObject.SetActive(false);
        }

        // ẨN HÌNH ẢNH NGAI VÀNG: Tắt SpriteRenderer để nó không đè giao diện lên màn hình chính Main Menu
        SpriteRenderer throneSr = GetComponent<SpriteRenderer>();
        if (throneSr != null) throneSr.enabled = false;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // 3. Xóa dữ liệu Save cũ (Để lần sau người chơi bấm New Game sẽ được chơi lại từ đầu)
        if (SaveManager.instance != null)
        {
            SaveManager.instance.ClearSaveData();
        }

        if (StoryManager.instance != null && outroStoryFrames != null && outroStoryFrames.Count > 0)
        {
            StoryManager.instance.PlayStory(outroStoryFrames);

            yield return null;

            while (Time.timeScale == 0f) yield return null;
        }

        if (FadeManager.instance != null)
        {
            yield return StartCoroutine(FadeManager.instance.FadeOut(0.5f));
        }

        List<string> scenesToUnload = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.name != "Core_Scene" && s.name != mainMenuSceneName && s.name != "DontDestroyOnLoad")
            {
                scenesToUnload.Add(s.name);
            }
        }

        AsyncOperation loadMenu = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
        while (!loadMenu.isDone) yield return null;

        Scene menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (menuScene.IsValid()) SceneManager.SetActiveScene(menuScene);

        foreach (string mapName in scenesToUnload)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(mapName);
            while (unload != null && !unload.isDone) yield return null;
        }

        if (FadeManager.instance != null)
        {
            yield return StartCoroutine(FadeManager.instance.FadeIn(1f));
        }

        //Destroy(gameObject);
    }

    #region XỬ LÝ VA CHẠM TRIGGER
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;

            if (CheckHasCrown())
            {
                if (InteractionUI.instance != null)
                {
                    InteractionUI.instance.Show(transform, "[S] Ngồi vào ngai vàng");
                }
            }
            else
            {
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