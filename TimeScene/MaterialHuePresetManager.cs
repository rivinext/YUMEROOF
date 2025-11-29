using System.Collections.Generic;
using UnityEngine;

public class MaterialHuePresetManager : MonoBehaviour
{
    [SerializeField] private List<MaterialHueController> controllers = new();
    [SerializeField] private string keyPrefix = "multi_mat_preset";

    // インスペクタから確認しやすいようにスロット数だけ表示しておく（お好みで）
    [SerializeField] private int maxSlots = 5;

    // 指定スロットに、すべての MaterialHueController の色を保存
    public void SavePreset(int slotIndex)
    {
        if (controllers == null || controllers.Count == 0)
        {
            Debug.LogWarning("No MaterialHueControllers are assigned.");
            return;
        }

        int clampedSlot = Mathf.Max(0, slotIndex);

        for (int i = 0; i < controllers.Count; i++)
        {
            MaterialHueController controller = controllers[i];
            if (controller == null) continue;

            string baseKey = $"{keyPrefix}_{clampedSlot}_{i}";
            PlayerPrefs.SetFloat(baseKey + "_h", controller.Hue);
            PlayerPrefs.SetFloat(baseKey + "_s", controller.Saturation);
            PlayerPrefs.SetFloat(baseKey + "_v", controller.Value);
        }

        PlayerPrefs.Save();
        Debug.Log($"Saved preset slot {clampedSlot}");
    }

    // 指定スロットから、すべての MaterialHueController の色を復元
    public void LoadPreset(int slotIndex)
    {
        if (controllers == null || controllers.Count == 0)
        {
            Debug.LogWarning("No MaterialHueControllers are assigned.");
            return;
        }

        int clampedSlot = Mathf.Max(0, slotIndex);

        for (int i = 0; i < controllers.Count; i++)
        {
            MaterialHueController controller = controllers[i];
            if (controller == null) continue;

            string baseKey = $"{keyPrefix}_{clampedSlot}_{i}";
            string hueKey = baseKey + "_h";

            // そのスロットにまだ保存されていない場合はスキップ
            if (!PlayerPrefs.HasKey(hueKey))
                continue;

            float h = PlayerPrefs.GetFloat(hueKey, controller.Hue);
            float s = PlayerPrefs.GetFloat(baseKey + "_s", controller.Saturation);
            float v = PlayerPrefs.GetFloat(baseKey + "_v", controller.Value);

            controller.SetHSV(h, s, v);
        }

        Debug.Log($"Loaded preset slot {clampedSlot}");
    }

    // ボタン用のラッパー（インスペクタで使いやすいように）
    public void SavePresetSlot0() => SavePreset(0);
    public void SavePresetSlot1() => SavePreset(1);
    public void SavePresetSlot2() => SavePreset(2);

    public void LoadPresetSlot0() => LoadPreset(0);
    public void LoadPresetSlot1() => LoadPreset(1);
    public void LoadPresetSlot2() => LoadPreset(2);
}
