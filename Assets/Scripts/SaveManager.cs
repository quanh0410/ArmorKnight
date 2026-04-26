using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GameSaveData
{
    public List<string> interactedObjectIDs = new List<string>();
    public string respawnSceneName = "Scene_1";
    public string respawnBenchID = "";

    // --- MỚI: DỮ LIỆU LƯU TRỮ TRANG BỊ & TIỀN ---
    public int totalCoins;
    // --- ĐỔI TÊN THÀNH ID ---
    public List<string> inventoryItemIDs = new List<string>();
    public string equippedWeaponID;
    public List<string> socketedGemIDs = new List<string>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    public GameSaveData currentSaveData = new GameSaveData();
    public List<string> temporaryEnemyDeaths = new List<string>(); // Quái thường

    private string saveFilePath;

    private void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        saveFilePath = Application.persistentDataPath + "/gamesave.json";
        LoadGame();
    }

    public void SaveObjectState(string objectID, bool isPermanent = true)
    {
        if (isPermanent) { if (!currentSaveData.interactedObjectIDs.Contains(objectID)) currentSaveData.interactedObjectIDs.Add(objectID); }
        else { if (!temporaryEnemyDeaths.Contains(objectID)) temporaryEnemyDeaths.Add(objectID); }
    }

    public bool IsObjectInteracted(string objectID)
    {
        return currentSaveData.interactedObjectIDs.Contains(objectID) || temporaryEnemyDeaths.Contains(objectID);
    }

    // Đã đổi int benchID thành string benchID
    public void UpdateCheckpoint(string sceneName, string benchID)
    {
        currentSaveData.respawnSceneName = sceneName;
        currentSaveData.respawnBenchID = benchID;
        ResetNormalEnemies();
        SaveGame();
    }

    public void ResetNormalEnemies() { temporaryEnemyDeaths.Clear(); }

    public void SaveGame()
    {
        currentSaveData.totalCoins = CoinManager.Instance.totalCoins;

        // Lưu ID của đồ trong túi
        currentSaveData.inventoryItemIDs.Clear();
        foreach (var item in InventoryManager.instance.items)
            currentSaveData.inventoryItemIDs.Add(item.itemID); // Lấy itemID, không lấy itemName

        // Lưu ID của vũ khí và ngọc
        if (EquipmentManager.instance.currentWeapon != null)
        {
            currentSaveData.equippedWeaponID = EquipmentManager.instance.currentWeapon.itemID;
            currentSaveData.socketedGemIDs = EquipmentManager.instance.GetSocketedGemIDs();
        }
        else
        {
            currentSaveData.equippedWeaponID = "";
            currentSaveData.socketedGemIDs.Clear();
        }

        string json = JsonUtility.ToJson(currentSaveData, true);
        File.WriteAllText(saveFilePath, json);
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentSaveData = JsonUtility.FromJson<GameSaveData>(json);

            // 3. Phân phát dữ liệu ngược lại cho các Manager (Sau khi vào Scene)
            StartCoroutine(ApplyLoadData());
        }
    }

    private IEnumerator ApplyLoadData()
    {
        yield return new WaitForEndOfFrame();

        CoinManager.Instance.LoadData(currentSaveData.totalCoins);
        InventoryManager.instance.LoadData(currentSaveData.inventoryItemIDs);

        string savedWeaponID = currentSaveData.equippedWeaponID;
        EquipmentData weaponInInventory = null;

        if (!string.IsNullOrEmpty(savedWeaponID))
        {
            foreach (ItemData item in InventoryManager.instance.items)
            {
                if (item is EquipmentData equip && equip.itemID == savedWeaponID)
                {
                    weaponInInventory = equip;
                    break;
                }
            }

            // --- DÒNG THÔNG BÁO LỖI QUAN TRỌNG ---
            if (weaponInInventory == null)
            {
                Debug.LogError($"<color=red>LỖI LOAD TRANG BỊ:</color> Tìm thấy ID '{savedWeaponID}' trong file Save, nhưng không tìm thấy món đồ nào có ID này trong Túi đồ!");
            }
        }

        if (weaponInInventory != null && weaponInInventory.weaponStats != null)
        {
            EquipmentManager.instance.EquipWeapon(weaponInInventory.weaponStats);
            EquipmentManager.instance.LoadGemsFromInventory(currentSaveData.socketedGemIDs);
        }
        else
        {
            EquipmentManager.instance.UnequipWeapon();
        }

        if (InventoryUIManager.instance != null)
        {
            InventoryUIManager.instance.RefreshInventoryFromSave();
        }
    }

    // --- GIAO NHIỆM VỤ RELOAD MAP VÀ HỒI SINH QUÁI ---
    public void ReloadMapFromBench(string sceneName, string targetBenchID, GameObject player)
    {
        StartCoroutine(ReloadRoutine(sceneName, targetBenchID, player));
    }

    private IEnumerator ReloadRoutine(string sceneName, string targetBenchID, GameObject player)
    {
        // 1. Cất Player và Tối màn hình
        player.SetActive(false);
        if (FadeManager.instance != null) yield return StartCoroutine(FadeManager.instance.FadeOut(0.5f));

        // 2. Tự tin xóa Map
        AsyncOperation unload = SceneManager.UnloadSceneAsync(sceneName);
        while (!unload.isDone) yield return null;

        // 3. Tải lại Map mới
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loadedScene);

        // 4. Tìm kiếm cô lập Ghế đá trong Map vừa load
        bool foundBench = false;
        GameObject[] rootObjects = loadedScene.GetRootGameObjects();

        foreach (GameObject root in rootObjects)
        {
            Checkpoint[] benchesInScene = root.GetComponentsInChildren<Checkpoint>(true);
            foreach (Checkpoint bench in benchesInScene)
            {
                if (bench.benchID == targetBenchID)
                {
                    player.transform.position = bench.transform.position;
                    foundBench = true;
                    break;
                }
            }
            if (foundBench) break;
        }

        // 5. Bơm máu, bật Player và Mở màn hình
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null) health.FullHeal();

        player.SetActive(true);
        if (InteractionUI.instance != null) InteractionUI.instance.Hide();

        if (FadeManager.instance != null) yield return StartCoroutine(FadeManager.instance.FadeIn(0.5f));
    }

    public ItemData GetItemFromResources(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return null;

        ItemData loadedItem = Resources.Load<ItemData>(itemID);
        if (loadedItem == null) Debug.LogError($"<color=red>LỖI LOAD ĐỒ:</color> Không tìm thấy file <b>{itemID}.asset</b> trong Resources!");

        return loadedItem;
    }

    [ContextMenu("🔥 Xóa Dữ Liệu (Clear Save)")]
    public void ClearSaveData()
    {
        string path = Application.persistentDataPath + "/gamesave.json";
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("<color=red><b>ĐÃ XÓA FILE SAVE TRÊN Ổ CỨNG TẠI: " + path + "</b></color>");
        }
        else Debug.Log("<color=yellow>Không tìm thấy file save nào để xóa.</color>");

        currentSaveData = new GameSaveData();
        temporaryEnemyDeaths.Clear();
        Debug.Log("<color=green><b>ĐÃ RESET TOÀN BỘ! Bấm Play để chơi như mới.</b></color>");
    }
}
