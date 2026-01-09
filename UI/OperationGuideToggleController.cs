using UnityEngine;
using UnityEngine.UI;

public class OperationGuideToggleController : MonoBehaviour
{
    private const string PlayerPrefsKey = "UI.OperationGuide.Enabled";

    [SerializeField] private Toggle operationGuideToggle;
    [SerializeField] private FurniturePlacementTutorialAnimator furniturePlacementTutorialAnimator;
    [SerializeField] private GameObject[] operationGuideCanvases;

    private void Awake()
    {
        if (operationGuideToggle == null)
        {
            operationGuideToggle = GetComponent<Toggle>();
        }
    }

    private void OnEnable()
    {
        InitializeToggle();

        if (operationGuideToggle != null)
        {
            operationGuideToggle.onValueChanged.AddListener(HandleToggleChanged);
        }
    }

    private void OnDisable()
    {
        if (operationGuideToggle != null)
        {
            operationGuideToggle.onValueChanged.RemoveListener(HandleToggleChanged);
        }
    }

    private void InitializeToggle()
    {
        if (operationGuideToggle == null)
        {
            return;
        }

        bool isEnabled = PlayerPrefs.GetInt(PlayerPrefsKey, 1) == 1;
        operationGuideToggle.SetIsOnWithoutNotify(isEnabled);
        ApplyState(isEnabled);
    }

    private void HandleToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(PlayerPrefsKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplyState(isOn);
    }

    private void ApplyState(bool isOn)
    {
        if (furniturePlacementTutorialAnimator != null)
        {
            if (isOn)
            {
                furniturePlacementTutorialAnimator.gameObject.SetActive(true);
                furniturePlacementTutorialAnimator.Play();
            }
            else
            {
                furniturePlacementTutorialAnimator.Stop();
                furniturePlacementTutorialAnimator.gameObject.SetActive(false);
            }
        }

        if (operationGuideCanvases == null || operationGuideCanvases.Length == 0)
        {
            return;
        }

        foreach (GameObject operationGuideCanvas in operationGuideCanvases)
        {
            if (operationGuideCanvas == null)
            {
                continue;
            }

            operationGuideCanvas.SetActive(isOn);
        }
    }
}
