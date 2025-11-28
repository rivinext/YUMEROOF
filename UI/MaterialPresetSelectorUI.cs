using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MaterialPresetSelectorUI : MonoBehaviour
{
    [SerializeField] private MaterialHueController hueController;
    [SerializeField] private MaterialPresetButton presetButtonPrefab;
    [SerializeField] private Transform buttonContainer;

    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

    private void OnEnable()
    {
        RefreshButtons();
    }

    public void RefreshButtons()
    {
        if (hueController == null || presetButtonPrefab == null || buttonContainer == null)
        {
            Debug.LogWarning($"{nameof(MaterialPresetSelectorUI)} on {name} is missing required references.");
            return;
        }

        ClearButtons();

        int totalPresets = hueController.GetTotalPresetCount();
        for (int i = 0; i < totalPresets; i++)
        {
            MaterialPresetButton buttonInstance = Instantiate(presetButtonPrefab, buttonContainer);
            buttonInstance.SetPresetIndex(i);
            buttonInstance.SetController(hueController);
            buttonInstance.SetSelectOnly(false);

            UpdateLabel(buttonInstance.gameObject, i);

            spawnedButtons.Add(buttonInstance.gameObject);
        }
    }

    private void UpdateLabel(GameObject buttonObject, int presetIndex)
    {
        if (!hueController.TryGetPresetDisplayName(presetIndex, out string displayName))
        {
            displayName = $"Preset {presetIndex + 1}";
        }

        if (buttonObject.TryGetComponent(out TMP_Text tmpText))
        {
            tmpText.text = displayName;
            return;
        }

        TMP_Text tmpChildText = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (tmpChildText != null)
        {
            tmpChildText.text = displayName;
            return;
        }

        if (buttonObject.TryGetComponent(out Text uiText))
        {
            uiText.text = displayName;
            return;
        }

        Text childText = buttonObject.GetComponentInChildren<Text>(true);
        if (childText != null)
        {
            childText.text = displayName;
        }
    }

    private void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
            {
                Destroy(spawnedButtons[i]);
            }
        }

        spawnedButtons.Clear();
    }
}
