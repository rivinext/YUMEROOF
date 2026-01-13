using UnityEngine;
using UnityEngine.UI;

public class CreativeYAxisRotationOverride : MonoBehaviour
{
    [SerializeField] private Slider yRotationSlider;
    [SerializeField] private float minDegrees = 0f;
    [SerializeField] private float maxDegrees = 360f;
    [SerializeField] private bool onlyCreativeMode = true;

    private float yDegrees;

    private void OnEnable()
    {
        if (yRotationSlider != null)
        {
            yRotationSlider.minValue = minDegrees;
            yRotationSlider.maxValue = maxDegrees;
            yRotationSlider.onValueChanged.AddListener(HandleSliderChanged);
            yDegrees = yRotationSlider.value;
        }
    }

    private void OnDisable()
    {
        if (yRotationSlider != null)
        {
            yRotationSlider.onValueChanged.RemoveListener(HandleSliderChanged);
        }
    }

    private void HandleSliderChanged(float value)
    {
        yDegrees = value;
    }

    private void LateUpdate()
    {
        if (onlyCreativeMode && !IsCreativeMode())
            return;

        Vector3 current = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(current.x, yDegrees, current.z);
    }

    private bool IsCreativeMode()
    {
        var save = SaveGameManager.Instance;
        if (save == null || string.IsNullOrEmpty(save.CurrentSlotKey))
            return false;

        return save.CurrentSlotKey.StartsWith("Creative", System.StringComparison.OrdinalIgnoreCase);
    }
}
