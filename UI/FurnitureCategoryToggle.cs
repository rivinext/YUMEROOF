using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 家具カテゴリ用のトグル。アイコンとラベルを扱いやすくまとめる。
/// </summary>
[RequireComponent(typeof(Toggle))]
public class FurnitureCategoryToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Image checkmark;
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private TMP_Text selectedIndicatorText;
    // UIホバーが反応しない場合は、シーンに EventSystem と GraphicRaycaster があるか確認すること。
    [SerializeField] private GameObject hoverTarget;

    private Toggle toggle;
    private string categoryId;

    public string CategoryId => categoryId;
    public Toggle Toggle => toggle;

    void Awake()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }

        SetHoverTargetActive(false);
        SetSelectedIndicatorActive(false);
    }

    void OnEnable()
    {
        SetHoverTargetActive(false);
        SetSelectedIndicatorActive(false);
    }

    /// <summary>
    /// トグルを初期化し、表示とイベントを設定する。
    /// </summary>
    public void Initialize(string id, string displayName, Sprite sprite, ToggleGroup group, UnityAction<string> onSelected, bool showLabel = true, bool useBackgroundColor = false, Color backgroundColor = default, bool useCheckmarkColor = false, Color checkmarkColor = default)
    {
        categoryId = id;
        Awake();
        if (selectedIndicatorText != null)
        {
            selectedIndicatorText.text = categoryId;
        }

        if (toggle != null)
        {
            toggle.group = group;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(isOn =>
            {
                SetSelectedIndicatorActive(isOn);
                if (isOn && onSelected != null)
                {
                    onSelected.Invoke(categoryId);
                }
            });
            SetSelectedIndicatorActive(toggle.isOn);
        }

        if (label != null)
        {
            label.text = string.IsNullOrEmpty(displayName) ? id : displayName;
            label.gameObject.SetActive(showLabel);
        }

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
        }

        if (background != null && useBackgroundColor)
        {
            background.color = backgroundColor;
        }

        if (useCheckmarkColor)
        {
            var checkmarkImage = checkmark != null ? checkmark : toggle != null ? toggle.graphic as Image : null;
            if (checkmarkImage != null)
            {
                checkmarkImage.color = checkmarkColor;
            }
        }
    }

    public void SetIsOn(bool isOn, bool notify = true)
    {
        if (toggle == null)
        {
            return;
        }

        if (notify)
        {
            toggle.isOn = isOn;
        }
        else
        {
            toggle.SetIsOnWithoutNotify(isOn);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHoverTargetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverTargetActive(false);
    }

    private void SetHoverTargetActive(bool isActive)
    {
        if (hoverTarget != null)
        {
            hoverTarget.SetActive(isActive);
        }
    }

    private void SetSelectedIndicatorActive(bool isActive)
    {
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(isActive);
        }
    }
}
