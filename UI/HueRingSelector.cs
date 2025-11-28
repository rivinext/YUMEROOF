using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class HueRingSelector : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private RectTransform ringRectTransform;
    [SerializeField] private RectTransform handleRectTransform;
    [SerializeField] private Graphic ringGraphic;

    [SerializeField] private UnityEvent<float> onHueChanged = new UnityEvent<float>();

    private float hue;

    public UnityEvent<float> OnHueChanged => onHueChanged;

    private RectTransform RectTransformCache => (RectTransform)transform;

    private void Awake()
    {
        if (ringRectTransform == null)
        {
            ringRectTransform = RectTransformCache;
        }

        if (ringGraphic == null)
        {
            ringGraphic = GetComponent<Graphic>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateHueFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateHueFromPointer(eventData);
    }

    public void SetHue(float newHue)
    {
        hue = Mathf.Repeat(newHue, 1f);
        UpdateHandlePosition();
    }

    private void UpdateHueFromPointer(PointerEventData eventData)
    {
        if (ringRectTransform == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(ringRectTransform, eventData.position,
                eventData.pressEventCamera, out var localPoint))
        {
            return;
        }

        float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
        hue = Mathf.Repeat(angle / 360f, 1f);
        UpdateHandlePosition();
        onHueChanged.Invoke(hue);
    }

    private void UpdateHandlePosition()
    {
        if (handleRectTransform == null || ringRectTransform == null)
        {
            return;
        }

        float angle = hue * 360f * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float radius = Mathf.Min(ringRectTransform.rect.width, ringRectTransform.rect.height) * 0.5f;
        handleRectTransform.anchoredPosition = direction * radius;
    }
}
