using UnityEngine;

/// <summary>
/// インタラクト時に子オブジェクトの TimedLightController を手動切り替えします。
/// </summary>
public class TimedLightInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        TimedLightController controller = GetComponentInChildren<TimedLightController>(true);
        if (controller == null)
        {
            Debug.LogWarning($"{nameof(TimedLightInteractable)}: TimedLightController が見つかりません。", this);
            return;
        }

        controller.ToggleManualOverride();
    }
}
