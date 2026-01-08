using UnityEngine;
using UnityEngine.EventSystems;

public class ShowObjectOnButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject[] targetObjects;
    [SerializeField] private bool hideOnStart = true;

    private void Awake()
    {
        if (hideOnStart)
        {
            SetTargetsActive(false);
        }
    }

    private void OnDisable()
    {
        if (!hideOnStart)
        {
            return;
        }

        SetTargetsActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetTargetsActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetTargetsActive(false);
    }

    private void SetTargetsActive(bool isActive)
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            return;
        }

        for (int i = 0; i < targetObjects.Length; i++)
        {
            GameObject targetObject = targetObjects[i];
            if (targetObject == null)
            {
                continue;
            }

            targetObject.SetActive(isActive);
        }
    }
}
