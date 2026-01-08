using UnityEngine;
using UnityEngine.EventSystems;

public class ShowObjectOnButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject[] targetObjects;
    [SerializeField] private bool hideOnStart = true;

    private void Awake()
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            return;
        }

        if (hideOnStart)
        {
            SetTargetsActive(false);
        }
    }

    private void OnDisable()
    {
        if (targetObjects == null || targetObjects.Length == 0 || !hideOnStart)
        {
            return;
        }

        SetTargetsActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            return;
        }

        SetTargetsActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            return;
        }

        SetTargetsActive(false);
    }

    private void SetTargetsActive(bool isActive)
    {
        foreach (var targetObject in targetObjects)
        {
            if (targetObject == null)
            {
                continue;
            }

            targetObject.SetActive(isActive);
        }
    }
}
