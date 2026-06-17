using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video; 

public class MainMenuManager : MonoBehaviour
{
    [Header("--- NÚT BẤM UI ---")]
    public Button continueButton;
    public Button newGameButton;
    public Button quitButton;

    [Header("--- THIẾT LẬP SCENE ---")]
    [Tooltip("Tên Map đầu tiên sẽ load khi bấm New Game (VD: Map_1)")]
    public string firstLevelName = "Map_1";

    [Header("--- CỐT TRUYỆN MỞ ĐẦU (INTRO) ---")]
    [Tooltip("Thêm các khung hình (Ảnh + Chữ) để kể chuyện khi bấm New Game")]
    public List<StoryFrame> introStoryFrames; 

    [Header("--- VIDEO NỀN ---")]
    public VideoPlayer backgroundVideo;

    [Header("--- LIÊN KẾT CORE SCENE ---")]
    [Tooltip("Kéo Object Player từ Core Scene vào đây để hệ thống tự bật/tắt")]
    public GameObject playerObject;

    private bool isTransitioning = false;

    private void Start()
    {
        if (playerObject == null)
        {
            playerObject = GameObject.FindWithTag("Player");
        }

        if (playerObject != null)
        {
            playerObject.SetActive(false);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayMusic("MainMenuTheme");
        }

        CheckSaveFile();
    }

    private void CheckSaveFile()
    {
        if (SaveManager.instance != null && continueButton != null)
        {
            bool hasSave = SaveManager.instance.HasSaveData();
            continueButton.gameObject.SetActive(hasSave);
        }
    }

    public void OnNewGameClicked()
    {
        if (isTransitioning) return;
        PlayClickSound();
        if (SaveManager.instance != null) SaveManager.instance.ClearSaveData();

        StartCoroutine(IntroStoryThenLoadRoutine());
    }

    public void OnContinueClicked()
    {
        if (isTransitioning) return;
        PlayClickSound();

        string mapToLoad = firstLevelName;

        if (SaveManager.instance != null && SaveManager.instance.currentSaveData != null)
        {
            mapToLoad = SaveManager.instance.currentSaveData.respawnSceneName;
            SaveManager.instance.ResetNormalEnemies();
        }

        StartCoroutine(TransitionToGame(mapToLoad, isNewGame: false));
    }

    public void OnQuitClicked()
    {
        if (isTransitioning) return;
        PlayClickSound();

        Debug.Log("<color=red>Đã thoát Game!</color>");
        Application.Quit();
    }

    private void PlayClickSound()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX("UI_Click");
    }

    private IEnumerator IntroStoryThenLoadRoutine()
    {
        isTransitioning = true;
        LockButtons();

        if (backgroundVideo != null)
        {
            backgroundVideo.Pause();
        }

        // 1. Làm tối màn hình Menu đi một chút cho điện ảnh (Tùy chọn)
        if (FadeManager.instance != null)
        {
            yield return StartCoroutine(FadeManager.instance.FadeOut(0.5f));
        }

        // 2. Kêu gọi StoryManager chiếu truyện (nếu có kịch bản)
        if (StoryManager.instance != null && introStoryFrames != null && introStoryFrames.Count > 0)
        {
            // Bật kể chuyện
            StoryManager.instance.PlayStory(introStoryFrames);

            yield return null;
            while (Time.timeScale == 0f)
            {
                yield return null;
            }
        }

        // 3. Truyện đã kể xong, bây giờ chính thức load vào Map 1
        StartCoroutine(TransitionToGame(firstLevelName, isNewGame: true));
    }

    private void LockButtons()
    {
        if (continueButton != null) continueButton.interactable = false;
        if (newGameButton != null) newGameButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;
    }

    private IEnumerator TransitionToGame(string mapName, bool isNewGame)
    {
        if (!isTransitioning)
        {
            isTransitioning = true;
            LockButtons();
        }


        if (!isNewGame && FadeManager.instance != null)
        {
            yield return StartCoroutine(FadeManager.instance.FadeOut(0.5f));
        }

        // 2. Load Map
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mapName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone) yield return null;

        Scene loadedMap = SceneManager.GetSceneByName(mapName);
        if (loadedMap.IsValid()) SceneManager.SetActiveScene(loadedMap);

        // 3. Xử lý vị trí & Trạng thái Player
        if (playerObject != null)
        {
            playerObject.SetActive(true);
            PlayerHealth health = playerObject.GetComponent<PlayerHealth>();
            if (health != null) health.FullHeal();

            if (!isNewGame && SaveManager.instance != null)
            {
                string targetBenchID = SaveManager.instance.currentSaveData.respawnBenchID;
                bool foundBench = false;

                GameObject[] rootObjects = loadedMap.GetRootGameObjects();
                foreach (GameObject root in rootObjects)
                {
                    Checkpoint[] benchesInScene = root.GetComponentsInChildren<Checkpoint>(true);
                    foreach (Checkpoint bench in benchesInScene)
                    {
                        if (bench.benchID == targetBenchID)
                        {
                            playerObject.transform.position = bench.transform.position;
                            foundBench = true;

                            PlayerController pc = playerObject.GetComponent<PlayerController>();
                            if (pc != null)
                            {
                                pc.SnapToRest();
                            }

                            break;
                        }
                    }
                    if (foundBench) break;
                }
            }
            else
            {
                // XỬ LÝ NEW GAME: Đặt Player về đúng điểm SpawnPoint đầu tiên của Game (nếu có)
                // (Bạn có thể bỏ qua nếu Map_1 của bạn đã thiết kế điểm rơi cố định ở gốc 0,0)
            }
        }


        if (FadeManager.instance != null)
        {
            FadeManager.instance.StartCoroutine(FadeManager.instance.FadeIn(0.5f));
        }

        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}