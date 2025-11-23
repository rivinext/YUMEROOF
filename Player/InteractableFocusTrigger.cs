using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractableFocusTrigger : MonoBehaviour
{
    [SerializeField] private MonoBehaviour interactableSource;
    [SerializeField] private bool autoInteractOnEnter;

    private Collider interactionCollider;
    private IInteractable interactable;
    private IFocusableInteractable focusable;
    private readonly HashSet<IFocusInteractor> focusingInteractors = new();

    public bool AutoInteractOnEnter
    {
        get => autoInteractOnEnter;
        set => autoInteractOnEnter = value;
    }

    private void Awake()
    {
        ResolveInteractable();
        InteractableTriggerUtility.EnsureTriggerCollider(this, ref interactionCollider);
    }

    private void OnTriggerEnter(Collider other)
    {
        var interactor = other.GetComponentInParent<IFocusInteractor>();
        if (interactor == null)
            return;

        if (autoInteractOnEnter && interactable != null)
        {
            interactable.Interact();
        }

        if (focusable != null && focusingInteractors.Add(interactor))
        {
            focusable.OnFocus(interactor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var interactor = other.GetComponentInParent<IFocusInteractor>();
        if (interactor == null)
            return;

        if (focusable != null && focusingInteractors.Remove(interactor))
        {
            focusable.OnBlur(interactor);
        }
    }

    private void OnDisable()
    {
        if (focusable == null || focusingInteractors.Count == 0)
            return;

        foreach (var interactor in focusingInteractors)
        {
            focusable.OnBlur(interactor);
        }

        focusingInteractors.Clear();
    }

    private void ResolveInteractable()
    {
        interactable = interactableSource as IInteractable ?? GetComponent<IInteractable>();
        focusable = interactable as IFocusableInteractable ?? GetComponent<IFocusableInteractable>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveInteractable();
        InteractableTriggerUtility.EnsureTriggerCollider(this, ref interactionCollider);
    }
#endif
}
