using UnityEngine;
using UnityEngine.EventSystems;

public class ShowObjectOnButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool hideOnStart = true;

    private void Awake()
    {
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
        if (targetObject == null || !hideOnStart)
        {
            return;
        }

        targetObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
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

        targetObject.SetActive(false);
    }
}
