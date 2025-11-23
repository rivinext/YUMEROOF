using UnityEngine;
using System.Collections;

public class FloatingItemCard : MonoBehaviour
{
    [SerializeField] private float tiltAngle = 5f;
    [SerializeField] private float tiltSpeed = 1f;
    [SerializeField] private float moveAmount = 10f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] [Range(0f, 2f * Mathf.PI)] private float phaseRange = Mathf.PI * 2f;

    private RectTransform rectTransform;
    private Vector2 basePosition;
    private Quaternion baseRotation;
    private float phase;

    private void Awake()
    {
        phase = phaseRange > 0f ? Random.Range(0f, phaseRange) : 0f;
    }

    private void OnEnable()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        yield return null;
        rectTransform = GetComponent<RectTransform>();
        basePosition = rectTransform.anchoredPosition;
        baseRotation = rectTransform.localRotation;
    }

    private void Update()
    {
        if (rectTransform == null) return;
        float t = Time.time + phase;
        float tilt = Mathf.Sin(t * tiltSpeed) * tiltAngle;
        rectTransform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, tilt);
        Vector2 pos = basePosition;
        pos.y = basePosition.y + Mathf.Sin(t * moveSpeed) * moveAmount;
        rectTransform.anchoredPosition = pos;
    }
}
