using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class HorizontalLayoutLocaleRefresher : MonoBehaviour
{
    [SerializeField]
    private HorizontalLayoutGroup targetLayout;

    private RectTransform targetRectTransform;

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
            targetLayout = GetComponent<HorizontalLayoutGroup>();
        }

        targetRectTransform = targetLayout != null ? targetLayout.GetComponent<RectTransform>() : null;
    }

    private void HandleLocaleChanged(Locale locale)
    {
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        if (!gameObject.activeInHierarchy || targetRectTransform == null)
        {
            return;
        }

        StartCoroutine(RefreshLayoutRoutine());
    }

    private IEnumerator RefreshLayoutRoutine()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(targetRectTransform);
    }
}
