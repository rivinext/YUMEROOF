using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

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
    }

    void OnEnable()
    {
        SetHoverTargetActive(false);
    }

    /// <summary>
    /// トグルを初期化し、表示とイベントを設定する。
    /// </summary>
    public void Initialize(string id, string displayName, Sprite sprite, ToggleGroup group, UnityAction<string> onSelected, bool showLabel = true, bool useBackgroundColor = false, Color backgroundColor = default, bool useCheckmarkColor = false, Color checkmarkColor = default)
    {
        categoryId = id;
        Awake();

        if (toggle != null)
        {
            toggle.group = group;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn && onSelected != null)
                {
                    onSelected.Invoke(categoryId);
                }
            });
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

        UpdateHoverTargetCategoryText();
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

    private void UpdateHoverTargetCategoryText()
    {
        if (hoverTarget == null)
        {
            return;
        }

        var hoverText = hoverTarget.GetComponentInChildren<TMP_Text>(true);
        if (hoverText == null)
        {
            return;
        }

        var labelLocalizeEvent = label != null ? label.GetComponent<LocalizeStringEvent>() : null;
        if (labelLocalizeEvent != null)
        {
            var hoverLocalizeEvent = hoverText.GetComponent<LocalizeStringEvent>();
            if (hoverLocalizeEvent == null)
            {
                hoverLocalizeEvent = hoverText.gameObject.AddComponent<LocalizeStringEvent>();
            }

            hoverLocalizeEvent.enabled = true;
            hoverLocalizeEvent.StringReference.TableReference = labelLocalizeEvent.StringReference.TableReference;
            hoverLocalizeEvent.StringReference.TableEntryReference = labelLocalizeEvent.StringReference.TableEntryReference;
            hoverLocalizeEvent.RefreshString();
            return;
        }

        var existingHoverLocalizeEvent = hoverText.GetComponent<LocalizeStringEvent>();
        if (existingHoverLocalizeEvent != null)
        {
            existingHoverLocalizeEvent.enabled = false;
        }

        hoverText.text = label != null ? label.text : string.Empty;
    }

    public void SetLabelLocalization(string tableName, string key)
    {
        if (label == null)
        {
            return;
        }

        var localizeEvent = label.GetComponent<LocalizeStringEvent>();
        if (localizeEvent == null)
        {
            localizeEvent = label.gameObject.AddComponent<LocalizeStringEvent>();
        }

        localizeEvent.StringReference = new LocalizedString(tableName, key);
        localizeEvent.enabled = false;
        localizeEvent.enabled = true;
        UpdateHoverTargetCategoryText();
    }
}
