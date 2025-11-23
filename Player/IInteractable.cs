public interface IInteractable
{
    void Interact();
}

public interface IFocusInteractor
{
    InteractionUIController InteractionUI { get; }
    void ReleaseHighlightIfCurrent(IInteractable target);
    void ClearFocusIfCurrent(IInteractable target);
}

public interface IFocusableInteractable : IInteractable
{
    void OnFocus(IFocusInteractor interactor);
    void OnBlur(IFocusInteractor interactor);
}
