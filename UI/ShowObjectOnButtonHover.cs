using UnityEngine;
using UnityEngine.EventSystems;

public class ShowObjectOnButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject[] targetObjects;
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool hideOnStart = true;

    private void Awake()
    {
        if (hideOnStart)
        {
            SetTargetsActive(false);
        if (targetObject == null)
        {
            return;
        }

        if (hideOnStart)
        {
            targetObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (!hideOnStart)
        if (targetObject == null || !hideOnStart)
        {
            return;
        }

        SetTargetsActive(false);
        targetObject.SetActive(false);
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
        if (targetObject == null)
        {
            return;
        }

        targetObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetObject == null)
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
        targetObject.SetActive(false);
    }
}
