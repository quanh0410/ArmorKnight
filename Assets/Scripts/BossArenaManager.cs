using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class BossArenaManager : MonoBehaviour
{
    [Header("--- CÀI ĐẶT ĐẤU TRƯỜNG ---")]
    public GameObject[] doors;
    public Collider2D bossRoomBounds;

    [Header("--- CAMERA CINEMATIC ---")]
    [Tooltip("Kéo Transform của Boss (hoặc điểm bạn muốn Camera nhìn vào) trong lúc hội thoại")]
    public Transform dialogCameraFocus; // --- MỚI: Điểm Camera sẽ lia tới ---

    [Header("--- KỊCH BẢN ĐỘNG ĐẤT ---")]
    public float delayBeforeEarthquake = 0.5f;
    public float shakeIntensity = 5f;
    public float earthquakeDuration = 2f;

    [Header("--- THEO DÕI BOSS (HỖ TRỢ NHIỀU BOSS) ---")]
    public EnemyHealth[] bossHealths;
    public GameObject[] bossObjects;

    // Biến nội bộ
    private bool isBattleActive = false;
    private Collider2D triggerCollider;
    private NPCDialog bossDialog;

    // Biến lưu Camera
    private CinemachineConfiner2D cameraConfiner;
    private Collider2D originalCameraBounds;

    // --- MỚI: Biến để thao tác lia Camera ---
    private CinemachineCamera mainCam;
    private Transform originalFollowTarget;

    private void Start()
    {
        triggerCollider = GetComponent<Collider2D>();
        bossDialog = GetComponent<NPCDialog>();
        UnlockDoors();
    }

    private void Update()
    {
        if (isBattleActive && bossHealths != null && bossHealths.Length > 0)
        {
            bool areAllBossesDead = true;

            foreach (EnemyHealth health in bossHealths)
            {
                if (health != null && !health.isDead)
                {
                    areAllBossesDead = false;
                    break;
                }
            }

            if (areAllBossesDead)
            {
                EndBattle();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isBattleActive)
        {
            isBattleActive = true;
            if (triggerCollider != null) triggerCollider.enabled = false;

            PrepareArena();

            if (bossDialog != null)
            {
                bossDialog.isManagedByQuest = true;
                bossDialog.onFirstDialogComplete.AddListener(OnDialogFinished);
                bossDialog.onDefaultDialogComplete.AddListener(OnDialogFinished);
                bossDialog.TriggerDialog();
            }
            else
            {
                StartCoroutine(EarthquakeAndWakeBossRoutine());
            }
        }
    }

    // ==========================================
    // CÁC BƯỚC KỊCH BẢN ĐẠO DIỄN
    // ==========================================
    private void PrepareArena()
    {
        LockDoors();
        GameObject mainCamObj = GameObject.Find("CinemachineCamera");
        if (mainCamObj != null)
        {
            // 1. Gài Bounding Box giới hạn phòng
            cameraConfiner = mainCamObj.GetComponent<CinemachineConfiner2D>();
            if (cameraConfiner != null)
            {
                originalCameraBounds = cameraConfiner.BoundingShape2D;
                cameraConfiner.BoundingShape2D = bossRoomBounds;
                cameraConfiner.InvalidateBoundingShapeCache();
            }

            // 2. TẠO HIỆU ỨNG LIA CAMERA TỚI BOSS
            mainCam = mainCamObj.GetComponent<CinemachineCamera>();
            if (mainCam != null)
            {
                // Lưu lại mục tiêu cũ (Chính là Player)
                originalFollowTarget = mainCam.Follow;

                // Đổi mục tiêu sang Boss nếu có gán điểm nhìn và có hội thoại
                if (dialogCameraFocus != null && bossDialog != null)
                {
                    mainCam.Follow = dialogCameraFocus;
                }
            }
        }
    }

    private void OnDialogFinished()
    {
        bossDialog.onFirstDialogComplete.RemoveListener(OnDialogFinished);
        bossDialog.onDefaultDialogComplete.RemoveListener(OnDialogFinished);

        // --- MỚI: TRẢ CAMERA VỀ LẠI CHO PLAYER ---
        if (mainCam != null && originalFollowTarget != null)
        {
            mainCam.Follow = originalFollowTarget;
        }

        StartCoroutine(EarthquakeAndWakeBossRoutine());
    }

    private IEnumerator EarthquakeAndWakeBossRoutine()
    {
        yield return new WaitForSeconds(delayBeforeEarthquake);

        if (CinemachineShake.Instance != null)
        {
            CinemachineShake.Instance.ShakeCameraContinuous(shakeIntensity, earthquakeDuration);
        }

        yield return new WaitForSeconds(earthquakeDuration);

        if (bossObjects != null && bossObjects.Length > 0)
        {
            foreach (GameObject boss in bossObjects)
            {
                if (boss != null)
                {
                    boss.SendMessage("SetArenaBounds", bossRoomBounds, SendMessageOptions.DontRequireReceiver);
                    boss.SendMessage("WakeUpBoss", SendMessageOptions.DontRequireReceiver);
                }
            }
            Debug.Log("<color=orange>Kết thúc Động đất! Các Boss bắt đầu tấn công.</color>");
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