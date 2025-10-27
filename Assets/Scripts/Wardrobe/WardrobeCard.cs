using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WardrobeCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image iconImage;

    public TMP_Text NameLabel
    {
        get { return nameLabel; }
    }

    public Image IconImage
    {
        get { return iconImage; }
    }

    public Sprite IconSprite
    {
        get { return iconImage != null ? iconImage.sprite : null; }
    }

    public void Apply(WardrobeCatalogEntry entry)
    {
        if (nameLabel != null)
        {
            nameLabel.text = entry.NameId;
        }

        Sprite sprite = LoadSprite(entry);
        SetIcon(sprite);
    }

    public void SetName(string value)
    {
        if (nameLabel != null)
        {
            nameLabel.text = value;
        }
    }

    public void SetIcon(Sprite sprite)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    public static Sprite LoadSprite(WardrobeCatalogEntry entry)
    {
        if (entry.ImageSprite != null)
        {
            return entry.ImageSprite;
        }

        if (!string.IsNullOrEmpty(entry.Image2D))
        {
            return Resources.Load<Sprite>(entry.Image2D);
        }

        return null;
    }
}
