using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ゲームを終了するボタン動作を提供する
/// </summary>
[RequireComponent(typeof(Button))]
public class ExitGameButton : MonoBehaviour
{
    private Button exitButton;

    private void Awake()
    {
        exitButton = GetComponent<Button>();

        if (exitButton == null)
        {
            Debug.LogError("ExitGameButton requires a Button component.", this);
            enabled = false;
            return;
        }

        exitButton.onClick.AddListener(HandleExitRequested);
    }

    private void OnDestroy()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(HandleExitRequested);
        }
    }

    private static void HandleExitRequested()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
