using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaterialHuePresetButtonBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MaterialHuePresetManager presetManager;
    [SerializeField] private Button saveButtonTemplate;
    [SerializeField] private Button loadButtonTemplate;
    [SerializeField] private Transform saveButtonContainer;
    [SerializeField] private Transform loadButtonContainer;

    [Header("Labels")]
    [SerializeField] private string saveLabelFormat = "Save {0}";
    [SerializeField] private string loadLabelFormat = "Load {0}";

    [Header("Options")]
    [SerializeField] private bool rebuildOnEnable = true;

    private readonly List<Button> spawnedButtons = new();

    private void OnEnable()
    {
        if (rebuildOnEnable)
        {
            RebuildButtons();
        }
    }

    public void RebuildButtons()
    {
        ClearSpawnedButtons();

        if (presetManager == null)
        {
            Debug.LogWarning("MaterialHuePresetManager is not assigned.");
            return;
        }

        int slotCount = presetManager.SlotCount;
        if (slotCount <= 0)
        {
            Debug.LogWarning("No preset slots configured on the manager.");
            return;
        }

        for (int i = 0; i < slotCount; i++)
        {
            SpawnButton(saveButtonTemplate, saveButtonContainer, i, true);
            SpawnButton(loadButtonTemplate, loadButtonContainer, i, false);
        }
    }

    private void SpawnButton(Button template, Transform parent, int slotIndex, bool isSave)
    {
        if (template == null || parent == null)
        {
            return;
        }

        Button buttonInstance = Instantiate(template, parent);
        buttonInstance.gameObject.SetActive(true);

        string labelTemplate = isSave ? saveLabelFormat : loadLabelFormat;
        string label = string.Format(labelTemplate, slotIndex + 1);
        Text textComponent = buttonInstance.GetComponentInChildren<Text>();
        if (textComponent != null)
        {
            textComponent.text = label;
        }

        bool isDefaultSlot = presetManager != null && presetManager.IsDefaultSlot(slotIndex);
        if (isSave && isDefaultSlot)
        {
            buttonInstance.interactable = false;
        }

        buttonInstance.onClick.AddListener(() =>
        {
            if (presetManager == null)
            {
                return;
            }

            if (isSave)
            {
                presetManager.SavePreset(slotIndex);
            }
            else
            {
                presetManager.LoadPreset(slotIndex);
            }
        });

        spawnedButtons.Add(buttonInstance);
    }

    private void ClearSpawnedButtons()
    {
        foreach (Button button in spawnedButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        spawnedButtons.Clear();
    }
}
