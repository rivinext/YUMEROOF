using UnityEngine;
using UnityEngine.UI;
using Yume;

[RequireComponent(typeof(Button))]
public class ScreenshotButton : MonoBehaviour
{
    [SerializeField] private ScreenshotCapture screenshotCapture;

    private void Awake()
    {
        var button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("ScreenshotButton requires a Button component.");
            return;
        }

        if (screenshotCapture == null)
        {
            screenshotCapture = FindObjectOfType<ScreenshotCapture>();
        }

        if (screenshotCapture != null)
        {
            button.onClick.AddListener(screenshotCapture.Capture);
        }
        else
        {
            Debug.LogError("ScreenshotCapture not found for ScreenshotButton.");
        }
    }
}
