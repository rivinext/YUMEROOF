using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ReturnToMainMenuButton : MonoBehaviour
{
    [SerializeField] private string mainMenuScene;
    private void Start()
    {
        var button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("ReturnToMainMenuButton requires a Button component.");
            return;
        }

        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        OnConfirm();
    }

    private void OnConfirm()
    {
        SaveGameManager.Instance?.SaveCurrentSlot();
        SlideTransitionManager.Instance?.LoadSceneWithSlide(mainMenuScene);
    }
}
