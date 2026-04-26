using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;


public class FadeManager : MonoBehaviour
{
    public static FadeManager instance;

    [Header("UI Element")]
    public Image blackScreen;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Bất tử
        }
        else
        {
            Destroy(gameObject);
        }

        // Đảm bảo lúc mới mở game màn hình không bị đen thui
        if (blackScreen != null)
        {
            Color c = blackScreen.color;
            c.a = 0f;
            blackScreen.color = c;
            blackScreen.raycastTarget = false; // Chặn UI cản trở chuột
        }
    }

    // --- HÀM TỐI DẦN (FADE OUT) ---
    public IEnumerator FadeOut(float duration)
    {
        blackScreen.raycastTarget = true; // Chặn người chơi bấm bậy trong lúc chuyển cảnh
        Color c = blackScreen.color;
        float time = 0;

        while (time < duration)
        {
            // Dùng unscaledDeltaTime để màn hình vẫn đen lại kể cả khi Time.timeScale = 0 (Lúc đóng băng game)
            time += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0f, 1f, time / duration);
            blackScreen.color = c;
            yield return null;
        }

        c.a = 1f;
        blackScreen.color = c;
    }

    // --- HÀM SÁNG DẦN (FADE IN) ---
    public IEnumerator FadeIn(float duration)
    {
        Color c = blackScreen.color;
        float time = 0;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, time / duration);
            blackScreen.color = c;
            yield return null;
        }

        c.a = 0f;
        blackScreen.color = c;
        blackScreen.raycastTarget = false;
    }

    public void StartTransition(string sceneToLoad, string sceneToUnload, int targetSpawnID, GameObject player)
    {
        // Bắt đầu chu trình xử lý dựa trên thông tin vừa nhận được từ cái Cửa
        StartCoroutine(TransitionRoutine(sceneToLoad, sceneToUnload, targetSpawnID, player));
    }

    private IEnumerator TransitionRoutine(string load, string unload, int spawnID, GameObject player)
    {
        // Lấy các component của Player
        PlayerController pc = player.GetComponent<PlayerController>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Animator anim = player.GetComponent<Animator>();

        // --- 1. CHẶN MỌI HÀNH ĐỘNG VÀ ĐẦU VÀO NGAY LẬP TỨC ---
        if (pc != null)
        {
            // Tận dụng hàm bạn đã viết sẵn để ngắt lướt, chém, leo tường ngay lập tức
            pc.InterruptDashAndActions();
            // Khóa phím bấm
            pc.isInputLocked = true;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Dừng lực quán tính để nhân vật đứng yên
            rb.bodyType = RigidbodyType2D.Static; // Tạm thời biến thành vật thể tĩnh
        }

        // Ép Animator về tư thế đứng yên
        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
            anim.SetFloat("yVelocity", 0f);
            anim.SetBool("IsGrounded", true);
            anim.Play("Idle", 0, 0f); // Tên animation đứng yên của bạn (Sửa nếu tên khác)
        }

        // 2. Tối màn hình
        yield return StartCoroutine(FadeOut(0.5f));

        player.SetActive(false);

        // 3. Load Scene Mới
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(load, LoadSceneMode.Additive);
        while (!loadOp.isDone) yield return null;

        Scene loadedScene = SceneManager.GetSceneByName(load);
        SceneManager.SetActiveScene(loadedScene);

        // 4. Tìm kiếm SpawnPoint và Dịch chuyển
        if (player != null)
        {
            bool foundSpawn = false;
            GameObject[] rootObjects = loadedScene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                SpawnPoint[] spawnPointsInScene = root.GetComponentsInChildren<SpawnPoint>(true);
                foreach (SpawnPoint sp in spawnPointsInScene)
                {
                    if (sp.spawnPointID == spawnID)
                    {
                        player.transform.position = sp.transform.position;
                        foundSpawn = true;
                        break;
                    }
                }
                if (foundSpawn) break;
            }

            if (!foundSpawn)
            {
                Debug.LogWarning($"<color=yellow>Không tìm thấy SpawnPoint ID {spawnID} trong Map {load}!</color>");
                player.transform.position = Vector3.zero;
            }

            // Ép hệ thống vật lý cập nhật ngay vị trí mới (Tránh lỗi lơ lửng)
            Physics2D.SyncTransforms();
        }

        // 5. Xóa Map cũ
        if (!string.IsNullOrEmpty(unload))
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(unload);
            while (!unloadOp.isDone) yield return null;
        }

        player.SetActive(true);

        // --- 6. GIAI ĐOẠN ỔN ĐỊNH VÀ SÁNG MÀN HÌNH ---
        // Trả lại vật lý bình thường nhưng VẪN KHÓA PHÍM
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;

        // Đợi 1 frame vật lý để nhân vật đáp xuống đất hoàn toàn
        yield return new WaitForFixedUpdate();

        // Ép IsGrounded một lần nữa để chắc chắn Animator không bị giật nhảy
        if (anim != null) anim.SetBool("IsGrounded", true);

        // Sáng màn hình
        yield return StartCoroutine(FadeIn(0.5f));

        // --- 7. HOÀN TẤT: TRẢ LẠI QUYỀN ĐIỀU KHIỂN ---
        if (pc != null) pc.isInputLocked = false;
    }
}