using UnityEngine;
using UnityEngine.EventSystems;

public class UIRotateOnClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Rotation Settings")]
    public float duration = 0.5f;
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public Vector3 rotationAxis = new Vector3(0, 0, 1);

    [Header("Direction")]
    public bool reverse = false;

    private bool isRotating;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isRotating)
            StartCoroutine(RotateOnce());
    }

    private System.Collections.IEnumerator RotateOnce()
    {
        isRotating = true;

        float elapsed = 0f;
        Vector3 startEuler = rectTransform.localEulerAngles;
        Vector3 axis = rotationAxis.normalized;
        float direction = reverse ? -1f : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = rotationCurve.Evaluate(t);

            float angle = 360f * curvedT * direction;
            rectTransform.localEulerAngles = startEuler + axis * angle;

            yield return null;
        }

        rectTransform.localEulerAngles =
            startEuler + axis * 360f * direction;

        isRotating = false;
    }
}
