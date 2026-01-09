using UnityEngine;
using UnityEngine.EventSystems;
using Yume;

public class CameraControlPanelToggleRightClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ScreenshotCapture screenshotCapture;

    private void Awake()
    {
        if (screenshotCapture == null)
        {
            screenshotCapture = FindFirstObjectByType<ScreenshotCapture>();
        }

        if (screenshotCapture == null)
        {
            Debug.LogWarning("ScreenshotCapture not found for CameraControlPanelToggleRightClickHandler.");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        if (screenshotCapture == null)
        {
            Debug.LogWarning("ScreenshotCapture not assigned when right-clicking CameraControlPanel toggle.");
            return;
        }

        screenshotCapture.Capture();
    }
}
