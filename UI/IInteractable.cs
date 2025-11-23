using Interaction;
using UnityEngine;

public class CylinderInteractable : MonoBehaviour, IInteractable
{
    private Collider interactionCollider;

    void Awake()
    {
        InteractableTriggerUtility.EnsureTriggerCollider(this, ref interactionCollider);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        InteractableTriggerUtility.EnsureTriggerCollider(this, ref interactionCollider);
    }
#endif
    // IInteractable で要求されるメソッド
    public void Interact()
    {
        // 特に処理が無ければ空で構いません
    }
}
