public interface IInteractable
{
    void Interact();
}

public interface IFocusableInteractable : IInteractable
{
    void OnFocus(PlayerRayInteractor interactor);
    void OnBlur(PlayerRayInteractor interactor);
}
