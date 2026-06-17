using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameBootstrapper : MonoBehaviour
{
    [Tooltip("Tên Scene Menu chính c?a b?n")]
    public string mainMenuName = "MainMenu";

    private IEnumerator Start()
    {
        if (SceneManager.sceneCount == 1)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenuName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone) yield return null;

            Scene menuScene = SceneManager.GetSceneByName(mainMenuName);
            if (menuScene.IsValid()) SceneManager.SetActiveScene(menuScene);
        }
    }
}