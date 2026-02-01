using UnityEngine;

/// <summary>
/// Allows manual interaction with a child TimedLightController.
/// </summary>
public class TimedLightInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private TimedLightController targetController;

    void Awake()
    {
        if (targetController == null)
        {
            targetController = GetComponentInChildren<TimedLightController>(true);
        }
    }

    public void Interact()
    {
        if (targetController == null)
        {
            Debug.LogWarning("[TimedLightInteractable] TimedLightController が見つかりません。", this);
            return;
        }

        targetController.ToggleManualOverride();
    }
}
