using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPersistence : MonoBehaviour
{
    private static PlayerPersistence instance;

    private GameObject rootObject;

    private void Awake()
    {
        rootObject = transform.root.gameObject;

        // 既存のプレイヤーが存在する場合は、MainMenu内にあるプレイヤーを優先的に置き換える
        if (instance != null && instance != this)
        {
            if (instance.ShouldBeReplacedBy(this))
            {
                instance.DisposeAndDestroy();
            }
            else
            {
                // こちらは不要なインスタンスなので破棄
                Destroy(rootObject);
                return;
            }
        }

        instance = this;

        MoveRootToDontDestroyOnLoad();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void MoveRootToDontDestroyOnLoad()
    {
        if (rootObject != null && rootObject.scene.name != "DontDestroyOnLoad")
        {
            DontDestroyOnLoad(rootObject);
        }
    }

    private bool ShouldBeReplacedBy(PlayerPersistence candidate)
    {
        if (candidate == null || candidate.rootObject == null)
            return false;

        string currentScene = rootObject != null ? rootObject.scene.name : string.Empty;
        string candidateScene = candidate.rootObject.scene.name;

        // MainMenu上のプレイヤーはゲーム用のプレイヤーに置き換える
        if (currentScene == "MainMenu" && candidateScene != "MainMenu")
        {
            return true;
        }

        // DontDestroyOnLoad内にいる方を優先する
        if (currentScene != "DontDestroyOnLoad" && candidateScene == "DontDestroyOnLoad")
        {
            return true;
        }

        return false;
    }

    private void DisposeAndDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (rootObject != null)
        {
            Destroy(rootObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            if (rootObject != null)
            {
                Destroy(rootObject);
            }
        }
        else
        {
            // 別シーン読み込み時にDontDestroyOnLoadに移動しているか念のため再チェック
            MoveRootToDontDestroyOnLoad();
        }
    }
}
