using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a group of wardrobe tab buttons, ensuring that only the selected tab's
/// content is active and updating visual states across the group.
/// </summary>
public class WardrobeTabGroup : MonoBehaviour
{
    [SerializeField] private List<WardrobeTabButton> registeredTabs = new List<WardrobeTabButton>();
    [SerializeField] private WardrobeTabButton defaultTab;

    private WardrobeTabButton selectedTab;

    private void Awake()
    {
        RemoveNullTabs();
    }

    private void OnEnable()
    {
        RemoveNullTabs();
        if (selectedTab == null || !registeredTabs.Contains(selectedTab))
        {
            SelectInitialTab();
        }
        else
        {
            SelectTab(selectedTab);
        }
    }

    private void OnDisable()
    {
        var tabsSnapshot = new List<WardrobeTabButton>(registeredTabs);
        foreach (WardrobeTabButton tab in tabsSnapshot)
        {
            tab?.SetContentActive(false);
        }
    }

    /// <summary>
    /// Registers a tab button with this group.
    /// </summary>
    public void RegisterTab(WardrobeTabButton tab)
    {
        if (tab == null || registeredTabs.Contains(tab))
        {
            return;
        }

        registeredTabs.Add(tab);

        if (selectedTab == null && (defaultTab == null || defaultTab == tab))
        {
            SelectInitialTab();
        }
        else
        {
            tab.SetSelected(tab == selectedTab);
            tab.SetContentActive(tab == selectedTab);
        }
    }

    /// <summary>
    /// Unregisters a tab button from this group.
    /// </summary>
    public void UnregisterTab(WardrobeTabButton tab)
    {
        if (tab == null)
        {
            return;
        }

        registeredTabs.Remove(tab);

        if (selectedTab == tab)
        {
            selectedTab = null;
            SelectInitialTab();
        }
    }

    /// <summary>
    /// Requests selection of a specific tab button.
    /// </summary>
    public void RequestTabSelection(WardrobeTabButton tab)
    {
        if (tab == null || !registeredTabs.Contains(tab))
        {
            return;
        }

        SelectTab(tab);
    }

    /// <summary>
    /// Selects a tab by its wardrobe category.
    /// </summary>
    public void SelectTabByCategory(WardrobeCategory category)
    {
        foreach (WardrobeTabButton tab in registeredTabs)
        {
            if (tab != null && tab.Category == category)
            {
                SelectTab(tab);
                return;
            }
        }
    }

    private void SelectInitialTab()
    {
        WardrobeTabButton tabToSelect = defaultTab != null && registeredTabs.Contains(defaultTab)
            ? defaultTab
            : registeredTabs.Count > 0 ? registeredTabs[0] : null;

        if (tabToSelect != null)
        {
            SelectTab(tabToSelect);
        }
        else
        {
            selectedTab = null;
        }
    }

    private void SelectTab(WardrobeTabButton tab)
    {
        if (tab == null)
        {
            return;
        }

        if (!registeredTabs.Contains(tab))
        {
            registeredTabs.Add(tab);
        }

        selectedTab = tab;

        foreach (WardrobeTabButton button in registeredTabs)
        {
            bool isActiveTab = button == tab;
            if (button != null)
            {
                button.SetSelected(isActiveTab);
                button.SetContentActive(isActiveTab);
            }
        }
    }

    private void RemoveNullTabs()
    {
        registeredTabs.RemoveAll(tab => tab == null);

        WardrobeTabButton[] childTabs = GetComponentsInChildren<WardrobeTabButton>(includeInactive: true);
        foreach (WardrobeTabButton tab in childTabs)
        {
            if (tab != null && !registeredTabs.Contains(tab))
            {
                registeredTabs.Add(tab);
            }
        }
    }
}
