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
    private GameClock clock;

    private void OnEnable()
    {
        CacheOriginalColor();
        TrySubscribeToClock();

        if (clock != null)
        {
            UpdateMaterialColor(clock.NormalizedTime);
        }
    }

    private void Update()
    {
        if (clock == null)
        {
            TrySubscribeToClock();
            if (clock != null)
            {
                UpdateMaterialColor(clock.NormalizedTime);
            }
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromClock();
        RestoreOriginalColor();
    }

    private void OnDestroy()
    {
        UnsubscribeFromClock();
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

    private void TrySubscribeToClock()
    {
        if (clock != null)
        {
            return;
        }

        clock = GameClock.Instance;
        if (clock == null)
        {
            return;
        }

        clock.OnTimeUpdated += UpdateMaterialColor;
    }

    private void UnsubscribeFromClock()
    {
        if (clock != null)
        {
            clock.OnTimeUpdated -= UpdateMaterialColor;
            clock = null;
        }
    }

    private void UpdateMaterialColor(float normalizedTime)
    {
        if (targetMaterial == null)
        {
            return;
        }

        Color targetColor = colorOverDay.Evaluate(normalizedTime);

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
