using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SlotType { Story, Creative }

public class SaveSlotUI : MonoBehaviour
{
    private string slotKey;
    [SerializeField] private SlotType slotType;
    [SerializeField] private int slotIndex;
    [SerializeField] private TMP_Text slotNameText;
    [SerializeField] private TMP_Text lastSaveText;
    [SerializeField] private TMP_Text chapterText;
    [SerializeField] private TMP_Text locationText;
    [SerializeField] private TMP_Text playTimeText;
    [SerializeField] private Image screenshotImage;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button deleteButton;

    public event Action<string> OnSelected;
    public event Action<string> OnDeleteRequested;

    public string SlotKey => slotKey;

    void Awake()
    {
        UpdateSlotKey();
        if (selectButton != null)
            selectButton.onClick.AddListener(Select);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(RequestDelete);
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
                    string path = Path.Combine(Application.persistentDataPath, data.screenshotFilename);
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
            if (lastSaveText != null)
                lastSaveText.text = "Empty";
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
            deleteButton.interactable = interactable;
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
        var slots = FindObjectsOfType<SaveSlotUI>();

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
}
