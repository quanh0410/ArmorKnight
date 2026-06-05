using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameBootstrapper : MonoBehaviour
{
    [Tooltip("Tên Scene Menu chính c?a b?n")]
    public string mainMenuName = "MainMenu";

    private IEnumerator Start()
    {
        // Ki?m tra: N?u game v?a b?t lên và ch? có DUY NH?T Core_Scene ?ang ch?y
        if (SceneManager.sceneCount == 1)
        {
            // T?i Main Menu theo d?ng Additive (T?i thêm vào Core_Scene)
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenuName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone) yield return null;

            // ??i quy?n ?i?u khi?n ?u tiên sang cho Main Menu
            Scene menuScene = SceneManager.GetSceneByName(mainMenuName);
            if (menuScene.IsValid()) SceneManager.SetActiveScene(menuScene);
        }
    }
}