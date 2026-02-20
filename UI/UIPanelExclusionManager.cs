using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures that key UI panels remain mutually exclusive by closing the others
/// when one panel is opened.
/// </summary>
public class UIPanelExclusionManager : MonoBehaviour
{
    private interface IUIPanelHandle
    {
        UnityEngine.Object Source { get; }
        string Name { get; }
        bool IsOpen { get; }
        void Close();
    }

    private sealed class UIPanelHandle<TPanel> : IUIPanelHandle where TPanel : UnityEngine.Object
    {
        private readonly TPanel panel;
        private readonly Func<TPanel, bool> isOpen;
        private readonly Action<TPanel> close;

        public UIPanelHandle(TPanel panel, Func<TPanel, bool> isOpen, Action<TPanel> close)
        {
            this.panel = panel;
            this.isOpen = isOpen;
            this.close = close;
        }

        public UnityEngine.Object Source => panel;
        public string Name => panel.GetType().Name;
        public bool IsOpen => panel != null && isOpen(panel);

        public void Close()
        {
            if (panel != null)
            {
                close(panel);
            }
        }
    }

    private static UIPanelExclusionManager instance;

    public static UIPanelExclusionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIPanelExclusionManager>();
            }

            return instance;
        }
    }

    private readonly List<IUIPanelHandle> panelHandles = new();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterScenePanels();
        CloseAllRegisteredPanels();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HashSet<string> closedPanelNames = new();
        CloseAllRegisteredPanels(closedPanelNames);
        RegisterScenePanels();
        CloseAllRegisteredPanels(closedPanelNames);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[UIPanelExclusionManager] Closed panels on scene load: {(closedPanelNames.Count == 0 ? "none" : string.Join(", ", closedPanelNames))}");
#endif
    }

    public void Register(SettingsPanelAnimator panel)
    {
        RegisterSingle(panel, value => value.IsOpen, value => value.ClosePanel());
    }

    public void Register(CameraControlPanel panel)
    {
        RegisterSingle(panel, value => value.IsOpen, value => value.ClosePanel());
    }

    public void Register(MilestonePanel panel)
    {
        RegisterSingle(panel, value => value.IsOpen, value => value.ClosePanel());
    }

    public void Register(InventoryUI panel)
    {
        RegisterSingle(panel, value => value.IsOpen, value => value.CloseInventory());
    }

    public void Register(WardrobeUIController panel)
    {
        RegisterSingle(panel, value => value.IsShown, value => value.HidePanel());
    }

    public void Register(ColorPanelController panel)
    {
        RegisterSingle(panel, value => value.IsOpen, value => value.ClosePanel());
    }

    private void RegisterScenePanels()
    {
        panelHandles.Clear();
        RegisterAll(FindObjectsByType<SettingsPanelAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None), value => value.IsOpen, value => value.ClosePanel());
        RegisterAll(FindObjectsByType<CameraControlPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None), value => value.IsOpen, value => value.ClosePanel());
        RegisterAll(FindObjectsByType<MilestonePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None), value => value.IsOpen, value => value.ClosePanel());
        RegisterAll(FindObjectsByType<InventoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None), value => value.IsOpen, value => value.CloseInventory());
        RegisterAll(FindObjectsByType<WardrobeUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None), value => value.IsShown, value => value.HidePanel());
        RegisterAll(FindObjectsByType<ColorPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None), value => value.IsOpen, value => value.ClosePanel());
    }

    private void RegisterAll<TPanel>(IEnumerable<TPanel> panels, Func<TPanel, bool> isOpen, Action<TPanel> close) where TPanel : UnityEngine.Object
    {
        foreach (TPanel panel in panels)
        {
            RegisterSingle(panel, isOpen, close);
        }
    }

    private void RegisterSingle<TPanel>(TPanel panel, Func<TPanel, bool> isOpen, Action<TPanel> close) where TPanel : UnityEngine.Object
    {
        if (panel == null)
        {
            return;
        }

        for (int i = panelHandles.Count - 1; i >= 0; i--)
        {
            if (panelHandles[i].Source == null)
            {
                panelHandles.RemoveAt(i);
            }
            else if (panelHandles[i].Source == panel)
            {
                return;
            }
        }

        panelHandles.Add(new UIPanelHandle<TPanel>(panel, isOpen, close));
    }

    public void NotifyOpened(SettingsPanelAnimator panel)
    {
        RegisterScenePanels();
        CloseOtherPanels(panel);
    }

    public void NotifyOpened(CameraControlPanel panel)
    {
        RegisterScenePanels();
        CloseOtherPanels(panel);
    }

    public void NotifyOpened(MilestonePanel panel)
    {
        RegisterScenePanels();
        CloseOtherPanels(panel);
    }

    public void NotifyOpened(InventoryUI panel)
    {
        RegisterScenePanels();
        CloseOtherPanels(panel);
    }

    public void NotifyOpened(WardrobeUIController panel)
    {
        RegisterScenePanels();
        CloseOtherPanels(panel);
    }

    public void NotifyOpened(ColorPanelController panel)
    {
        RegisterScenePanels();
        CloseOtherPanels(panel);
    }

    private void CloseOtherPanels(UnityEngine.Object openedPanel)
    {
        for (int i = panelHandles.Count - 1; i >= 0; i--)
        {
            IUIPanelHandle panelHandle = panelHandles[i];
            if (panelHandle.Source == null)
            {
                panelHandles.RemoveAt(i);
                continue;
            }

            if (panelHandle.Source != openedPanel && panelHandle.IsOpen)
            {
                panelHandle.Close();
            }
        }
    }

    private void CloseAllRegisteredPanels(ISet<string> closedPanelNames = null)
    {
        for (int i = panelHandles.Count - 1; i >= 0; i--)
        {
            IUIPanelHandle panelHandle = panelHandles[i];
            if (panelHandle.Source == null)
            {
                panelHandles.RemoveAt(i);
                continue;
            }

            if (panelHandle.IsOpen)
            {
                closedPanelNames?.Add(panelHandle.Name);
            }

            panelHandle.Close();
        }
    }
}
