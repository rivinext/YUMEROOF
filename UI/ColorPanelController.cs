using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ColorPanelController : MonoBehaviour
{
    [System.Serializable]
    public class TabBinding
    {
        public Toggle toggle;
        public GameObject tabRoot;
    }

    [SerializeField] private PanelScaleAnimator panelAnimator;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private ToggleGroup tabToggleGroup;
    [SerializeField] private List<TabBinding> tabs = new();

    private Coroutine closeRoutine;
    private readonly Dictionary<Toggle, UnityAction<bool>> tabToggleListeners = new();
    private Toggle currentTab;
    public bool IsOpen => panelAnimator != null && panelAnimator.IsOpen;

    private void Awake()
    {
        RegisterWithExclusionManager();
        RegisterOpenButton();
        InitializeTabs();
        SnapClosed();
    }

    private void OnEnable()
    {
        RegisterWithExclusionManager();
        RegisterOpenButton();
        InitializeTabs();
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
        RemoveTabListeners();
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

        SwitchTab(currentTab);
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

            SwitchTab(currentTab);
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

    private void InitializeTabs()
    {
        SetupTabListeners();

        Toggle targetTab = currentTab;

        if (TryGetActiveTabFromToggle(out var toggleTab))
        {
            targetTab = toggleTab;
        }
        else if (!HasBinding(targetTab))
        {
            targetTab = tabs.Count > 0 ? tabs[0].toggle : currentTab;
        }

        SwitchTab(targetTab);
    }

    private bool TryGetActiveTabFromToggle(out Toggle toggle)
    {
        foreach (var binding in tabs)
        {
            if (binding?.toggle != null && binding.toggle.isOn)
            {
                toggle = binding.toggle;
                return true;
            }
        }

        toggle = default;
        return false;
    }

    private void SetupTabListeners()
    {
        RemoveTabListeners();

        foreach (var binding in tabs)
        {
            if (binding == null || binding.toggle == null)
            {
                continue;
            }

            if (tabToggleGroup != null)
            {
                binding.toggle.group = tabToggleGroup;
            }

            var targetToggle = binding.toggle;
            UnityAction<bool> listener = isOn =>
            {
                if (isOn)
                {
                    SwitchTab(targetToggle);
                }
            };

            tabToggleListeners[binding.toggle] = listener;
            binding.toggle.onValueChanged.AddListener(listener);
        }
    }

    private void RemoveTabListeners()
    {
        foreach (var pair in tabToggleListeners)
        {
            if (pair.Key != null)
            {
                pair.Key.onValueChanged.RemoveListener(pair.Value);
            }
        }

        tabToggleListeners.Clear();
    }

    private bool HasBinding(Toggle toggle)
    {
        foreach (var binding in tabs)
        {
            if (binding == null)
            {
                continue;
            }

            if (binding.toggle == toggle)
            {
                return true;
            }
        }

        return false;
    }

    public void SwitchTab(Toggle toggle)
    {
        currentTab = toggle;

        foreach (var binding in tabs)
        {
            if (binding == null)
            {
                continue;
            }

            bool isActive = binding.toggle == toggle;

            if (binding.tabRoot != null)
            {
                binding.tabRoot.SetActive(isActive);
            }

            if (binding.toggle != null)
            {
                binding.toggle.SetIsOnWithoutNotify(isActive);
            }
        }
    }
}
