using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CinematicBossManager : MonoBehaviour
{
    [Header("--- LƯU TRẠNG THÁI (SAVE/LOAD) ---")]
    public string bossDefeatedSaveID;

    [Header("--- THÀNH PHẦN KẾT NỐI ---")]
    public GameObject[] doors;
    public KnightEnemy bossLogic;

    [Header("--- KỊCH BẢN ---")]
    public NPCDialog dialogPhase1;
    public NPCDialog dialogPhase2;
    public List<StoryFrame> fakeDeathStory;


    private PlayerHealth playerHealth;
    private GameObject playerRef;
    private PlayerController playerController;
    private bool isEncounterStarted = false;

    private void Start()
    {
        if (dialogPhase2 != null) dialogPhase2.gameObject.SetActive(false);
        FindPlayerAutomatically();

        if (SaveManager.instance != null && !string.IsNullOrEmpty(bossDefeatedSaveID))
        {
            if (SaveManager.instance.IsObjectInteracted(bossDefeatedSaveID))
            {
                isEncounterStarted = true;
                UnlockDoors();

                if (bossLogic != null) bossLogic.TransformIntoDoor();

                Collider2D trigger = GetComponent<Collider2D>();
                if (trigger != null) trigger.enabled = false;
            }
        }
    }

    private void FindPlayerAutomatically()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerRef = playerObj;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerController = playerObj.GetComponent<PlayerController>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isEncounterStarted)
        {
            StartEncounter();
        }
    }

    private void StartEncounter()
    {
        isEncounterStarted = true;
        LockDoors();

        if (playerHealth == null) FindPlayerAutomatically();

        if (dialogPhase1 != null)
        {
            dialogPhase1.isManagedByQuest = true;
            dialogPhase1.TriggerDialog();
        }
    }

    public void StartFight()
    {
        if (bossLogic != null) bossLogic.WakeUpBoss();

        if (playerHealth != null)
        {
            playerHealth.hasPlotArmor = true;
            playerHealth.onPlotArmorTriggered.RemoveListener(OnPlayerFatalBlow);
            playerHealth.onPlotArmorTriggered.AddListener(OnPlayerFatalBlow);
        }
    }

    public void OnPlayerFatalBlow()
    {
        if (playerHealth != null) playerHealth.hasPlotArmor = false;
        if (bossLogic != null) bossLogic.PacifyBoss();

        if (playerController != null)
        {
            playerController.InterruptDashAndActions();
            playerController.isInputLocked = true;
            Rigidbody2D rb = playerRef.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        if (StoryManager.instance != null)
        {
            StoryManager.instance.PlayStory(fakeDeathStory);
            StartCoroutine(WaitForStoryToEnd());
        }
    }

    private IEnumerator WaitForStoryToEnd()
    {
        yield return null;
        while (Time.timeScale == 0f) yield return null;

        if (playerController != null) playerController.isInputLocked = false;

        if (dialogPhase2 != null)
        {
            dialogPhase2.gameObject.SetActive(true);
            dialogPhase2.isManagedByQuest = true;
            dialogPhase2.TriggerDialog();
        }
    }

    public void EndCinematicAndTransformBoss()
    {
        UnlockDoors();

        if (bossLogic != null) bossLogic.TransformIntoDoor();

        if (SaveManager.instance != null && !string.IsNullOrEmpty(bossDefeatedSaveID))
        {
            SaveManager.instance.SaveObjectState(bossDefeatedSaveID, true);
            SaveManager.instance.SaveGame();
        }
    }

    private void LockDoors() { foreach (var d in doors) if (d != null) d.SetActive(true); }
    private void UnlockDoors() { foreach (var d in doors) if (d != null) d.SetActive(false); }
}