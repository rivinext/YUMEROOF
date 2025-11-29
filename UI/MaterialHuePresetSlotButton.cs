using UnityEngine;
using UnityEngine.UI;

public class MaterialHuePresetSlotButton : MonoBehaviour
{
    [SerializeField] private MaterialHuePresetManager presetManager;
    [SerializeField] private int slotIndex;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image selectedIndicator;
    [SerializeField] private Image previewImage;
    [SerializeField] private Color emptyPreviewColor = Color.black;
    [SerializeField] private Text slotLabel;

    private void OnValidate()
    {
        slotIndex = Mathf.Max(0, slotIndex);
    }

    private void Awake()
    {
        RegisterCallbacks();
        RefreshSlot();
    }

    private void OnEnable()
    {
        RegisterCallbacks();
        RefreshSlot();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    public void RefreshSlot()
    {
        bool hasPreset = presetManager != null && presetManager.HasPreset(slotIndex);

        if (loadButton != null)
        {
            loadButton.interactable = hasPreset;
        }

        if (slotLabel != null)
        {
            slotLabel.text = $"Slot {slotIndex + 1}";
        }

        UpdateSelectionIndicator();
        UpdatePreview(hasPreset);
    }

    private void RegisterCallbacks()
    {
        UnregisterCallbacks();

        if (presetManager != null)
        {
            presetManager.OnCurrentSlotChanged.AddListener(OnCurrentSlotChanged);
            presetManager.OnPresetsChanged.AddListener(OnPresetsChanged);
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveClicked);
        }

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(OnLoadClicked);
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectClicked);
        }
    }

    private void UnregisterCallbacks()
    {
        if (presetManager != null)
        {
            presetManager.OnCurrentSlotChanged.RemoveListener(OnCurrentSlotChanged);
            presetManager.OnPresetsChanged.RemoveListener(OnPresetsChanged);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(OnSaveClicked);
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveListener(OnLoadClicked);
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSelectClicked);
        }
    }

    private void OnSaveClicked()
    {
        presetManager?.SavePreset(slotIndex);
        RefreshSlot();
    }

    private void OnLoadClicked()
    {
        presetManager?.LoadPreset(slotIndex);
        RefreshSlot();
    }

    private void OnSelectClicked()
    {
        presetManager?.SetCurrentSlot(slotIndex);
        RefreshSlot();
    }

    private void OnCurrentSlotChanged(int _)
    {
        UpdateSelectionIndicator();
    }

    private void OnPresetsChanged()
    {
        RefreshSlot();
    }

    private void UpdateSelectionIndicator()
    {
        bool isSelected = presetManager != null && presetManager.CurrentSlotIndex == slotIndex;
        if (selectedIndicator != null)
        {
            selectedIndicator.gameObject.SetActive(isSelected);
        }
    }

    private void UpdatePreview(bool hasPreset)
    {
        if (previewImage == null)
        {
            return;
        }

        if (hasPreset && presetManager != null && presetManager.TryGetPresetColor(slotIndex, out Color color))
        {
            previewImage.color = color;
        }
        else
        {
            previewImage.color = emptyPreviewColor;
        }
    }
}
