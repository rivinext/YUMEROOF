using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ColorPanelController : MonoBehaviour
{
    public enum TabType
    {
        Primary,
        Secondary,
        Tertiary,
        Quaternary
    }

    [System.Serializable]
    public class TabBinding
    {
        public TabType type;
        public Toggle toggle;
        public GameObject tabRoot;
    }

    [SerializeField] private PanelScaleAnimator panelAnimator;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private ToggleGroup tabToggleGroup;
    [SerializeField] private List<TabBinding> tabs = new();
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = Color.gray;
    [SerializeField] private TabType initialTab = TabType.Primary;

    private Coroutine closeRoutine;
    private readonly Dictionary<Toggle, UnityAction<bool>> tabToggleListeners = new();
    private TabType currentTab;
    public bool IsOpen => panelAnimator != null && panelAnimator.IsOpen;

    private void Awake()
    {
        RegisterWithExclusionManager();
        RegisterOpenButton();
        InitializeTabs(resetToDefault: true);
        SnapClosed();
    }

    private void OnEnable()
    {
        RegisterWithExclusionManager();
        RegisterOpenButton();
        InitializeTabs(resetToDefault: false);
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

    private void InitializeTabs(bool resetToDefault)
    {
        SetupTabListeners();

        if (resetToDefault || !HasBinding(currentTab))
        {
            currentTab = ResolveInitialTab();
        }

        SwitchTab(currentTab);
    }

    private TabType ResolveInitialTab()
    {
        foreach (var binding in tabs)
        {
            if (binding == null)
            {
                continue;
            }

            if (binding.type == initialTab)
            {
                return binding.type;
            }
        }

        return tabs.Count > 0 ? tabs[0].type : initialTab;
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

            var targetType = binding.type;
            UnityAction<bool> listener = isOn =>
            {
                if (isOn)
                {
                    SwitchTab(targetType);
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

    private bool HasBinding(TabType type)
    {
        foreach (var binding in tabs)
        {
            if (binding == null)
            {
                continue;
            }

            if (binding.type == type)
            {
                return true;
            }
        }

        return false;
    }

    public void SwitchTab(TabType type)
    {
        currentTab = type;

        foreach (var binding in tabs)
        {
            if (binding == null)
            {
                continue;
            }

            bool isActive = binding.type == type;

            if (binding.tabRoot != null)
            {
                binding.tabRoot.SetActive(isActive);
            }

            if (binding.toggle != null)
            {
                binding.toggle.SetIsOnWithoutNotify(isActive);

                if (binding.toggle.graphic != null)
                {
                    binding.toggle.graphic.color = isActive ? activeTabColor : inactiveTabColor;
                }
            }
        }
    }
}
