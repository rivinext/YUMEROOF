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
        [Tooltip("If false, the tab remains visible but cannot be selected.")]
        public bool isEnabled = true;
    }

    [SerializeField] private PanelScaleAnimator panelAnimator;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private ToggleGroup tabToggleGroup;
    [SerializeField] private List<TabBinding> tabs = new();
    [SerializeField] private int defaultTabIndex;

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

        var targetBinding = TryGetActiveTabFromToggle(out var toggleTab)
            ? GetBindingForToggle(toggleTab)
            : null;

        if (targetBinding == null)
        {
            targetBinding = GetBindingForToggle(currentTab) ?? GetDefaultBinding();
        }

        SetActiveTab(targetBinding);
    }

    private bool TryGetActiveTabFromToggle(out Toggle toggle)
    {
        foreach (var binding in tabs)
        {
            if (binding?.toggle != null && binding.toggle.isOn)
            {
                if (binding.isEnabled)
                {
                    toggle = binding.toggle;
                    return true;
                }
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

            binding.toggle.interactable = binding.isEnabled;

            var targetToggle = binding.toggle;
            UnityAction<bool> listener = isOn =>
            {
                if (isOn && binding.isEnabled)
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

    public void SwitchTab(Toggle toggle)
    {
        var binding = GetBindingForToggle(toggle);

        if (binding == null || !binding.isEnabled)
        {
            binding = GetDefaultBinding();
        }

        SetActiveTab(binding);
    }

    private void SetActiveTab(TabBinding binding)
    {
        currentTab = binding?.toggle;

        foreach (var currentBinding in tabs)
        {
            if (currentBinding == null)
            {
                continue;
            }

            bool isActive = currentBinding == binding;

            if (currentBinding.tabRoot != null)
            {
                currentBinding.tabRoot.SetActive(isActive);
            }

            if (currentBinding.toggle != null)
            {
                currentBinding.toggle.SetIsOnWithoutNotify(isActive);
            }
        }
    }

    private TabBinding GetBindingForToggle(Toggle toggle)
    {
        foreach (var binding in tabs)
        {
            if (binding?.toggle == toggle)
            {
                return binding;
            }
        }

        return null;
    }

    private TabBinding GetDefaultBinding()
    {
        if (tabs.Count == 0)
        {
            return null;
        }

        int clampedIndex = Mathf.Clamp(defaultTabIndex, 0, tabs.Count - 1);

        for (int offset = 0; offset < tabs.Count; offset++)
        {
            int index = (clampedIndex + offset) % tabs.Count;
            var candidate = tabs[index];

            if (candidate != null && candidate.isEnabled)
            {
                return candidate;
            }
        }

        return null;
    }
}
