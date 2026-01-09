using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class VerticalLayoutLocaleRefresher : MonoBehaviour
{
    [SerializeField]
    private VerticalLayoutGroup targetLayout;

    private void Awake()
    {
        CacheTargetLayout();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    private void OnValidate()
    {
        CacheTargetLayout();
    }

    private void CacheTargetLayout()
    {
        if (targetLayout == null)
        {
            targetLayout = GetComponent<VerticalLayoutGroup>();
        }
    }

    private void HandleLocaleChanged(Locale locale)
    {
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        if (!gameObject.activeInHierarchy || targetLayout == null)
        {
            return;
        }

        StartCoroutine(RefreshLayoutRoutine());
    }

    private IEnumerator RefreshLayoutRoutine()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(targetLayout.GetComponent<RectTransform>());
    }
}
