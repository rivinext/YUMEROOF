using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColorPanelController : MonoBehaviour
{
    [SerializeField] private PanelScaleAnimator panelAnimator;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;

    private Coroutine closeRoutine;
    public bool IsOpen => panelAnimator != null && panelAnimator.IsOpen;

    private void Awake()
    {
        RegisterWithExclusionManager();
        RegisterOpenButton();
        SnapClosed();
    }

    private void OnEnable()
    {
        RegisterWithExclusionManager();
        RegisterOpenButton();
        SnapClosed();
    }

    private void OnDisable()
    {
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        UnregisterOpenButton();
    }

    public void OpenPanel()
    {
        if (panelAnimator == null)
        {
            return;
        }

        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        if (panelRoot != null && !panelRoot.activeSelf)
        {
            panelRoot.SetActive(true);
        }

        UIPanelExclusionManager.Instance?.NotifyOpened(this);
        panelAnimator.Open();
    }

    public void ClosePanel()
    {
        if (panelAnimator == null)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            return;
        }

        panelAnimator.Close();

        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
        }

        closeRoutine = StartCoroutine(WaitForCloseAnimation());
    }

    public void TogglePanel()
    {
        if (panelAnimator == null)
        {
            return;
        }

        if (panelAnimator.IsOpen)
        {
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            panelAnimator.Toggle();
            closeRoutine = StartCoroutine(WaitForCloseAnimation());
        }
        else
        {
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            if (panelRoot != null && !panelRoot.activeSelf)
            {
                panelRoot.SetActive(true);
            }

            UIPanelExclusionManager.Instance?.NotifyOpened(this);
            panelAnimator.Toggle();
        }
    }

    private void SnapClosed()
    {
        if (panelAnimator != null)
        {
            panelAnimator.SnapClosed();
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private IEnumerator WaitForCloseAnimation()
    {
        yield return new WaitUntil(() => panelAnimator == null || !panelAnimator.IsOpen);

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        closeRoutine = null;
    }

    private void RegisterWithExclusionManager()
    {
        UIPanelExclusionManager.Instance?.Register(this);
    }

    private void RegisterOpenButton()
    {
        if (openButton == null)
        {
            return;
        }

        openButton.onClick.RemoveListener(OnOpenButtonClicked);
        openButton.onClick.AddListener(OnOpenButtonClicked);
    }

    private void UnregisterOpenButton()
    {
        if (openButton == null)
        {
            return;
        }

        openButton.onClick.RemoveListener(OnOpenButtonClicked);
    }

    private void OnOpenButtonClicked()
    {
        TogglePanel();
    }
}
