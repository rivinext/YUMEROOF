using UnityEngine;

public class HighlightTarget : MonoBehaviour
{
    private Material originalMaterial;
    public Material highlightMaterial;

    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalMaterial = rend.material;
        }
    }

    public void Highlight()
    {
        if (rend != null && highlightMaterial != null)
        {
            rend.material = highlightMaterial;
        }
    }

    public void Unhighlight()
    {
        if (rend != null && originalMaterial != null)
        {
            rend.material = originalMaterial;
        }
    }
}
