using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class LayoutGroupLocaleRefresher : MonoBehaviour
{
    [SerializeField]
    private LayoutGroup targetLayout;

    private RectTransform targetRectTransform;

    protected virtual void Awake()
    {
        CacheTargetLayout();
    }

    protected virtual void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    protected virtual void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    protected virtual void OnValidate()
    {
        CacheTargetLayout();
    }

    protected void CacheTargetLayout()
    {
        if (targetLayout == null)
        {
            targetLayout = GetComponent<LayoutGroup>();
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
