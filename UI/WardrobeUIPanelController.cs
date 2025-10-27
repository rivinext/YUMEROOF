using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WardrobeUIPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private WardrobePanelAnimator panelAnimator;
    [SerializeField] private bool startOpen = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onPanelOpened;
    [SerializeField] private UnityEvent onPanelClosed;

    private bool isPanelOpen;

    private void Awake()
    {
        EnsureAnimatorReference();
        InitializePanelState();
    }

    private void OnEnable()
    {
        EnsureAnimatorReference();
        RegisterToggleButton();
    }

    private void OnDisable()
    {
        UnregisterToggleButton();
    }

    public void OpenPanel()
    {
        EnsureAnimatorReference();

        if (panelAnimator == null || isPanelOpen)
        {
            return;
        }

        panelAnimator.PlayOpen();
        isPanelOpen = true;
        onPanelOpened?.Invoke();
    }

    public void ClosePanel()
    {
        if (panelAnimator == null || !isPanelOpen)
        {
            return;
        }

        panelAnimator.PlayClose();
        isPanelOpen = false;
        onPanelClosed?.Invoke();
    }

    public void TogglePanel()
    {
        if (isPanelOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    private void RegisterToggleButton()
    {
        if (toggleButton == null)
        {
            return;
        }

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

    private void EnsureAnimatorReference()
    {
        if (panelAnimator == null)
        {
            panelAnimator = GetComponent<WardrobePanelAnimator>();
        }
    }

    private void InitializePanelState()
    {
        if (panelAnimator == null)
        {
            isPanelOpen = false;
            return;
        }

        if (startOpen)
        {
            panelAnimator.SnapOpen();
            isPanelOpen = true;
        }
        else
        {
            panelAnimator.SnapClosed();
            isPanelOpen = false;
        }
    }
}
