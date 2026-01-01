using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 家具カテゴリ用のトグル。アイコンとラベルを扱いやすくまとめる。
/// </summary>
[RequireComponent(typeof(Toggle))]
public class FurnitureCategoryToggle : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;
    [SerializeField] private Image background;

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
    }

    /// <summary>
    /// トグルを初期化し、表示とイベントを設定する。
    /// </summary>
    public void Initialize(string id, string displayName, Sprite sprite, ToggleGroup group, UnityAction<string> onSelected, bool showLabel = true, bool useBackgroundColor = false, Color backgroundColor = default)
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
}
