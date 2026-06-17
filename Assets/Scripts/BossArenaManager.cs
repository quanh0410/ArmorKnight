using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class BossArenaManager : MonoBehaviour
{
    [Header("--- LƯU TRỮ TRẠNG THÁI ---")]
    [Tooltip("ID để lưu vào SaveManager (Dùng chung hệ thống với Rương/Cửa).")]
    public string arenaID;

    [Header("--- CÀI ĐẶT ĐẤU TRƯỜNG ---")]
    public GameObject[] doors;
    public Collider2D bossRoomBounds;

    [Header("--- ÂM THANH CHIẾN ĐẬU ---")]
    public string bossMusicTheme = "BossTheme";
    public string mapMusicTheme = "MapTheme";

    [Header("--- CAMERA CINEMATIC ---")]
    public Transform dialogCameraFocus;

    [Header("--- KỊCH BẢN ĐỘNG ĐẤT ---")]
    public float delayBeforeEarthquake = 0.5f;
    public float shakeIntensity = 5f;
    public float earthquakeDuration = 2f;

    [Header("--- THEO DÕI BOSS ---")]
    public EnemyHealth[] bossHealths;
    public GameObject[] bossObjects;

    private bool isBattleActive = false;
    private Collider2D triggerCollider;
    private NPCDialog bossDialog;
    private CinemachineConfiner2D cameraConfiner;
    private Collider2D originalCameraBounds;
    private CinemachineCamera mainCam;
    private Transform originalFollowTarget;

    private void Start()
    {
        triggerCollider = GetComponent<Collider2D>();
        bossDialog = GetComponent<NPCDialog>();
        UnlockDoors();

        if (!string.IsNullOrEmpty(arenaID) && SaveManager.instance != null)
        {
            if (SaveManager.instance.IsObjectInteracted(arenaID))
            {
                DisableArenaPermanently();
            }
        }
    }

    private void DisableArenaPermanently()
    {
        isBattleActive = false;

        if (triggerCollider != null) triggerCollider.enabled = false;

        if (bossObjects != null)
        {
            foreach (GameObject boss in bossObjects)
            {
                if (boss != null) boss.SetActive(false);
            }
        }

        Debug.Log($"<color=gray>Đấu trường {arenaID} đã bị vô hiệu hóa do Boss đã bị tiêu diệt từ trước!</color>");
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

            if (AudioManager.instance != null && !string.IsNullOrEmpty(bossMusicTheme))
            {
                AudioManager.instance.PlayMusic(bossMusicTheme);
            }

            if (bossDialog != null && bossDialog.hasSpokenOnce)
            {
                PrepareArena(panToBoss: false);
                StartCoroutine(EarthquakeAndWakeBossRoutine());
            }
            else
            {
                PrepareArena(panToBoss: true);

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
    }

    private void PrepareArena(bool panToBoss)
    {
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

            mainCam = mainCamObj.GetComponent<CinemachineCamera>();
            if (mainCam != null)
            {
                originalFollowTarget = mainCam.Follow;
                if (panToBoss && dialogCameraFocus != null && bossDialog != null)
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

        if (AudioManager.instance != null && !string.IsNullOrEmpty(mapMusicTheme))
        {
            AudioManager.instance.PlayMusicWithFade(mapMusicTheme);
        }

        if (!string.IsNullOrEmpty(arenaID) && SaveManager.instance != null)
        {
            // Truyền tham số true để lưu vĩnh viễn (Permanent = true)
            SaveManager.instance.SaveObjectState(arenaID, true);
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