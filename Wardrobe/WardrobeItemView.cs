using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class WardrobeItemView : MonoBehaviour
{
    [SerializeField] private WardrobeTabType category;
    [SerializeField] private GameObject wearablePrefab;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image iconImage;

    private string displayName;
    private string nameId;
    private string descriptionId;
    private Sprite iconSprite;

    private WardrobeUIController owner;
    private bool isSelected;

    public WardrobeTabType Category => category;
    public GameObject WearablePrefab => wearablePrefab;
    public bool IsEmpty => wearablePrefab == null;
    public string DisplayName => displayName;
    public string NameId => nameId;
    public string DescriptionId => descriptionId;
    public Sprite IconSprite => iconSprite;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        // ✅ ここで TMP_Text のクリック貫通を設定
        TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in tmpTexts)
        {
            tmp.raycastTarget = false; // ← 貫通ON
        }
    }

    private void OnEnable()
    {
        RefreshSelectionState();
    }

    internal void Initialize(WardrobeUIController wardrobeUIController)
    {
        if (owner == wardrobeUIController)
        {
            RefreshSelectionState();
            return;
        }

        if (owner != null)
        {
            Unbind();
        }

        owner = wardrobeUIController;
        Bind();
        RefreshSelectionState();
    }

    public void SetWearablePrefab(GameObject prefab)
    {
        wearablePrefab = prefab;
    }

    public void ApplyCatalogEntry(WardrobeCatalogEntry entry)
    {
        category = entry.TabType;
        displayName = entry.DisplayName;
        SetNameId(entry.NameId);
        descriptionId = entry.DescriptionId;

        string viewName = !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : entry.NameId;
        if (!string.IsNullOrEmpty(viewName))
        {
            gameObject.name = viewName;
        }

        GameObject prefab = entry.WearablePrefab;
        if (prefab == null && !string.IsNullOrEmpty(entry.Model3D))
        {
            prefab = Resources.Load<GameObject>(entry.Model3D);
        }

        SetWearablePrefab(prefab);

        Sprite sprite = entry.ImageSprite;
        if (sprite == null && !string.IsNullOrEmpty(entry.Image2D))
        {
            sprite = Resources.Load<Sprite>(entry.Image2D);
        }

        SetIcon(sprite);
    }

    public void SetNameId(string value)
    {
        nameId = value;
        if (nameLabel != null)
        {
            nameLabel.text = value;
        }
    }

    public void SetIcon(Sprite sprite)
    {
        iconSprite = sprite;
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }
    }

    private void Bind()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void Unbind()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    private void OnDestroy()
    {
        Unbind();

        if (owner != null)
        {
            owner.HandleItemDestroyed(this);
            owner = null;
        }
    }

    private void OnButtonClicked()
    {
        if (owner != null)
        {
            owner.HandleItemSelected(this);
        }
    }

    internal void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(selected);
        }
    }

    private void RefreshSelectionState()
    {
        if (owner == null)
        {
            SetSelected(false);
            return;
        }

        WardrobeItemView currentSelection = owner.GetSelectedItem(category);
        SetSelected(currentSelection == this);
    }
}
