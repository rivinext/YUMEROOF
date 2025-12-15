using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PNG画像（ImageやSpriteRendererなど）のクリックを検知し、指定したカーブに沿って一回転させるアニメーションを行うコンポーネント。
/// </summary>
public class PngClickRotateAnimator : MonoBehaviour, IPointerClickHandler
{
    [Header("回転対象")]
    [SerializeField] private RectTransform targetRectTransform;

    [Header("回転設定")]
    [SerializeField, Tooltip("1回転にかかる秒数")]
    private float rotationDuration = 0.6f;

    [SerializeField, Tooltip("回転スピードを調整するアニメーションカーブ")]
    private AnimationCurve rotationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private Coroutine rotateCoroutine;

    private void Awake()
    {
        if (targetRectTransform == null)
        {
            targetRectTransform = transform as RectTransform;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetRectTransform == null)
        {
            return;
        }

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
        }

        rotateCoroutine = StartCoroutine(RotateOnce());
    }

    private IEnumerator RotateOnce()
    {
        float startZ = targetRectTransform.localEulerAngles.z;
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            float curveValue = rotationCurve.Evaluate(t);
            float angle = Mathf.Lerp(0f, 360f, curveValue);

            targetRectTransform.localRotation = Quaternion.Euler(0f, 0f, startZ + angle);
            yield return null;
        }

        targetRectTransform.localRotation = Quaternion.Euler(0f, 0f, startZ + 360f);
        rotateCoroutine = null;
    }
}
