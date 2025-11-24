using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildingGhostDialogueInitializer
{
    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        if (subscribed)
            return;

        SceneManager.sceneLoaded += HandleSceneLoaded;
        subscribed = true;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuildingGhostDialogueManager.ValidateSceneConfiguration();
    }
}
