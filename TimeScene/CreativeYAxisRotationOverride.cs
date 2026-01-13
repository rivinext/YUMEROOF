using UnityEngine;
using UnityEngine.UI;

public class CreativeYAxisRotationOverride : MonoBehaviour
{
    [SerializeField] private Slider yRotationSlider;
    [SerializeField] private float minDegrees = 0f;
    [SerializeField] private float maxDegrees = 360f;
    [SerializeField, Tooltip("Disable this to allow testing outside creative mode.")] private bool onlyCreativeMode = true;

    private float yDegrees;
    private bool isCreativeSlot;
    private bool isSubscribedToSlotKey;

    private void Reset()
    {
        if (yRotationSlider == null)
            yRotationSlider = GetComponentInChildren<Slider>();
    }

    private void OnEnable()
    {
        if (yRotationSlider != null)
        {
            yRotationSlider.minValue = minDegrees;
            yRotationSlider.maxValue = maxDegrees;
            yRotationSlider.onValueChanged.AddListener(HandleSliderChanged);
            yDegrees = yRotationSlider.value;
        }

        RefreshCreativeSlotState();
        SubscribeToSlotKeyChanged();
    }

    private void OnDisable()
    {
        if (yRotationSlider != null)
        {
            yRotationSlider.onValueChanged.RemoveListener(HandleSliderChanged);
        }

        UnsubscribeFromSlotKeyChanged();
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
        return isCreativeSlot;
    }

    private void SubscribeToSlotKeyChanged()
    {
        if (isSubscribedToSlotKey)
        {
            return;
        }

        var save = SaveGameManager.Instance;
        if (save == null)
        {
            return;
        }

        save.OnSlotKeyChanged -= HandleSlotKeyChanged;
        save.OnSlotKeyChanged += HandleSlotKeyChanged;
        isSubscribedToSlotKey = true;
    }

    private void UnsubscribeFromSlotKeyChanged()
    {
        if (!isSubscribedToSlotKey)
        {
            return;
        }

        var save = SaveGameManager.Instance;
        if (save != null)
        {
            save.OnSlotKeyChanged -= HandleSlotKeyChanged;
        }

        isSubscribedToSlotKey = false;
    }

    private void HandleSlotKeyChanged(string slotKey)
    {
        UpdateCreativeSlotState(slotKey);
    }

    private void RefreshCreativeSlotState()
    {
        UpdateCreativeSlotState(SaveGameManager.Instance?.CurrentSlotKey);
    }

    private void UpdateCreativeSlotState(string slotKey)
    {
        if (string.IsNullOrEmpty(slotKey))
        {
            isCreativeSlot = false;
            return;
        }

        isCreativeSlot = slotKey.StartsWith("Creative", System.StringComparison.OrdinalIgnoreCase);
    }
}
