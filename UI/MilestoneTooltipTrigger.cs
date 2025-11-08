using UnityEngine;
using UnityEngine.EventSystems;

public class MilestoneTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private MilestonePanel milestonePanel;
    private int milestoneIndex;
    private RectTransform localRectTransform;

    public void Initialize(MilestonePanel panel, int index)
    {
        milestonePanel = panel;
        milestoneIndex = index;
        localRectTransform = transform as RectTransform;
        if (localRectTransform == null)
        {
            localRectTransform = GetComponent<RectTransform>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        milestonePanel?.ShowMilestoneTooltip(milestoneIndex, localRectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        milestonePanel?.HideMilestoneTooltip();
    }

    void OnDisable()
    {
        milestonePanel?.HideMilestoneTooltip();
    }
}
