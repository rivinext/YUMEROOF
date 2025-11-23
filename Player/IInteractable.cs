public interface IInteractable
{
    void Interact();
}

public interface IFocusableInteractable : IInteractable
{
    void OnFocus(PlayerProximityInteractor interactor);
    void OnBlur(PlayerProximityInteractor interactor);
}
