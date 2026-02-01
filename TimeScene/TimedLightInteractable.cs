using UnityEngine;

/// <summary>
/// Allows manual interaction with a child TimedLightController.
/// </summary>
public class TimedLightInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private TimedLightController[] targetControllers;

    void Awake()
    {
        if (targetControllers == null || targetControllers.Length == 0)
        {
            targetControllers = GetComponentsInChildren<TimedLightController>(true);
        }
    }

    public void Interact()
    {
        if (targetControllers == null || targetControllers.Length == 0)
        {
            Debug.LogWarning("[TimedLightInteractable] TimedLightController が見つかりません。", this);
            return;
        }

        foreach (var controller in targetControllers)
        {
            if (controller == null)
            {
                continue;
            }

            controller.ToggleManualOverrideFromInteract();
        }
    }
}
