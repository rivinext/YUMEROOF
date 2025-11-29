using UnityEngine;
using UnityEngine.UI;

public class MaterialHuePresetPanel : MonoBehaviour
{
    [SerializeField] private MaterialHuePresetManager presetManager;
    [SerializeField] private Text currentSlotLabel;
    [SerializeField] private MaterialHuePresetSlotButton[] slotButtons;

    private void Awake()
    {
        RegisterCallbacks();
        RefreshUI();
    }

    private void OnEnable()
    {
        RegisterCallbacks();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        UnregisterCallbacks();

        if (presetManager == null)
        {
            return;
        }

        presetManager.OnCurrentSlotChanged.AddListener(UpdateCurrentSlotLabel);
        presetManager.OnPresetsChanged.AddListener(RefreshSlotButtons);
    }

    private void UnregisterCallbacks()
    {
        if (presetManager == null)
        {
            return;
        }

        presetManager.OnCurrentSlotChanged.RemoveListener(UpdateCurrentSlotLabel);
        presetManager.OnPresetsChanged.RemoveListener(RefreshSlotButtons);
    }

    private void RefreshUI()
    {
        int slotIndex = presetManager != null ? presetManager.CurrentSlotIndex : 0;
        UpdateCurrentSlotLabel(slotIndex);
        RefreshSlotButtons();
    }

    private void UpdateCurrentSlotLabel(int slotIndex)
    {
        if (currentSlotLabel == null)
        {
            return;
        }

        int displayIndex = slotIndex + 1;
        int total = presetManager != null ? presetManager.SlotCount : displayIndex;
        currentSlotLabel.text = $"Preset Slot: {displayIndex}/{total}";
    }

    private void RefreshSlotButtons()
    {
        if (slotButtons == null)
        {
            return;
        }

        foreach (MaterialHuePresetSlotButton slotButton in slotButtons)
        {
            slotButton?.RefreshSlot();
        }
    }
}
