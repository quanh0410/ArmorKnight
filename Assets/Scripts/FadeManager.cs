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
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }

        if (blackScreen != null)
        {
            Color c = blackScreen.color;
            c.a = 0f;
            blackScreen.color = c;
            blackScreen.raycastTarget = false; 
        }
    }

    public IEnumerator FadeOut(float duration)
    {
        if (blackScreen == null) yield break; 

        blackScreen.raycastTarget = true;
        Color c = blackScreen.color;
        float time = 0;

        if (duration <= 0f) duration = 0.1f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(time / duration); 
            blackScreen.color = c;
            yield return null;
        }

        c.a = 1f;
        blackScreen.color = c;
    }

    public IEnumerator FadeIn(float duration)
    {
        if (blackScreen == null) yield break;

        Color c = blackScreen.color;
        float time = 0;

        if (duration <= 0f) duration = 0.1f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(1f - (time / duration)); 
            blackScreen.color = c;
            yield return null;
        }

        c.a = 0f;
        blackScreen.color = c;
        blackScreen.raycastTarget = false;
    }

    public void StartTransition(string sceneToLoad, string sceneToUnload, int targetSpawnID, GameObject player)
    {
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
            pc.InterruptDashAndActions();
            pc.isInputLocked = true;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; 
            rb.bodyType = RigidbodyType2D.Static; 
        }

        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
            anim.SetFloat("yVelocity", 0f);
            anim.SetBool("IsGrounded", true);
            anim.Play("Idle", 0, 0f); 
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
        SpawnPoint targetSP = null;

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
                        targetSP = sp; 
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
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;

        if (targetSP != null && targetSP.isPushSpawn)
        {
            // 1. Ép vận tốc vật lý
            if (rb != null) rb.linearVelocity = targetSP.pushForce;

            if (player != null)
            {
                float flipDirection = Mathf.Sign(targetSP.pushForce.x);
                player.transform.localScale = new Vector3(flipDirection, 1f, 1f);
            }

            // 2. ĐÁNH THỨC ANIMATOR NGAY LẬP TỨC
            if (anim != null)
            {
                anim.SetBool("IsGrounded", false);
                anim.SetFloat("yVelocity", targetSP.pushForce.y); 

                anim.Play("JumpAndFall", 0, 0f);
            }
        }
        else
        {
            yield return new WaitForFixedUpdate();
            if (anim != null) anim.SetBool("IsGrounded", true);
        }

        yield return StartCoroutine(FadeIn(0.5f));

        // --- 7. HOÀN TẤT: TRẢ LẠI QUYỀN ĐIỀU KHIỂN ---
        if (pc != null) pc.isInputLocked = false;
    }
}