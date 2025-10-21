using UnityEngine;

/// <summary>
/// Adjusts a material's base color over the course of the in-game day using a gradient.
/// </summary>
public class MaterialTimeOfDayColor : MonoBehaviour
{
    [SerializeField] private Material targetMaterial;
    [SerializeField] private Gradient colorOverDay = new Gradient();
    [SerializeField] private string colorPropertyName = "_BaseColor";

    private Color? originalColor;

    private void OnEnable()
    {
        CacheOriginalColor();
        UpdateMaterialColor();
    }

    private void Update()
    {
        UpdateMaterialColor();
    }

    private void OnDisable()
    {
        RestoreOriginalColor();
    }

    private void OnDestroy()
    {
        RestoreOriginalColor();
    }

    private void CacheOriginalColor()
    {
        if (targetMaterial == null || originalColor.HasValue)
        {
            return;
        }

        if (targetMaterial.HasProperty(colorPropertyName))
        {
            originalColor = targetMaterial.GetColor(colorPropertyName);
        }
        else
        {
            originalColor = targetMaterial.color;
        }
    }

    private void RestoreOriginalColor()
    {
        if (targetMaterial == null || !originalColor.HasValue)
        {
            return;
        }

        if (targetMaterial.HasProperty(colorPropertyName))
        {
            targetMaterial.SetColor(colorPropertyName, originalColor.Value);
        }
        else
        {
            targetMaterial.color = originalColor.Value;
        }
    }

    private void UpdateMaterialColor()
    {
        if (targetMaterial == null)
        {
            return;
        }

        var clock = GameClock.Instance;
        if (clock == null)
        {
            return;
        }

        float time = clock.NormalizedTime;
        Color targetColor = colorOverDay.Evaluate(time);

        if (targetMaterial.HasProperty(colorPropertyName))
        {
            targetMaterial.SetColor(colorPropertyName, targetColor);
        }
        else
        {
            targetMaterial.color = targetColor;
        }
    }
}
