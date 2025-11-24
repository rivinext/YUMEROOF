using System.Collections;
using UnityEngine;

public class ColorPanelController : MonoBehaviour
{
    [SerializeField] private PanelScaleAnimator panelAnimator;
    [SerializeField] private GameObject panelRoot;

    private Coroutine closeRoutine;

    private void Awake()
    {
        SnapClosed();
    }

    private void OnEnable()
    {
        SnapClosed();
    }

    private void OnDisable()
    {
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }
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
}
