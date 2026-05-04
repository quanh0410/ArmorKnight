using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class BossArenaManager : MonoBehaviour
{
    [Header("--- CÀI ĐẶT ĐẤU TRƯỜNG ---")]
    public GameObject[] doors;
    public Collider2D bossRoomBounds;

    [Header("--- KỊCH BẢN ĐỘNG ĐẤT ---")]
    [Tooltip("Khoảng thời gian im lặng (giây) tính từ lúc cửa sập đến lúc bắt đầu động đất")]
    public float delayBeforeEarthquake = 0.5f;
    [Tooltip("Độ mạnh của trận động đất")]
    public float shakeIntensity = 5f;
    [Tooltip("Thời gian động đất diễn ra (giây) trước khi Boss thức tỉnh")]
    public float earthquakeDuration = 2f;

    [Header("--- THEO DÕI BOSS ---")]
    public EnemyHealth bossHealth;
    public DungeonBoss bossLogic;

    private bool isBattleActive = false;
    private Collider2D triggerCollider;

    // Biến lưu Camera
    private CinemachineConfiner2D cameraConfiner;
    private Collider2D originalCameraBounds;

    private void Start()
    {
        triggerCollider = GetComponent<Collider2D>();
        UnlockDoors();
    }

    private void Update()
    {
        if (isBattleActive && bossHealth != null)
        {
            if (bossHealth.isDead) EndBattle();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isBattleActive)
        {
            StartCoroutine(StartBattleRoutine());
        }
    }

    // ==========================================
    // CHUỖI SỰ KIỆN: CỬA SẬP -> ĐỘNG ĐẤT -> ĐÁNH
    // ==========================================
    private IEnumerator StartBattleRoutine()
    {
        isBattleActive = true;
        if (triggerCollider != null) triggerCollider.enabled = false;

        // BƯỚC 1: CỬA SẬP XUỐNG VÀ ĐỔI GÓC NHÌN NGAY LẬP TỨC
        LockDoors();
        GameObject mainCamObj = GameObject.Find("CinemachineCamera");
        if (mainCamObj != null)
        {
            cameraConfiner = mainCamObj.GetComponent<CinemachineConfiner2D>();
            if (cameraConfiner != null)
            {
                originalCameraBounds = cameraConfiner.BoundingShape2D;
                cameraConfiner.BoundingShape2D = bossRoomBounds;
                cameraConfiner.InvalidateBoundingShapeCache();
            }
        }

        // BƯỚC 2: TẠO MỘT NHỊP DỪNG NGẮN ĐỂ TĂNG SỰ CĂNG THẲNG
        // Người chơi nghe tiếng cửa sập và nhận ra mình đã bị nhốt
        yield return new WaitForSeconds(delayBeforeEarthquake);

        // BƯỚC 3: KÍCH HOẠT ĐỘNG ĐẤT
        if (CinemachineShake.Instance != null)
        {
            // Thay vì gọi ShakeCamera, ta gọi ShakeCameraContinuous và truyền vào thời gian
            CinemachineShake.Instance.ShakeCameraContinuous(shakeIntensity, earthquakeDuration);
        }

        // BƯỚC 4: CHỜ ĐỘNG ĐẤT QUA ĐI
        yield return new WaitForSeconds(earthquakeDuration);

        // BƯỚC 5: ĐÁNH THỨC BOSS
        if (bossLogic != null)
        {
            bossLogic.SetArenaBounds(bossRoomBounds);
            bossLogic.WakeUpBoss();
            Debug.Log("Kết thúc Động đất! Boss bắt đầu tấn công.");
        }
    }

    private void EndBattle()
    {
        isBattleActive = false;
        UnlockDoors();

        if (cameraConfiner != null && originalCameraBounds != null)
        {
            cameraConfiner.BoundingShape2D = originalCameraBounds;
            cameraConfiner.InvalidateBoundingShapeCache();
        }
    }

    private void LockDoors()
    {
        foreach (GameObject door in doors) if (door != null) door.SetActive(true);
    }

    private void UnlockDoors()
    {
        foreach (GameObject door in doors) if (door != null) door.SetActive(false);
    }
}