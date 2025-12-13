using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public enum SlotType { Story, Creative }

public class SaveSlotUI : MonoBehaviour
{
    private string slotKey;

    [Header("Slot Identity")]
    [SerializeField] private SlotType slotType;
    [SerializeField] private int slotIndex;

    [Header("UI References")]
    [SerializeField] private TMP_Text slotNameText;
    [SerializeField] private TMP_Text lastSaveText;
    [SerializeField] private TMP_Text chapterText;
    [SerializeField] private TMP_Text locationText;
    [SerializeField] private TMP_Text playTimeText;
    [SerializeField] private Image screenshotImage;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button deleteButton;

    [Header("Localization (Select Button)")]
    [Tooltip("Localize String Event attached to the select button label (e.g., TextMeshProUGUI on the button).")]
    [SerializeField] private LocalizeStringEvent selectButtonLocalizeEvent;

    [Tooltip("Fallback plain text label, used only if LocalizeStringEvent is not provided.")]
    [SerializeField] private TMP_Text selectButtonLabelFallback;

    [Header("Localization Keys")]
    [SerializeField] private string tableName = "StandardText";
    [SerializeField] private string startKey = "Start";
    [SerializeField] private string loadKey = "Load";

    public event Action<string> OnSelected;
    public event Action<string> OnDeleteRequested;

    public string SlotKey => slotKey;

    private bool hasSaveData;

    void Awake()
    {
        UpdateSlotKey();
        if (selectButton != null)
            selectButton.onClick.AddListener(Select);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(RequestDelete);

        // デフォルトでは「Start」を表示し、スタートボタンのみ操作可能にする
        SetSelectButtonLabelKey(startKey);
        if (selectButton != null)
            selectButton.interactable = true;
        if (deleteButton != null)
        {
            hasSaveData = false;
            deleteButton.interactable = false;
        }
    }

    void OnValidate()
    {
        UpdateSlotKey();
    }

    void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(Select);
        if (deleteButton != null)
            deleteButton.onClick.RemoveListener(RequestDelete);
    }

    public void Refresh()
    {
        if (slotNameText != null)
            slotNameText.text = slotKey;

        var data = SaveGameManager.Instance.LoadMetadata(slotKey);
        if (data != null)
        {
            hasSaveData = true;
            // Select button: show "Load"
            SetSelectButtonLabelKey(loadKey);

            if (deleteButton != null)
                deleteButton.interactable = true;

            if (lastSaveText != null)
            {
                if (DateTime.TryParse(data.saveDate, out DateTime saveDate))
                    lastSaveText.text = saveDate.ToString("yyyy/MM/dd HH:mm");
                else
                    lastSaveText.text = data.saveDate; // 変換できない場合は元の文字列を表示
            }

            if (chapterText != null)
            {
                if (data is StorySaveData story)
                    chapterText.text = $"{story.chapterName} (Milestone {story.milestoneIndex})";
                else if (data is CreativeSaveData creative)
                    chapterText.text = $"{creative.chapterName} (Milestone {creative.milestoneIndex})";
                else
                    chapterText.text = data.chapterName;
            }

            if (locationText != null)
                locationText.text = data.location;

            if (playTimeText != null)
            {
                var ts = TimeSpan.FromSeconds(data.playTime);
                playTimeText.text = ts.ToString(@"hh\:mm\:ss");
            }

            if (screenshotImage != null)
            {
                screenshotImage.sprite = null;
                if (!string.IsNullOrEmpty(data.screenshotFilename))
                {
                    string path = SaveGameManager.Instance.GetScreenshotPath(data.screenshotFilename);
                    if (File.Exists(path))
                    {
                        var bytes = File.ReadAllBytes(path);
                        Texture2D tex = new Texture2D(2, 2);
                        if (tex.LoadImage(bytes))
                        {
                            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            screenshotImage.sprite = sprite;
                        }
                    }
                }
            }
        }
        else
        {
            hasSaveData = false;
            // Select button: show "Start"
            SetSelectButtonLabelKey(startKey);

            if (deleteButton != null)
                deleteButton.interactable = false;

            if (lastSaveText != null)
                lastSaveText.text = string.Empty;
            if (chapterText != null)
                chapterText.text = string.Empty;
            if (locationText != null)
                locationText.text = string.Empty;
            if (playTimeText != null)
                playTimeText.text = string.Empty;
            if (screenshotImage != null)
                screenshotImage.sprite = null;
        }
    }

    /// <summary>
    /// スロットのボタンを有効/無効に設定
    /// </summary>
    /// <param name="interactable">有効にするかどうか</param>
    public void SetInteractable(bool interactable)
    {
        if (selectButton != null)
            selectButton.interactable = interactable;
        if (deleteButton != null)
            deleteButton.interactable = interactable && hasSaveData;
    }

    void Select()
    {
        OnSelected?.Invoke(slotKey);
    }

    private void RequestDelete()
    {
        OnDeleteRequested?.Invoke(slotKey);
    }

    private void UpdateSlotKey()
    {
        var slots = FindObjectsByType<SaveSlotUI>(FindObjectsSortMode.None);

        // Collect indices already used by other slots of the same type
        var usedIndices = new HashSet<int>();
        foreach (var slot in slots)
        {
            if (slot != this && slot.slotType == slotType)
                usedIndices.Add(slot.slotIndex);
        }

        // If this slot's index is negative or already taken, assign a new one
        if (slotIndex < 0 || usedIndices.Contains(slotIndex))
        {
            int newIndex = 0;
            while (usedIndices.Contains(newIndex))
                newIndex++;

            Debug.LogWarning(
                $"slotIndex for {name} was unset or duplicate. Auto-assigned {slotType}{newIndex}.",
                this);
            slotIndex = newIndex;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        slotKey = $"{slotType}{slotIndex}";
    }

    /// <summary>
    /// LocalizeStringEvent があればキーを割り当てて即座に更新。
    /// なければフォールバックの TMP_Text に英語キー文字列を直接表示。
    /// </summary>
    private void SetSelectButtonLabelKey(string key)
    {
        if (selectButtonLocalizeEvent != null)
        {
            selectButtonLocalizeEvent.StringReference = new LocalizedString(tableName, key);
            selectButtonLocalizeEvent.RefreshString();
        }
        else if (selectButtonLabelFallback != null)
        {
            // フォールバック: ローカライズ未使用の場合は英語キーそのまま表示
            selectButtonLabelFallback.text = key;
        }
    }
}
