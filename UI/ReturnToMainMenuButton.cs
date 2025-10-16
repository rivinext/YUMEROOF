using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ReturnToMainMenuButton : MonoBehaviour
{
    [SerializeField] private string mainMenuScene;
    [SerializeField] private ConfirmationPopup confirmationPopup;
    [SerializeField, TextArea] private string confirmationMessage = "メインメニューに戻りますか？";

    private void Start()
    {
        var button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("ReturnToMainMenuButton requires a Button component.");
            return;
        }

        if (confirmationPopup == null)
        {
            confirmationPopup = FindObjectOfType<ConfirmationPopup>(true);
            if (confirmationPopup == null)
            {
                Debug.LogError("ConfirmationPopup reference is not assigned. Falling back to immediate transition.", this);
            }
        }

        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        if (confirmationPopup == null)
        {
            OnConfirm();
            return;
        }

        var message = string.IsNullOrWhiteSpace(confirmationMessage)
            ? "メインメニューに戻りますか？"
            : confirmationMessage;

        confirmationPopup.Open(message, OnConfirm);
    }

    private void OnConfirm()
    {
        SaveGameManager.Instance?.SaveCurrentSlot();
        SlideTransitionManager.Instance?.LoadSceneWithSlide(mainMenuScene);
    }
}
