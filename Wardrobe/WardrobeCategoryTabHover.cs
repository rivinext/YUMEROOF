using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class WardrobeCategoryTabHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject hoverTarget;
    [SerializeField] private TMP_Text hoverText;

    private string categoryLabel = string.Empty;

    public void Configure(string label, GameObject targetOverride, TMP_Text textOverride)
    {
        categoryLabel = label ?? string.Empty;

        if (targetOverride != null)
        {
            hoverTarget = targetOverride;
        }

        if (textOverride != null)
        {
            hoverText = textOverride;
        }

        UpdateHoverText();
        SetHoverTargetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHoverTargetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverTargetActive(false);
    }

    private void OnDisable()
    {
        SetHoverTargetActive(false);
    }

    private void SetHoverTargetActive(bool isActive)
    {
        if (hoverTarget != null)
        {
            hoverTarget.SetActive(isActive);
        }
    }

    private void UpdateHoverText()
    {
        if (hoverText == null && hoverTarget != null)
        {
            hoverText = hoverTarget.GetComponentInChildren<TMP_Text>(true);
        }

        if (hoverText != null)
        {
            hoverText.text = categoryLabel;
        }
    }
}
