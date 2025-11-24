using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SettingsPanelAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private PanelScaleAnimator panelScaleAnimator;

    [Header("Control")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private bool startOpen = false;

    private void Awake()
    {
        if (panelScaleAnimator == null)
        {
            panelScaleAnimator = GetComponent<PanelScaleAnimator>();
        }

        UIPanelExclusionManager.Instance?.Register(this);

        if (panelScaleAnimator != null)
        {
            if (startOpen)
            {
                panelScaleAnimator.SnapOpen();
            }
            else
            {
                panelScaleAnimator.SnapClosed();
            }
        }

        RegisterToggleButton();
    }

    private void OnEnable()
    {
        RegisterToggleButton();
    }

    private void OnDisable()
    {
        UnregisterToggleButton();
    }

    public void TogglePanel()
    {
        if (panelScaleAnimator == null)
        {
            return;
        }

        if (panelScaleAnimator.IsOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    public bool IsOpen => panelScaleAnimator != null && panelScaleAnimator.IsOpen;

    public void OpenPanel()
    {
        if (panelScaleAnimator == null)
        {
            return;
        }

        UIPanelExclusionManager.Instance?.NotifyOpened(this);
        panelScaleAnimator.Open();
    }

    public void ClosePanel()
    {
        if (panelScaleAnimator == null)
        {
            return;
        }

        panelScaleAnimator.Close();
    }

    public void SnapOpen()
    {
        panelScaleAnimator?.SnapOpen();
    }

    public void SnapClosed()
    {
        panelScaleAnimator?.SnapClosed();
    }

    private void RegisterToggleButton()
    {
        if (toggleButton == null)
        {
            return;
        }

        toggleButton.onClick.RemoveListener(TogglePanel);
        toggleButton.onClick.AddListener(TogglePanel);
    }

    private void UnregisterToggleButton()
    {
        if (toggleButton == null)
        {
            return;
        }

        toggleButton.onClick.RemoveListener(TogglePanel);
    }
}
