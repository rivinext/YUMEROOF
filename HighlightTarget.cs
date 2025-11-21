using System.Collections.Generic;
using UnityEngine;

public class HighlightTarget : MonoBehaviour
{
    private Material originalMaterial;
    public Material highlightMaterial;
    public Material silhouetteMaterial;

    private Renderer rend;
    private Collider[] attachedColliders = System.Array.Empty<Collider>();
    private readonly HashSet<Collider> colliderSet = new();
    private bool isHighlighted;
    private bool isSilhouetteActive;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalMaterial = rend.material;
        }

        attachedColliders = GetComponentsInChildren<Collider>(true);
        colliderSet.Clear();
        if (attachedColliders != null)
        {
            for (int i = 0; i < attachedColliders.Length; i++)
            {
                if (attachedColliders[i] != null)
                {
                    colliderSet.Add(attachedColliders[i]);
                }
            }
        }
    }

    public void Highlight()
    {
        isHighlighted = true;

        if (rend == null)
        {
            return;
        }

        if (isSilhouetteActive && silhouetteMaterial != null)
        {
            rend.material = silhouetteMaterial;
            return;
        }

        if (highlightMaterial != null)
        {
            rend.material = highlightMaterial;
        }
        else if (originalMaterial != null)
        {
            rend.material = originalMaterial;
        }
    }

    public void Unhighlight()
    {
        isHighlighted = false;

        if (isSilhouetteActive)
        {
            ClearSilhouette();
            return;
        }

        if (rend != null && originalMaterial != null)
        {
            rend.material = originalMaterial;
        }
    }

    public void ApplySilhouette()
    {
        if (rend == null || silhouetteMaterial == null)
        {
            return;
        }

        rend.material = silhouetteMaterial;
        isSilhouetteActive = true;
    }

    public void ClearSilhouette()
    {
        if (!isSilhouetteActive)
        {
            return;
        }

        isSilhouetteActive = false;

        if (rend == null)
        {
            return;
        }

        if (isHighlighted && highlightMaterial != null)
        {
            rend.material = highlightMaterial;
            return;
        }

        if (originalMaterial != null)
        {
            rend.material = originalMaterial;
        }
    }

    public Renderer TargetRenderer => rend;

    public bool OwnsCollider(Collider collider)
    {
        return collider != null && colliderSet.Contains(collider);
    }
}
