using UnityEngine;
using UnityEngine.UI;

public class FullscreenToggle : MonoBehaviour
{
    [SerializeField] Toggle toggle;

    void Start()
    {
        if (!toggle)
        {
            toggle = GetComponent<Toggle>();
        }

        bool isFull = PlayerPrefs.GetInt("fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreenMode = isFull ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreen = isFull;
        toggle.isOn = Screen.fullScreen;
        toggle.onValueChanged.AddListener(SetFullscreen);
    }

    public void SetFullscreen(bool isFull)
    {
        Screen.fullScreenMode = isFull ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreen = isFull;
        PlayerPrefs.SetInt("fullscreen", isFull ? 1 : 0);
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}
