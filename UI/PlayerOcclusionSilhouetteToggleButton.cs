using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerOcclusionSilhouetteToggleButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Toggle toggle;
    [SerializeField] private PlayerOcclusionSilhouette occlusionSilhouette;

    [Header("Settings")]
    [SerializeField] private Material silhouetteMaterial;

    private bool isSilhouetteEnabled;
    private Dictionary<Renderer, Material[]> originalMaterials;
    private Dictionary<Renderer, Material[]> materialsWithoutSilhouette;

    private void Awake()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();
        RefreshSilhouetteState();
        isSilhouetteEnabled = toggle != null ? toggle.isOn : false;
    }

    private void OnEnable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(HandleToggleValueChanged);
            toggle.SetIsOnWithoutNotify(isSilhouetteEnabled);
        }
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyState();
    }

    private void OnDisable()
    {
        if (toggle != null) toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RefreshAfterSceneLoad());
    }

    private System.Collections.IEnumerator RefreshAfterSceneLoad()
    {
        yield return null;
        RefreshSilhouetteState();
        ApplyState();
    }

    private void RefreshSilhouetteState()
    {
        FindOrCacheSilhouetteReference();
        FindSilhouetteMaterials();
    }

    private void FindOrCacheSilhouetteReference()
    {
        if (occlusionSilhouette != null) return;

        occlusionSilhouette = FindObjectOfType<PlayerOcclusionSilhouette>();
        if (occlusionSilhouette != null) return;

        var allSilhouettes = Resources.FindObjectsOfTypeAll<PlayerOcclusionSilhouette>();
        var activeScene = SceneManager.GetActiveScene();

        foreach (var silhouette in allSilhouettes)
        {
            if (silhouette == null || silhouette.gameObject == null) continue;
            if (silhouette.gameObject.hideFlags != HideFlags.None) continue;

            var scene = silhouette.gameObject.scene;
            if (scene.IsValid() && (scene == activeScene || scene.name == "DontDestroyOnLoad"))
            {
                occlusionSilhouette = silhouette;
                return;
            }
        }

        Debug.LogWarning("PlayerOcclusionSilhouette component was not found.");
    }

    private void FindSilhouetteMaterials()
    {
        originalMaterials = new Dictionary<Renderer, Material[]>();
        materialsWithoutSilhouette = new Dictionary<Renderer, Material[]>();

        if (silhouetteMaterial == null)
        {
            Debug.LogWarning("Silhouette material is not assigned!");
            return;
        }

        var allRenderers = new List<Renderer>(FindObjectsOfType<Renderer>(true));

        // DontDestroyOnLoad内のオブジェクトを追加
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if ((go.scene.name == "DontDestroyOnLoad" || go.hideFlags == HideFlags.None))
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null && !allRenderers.Contains(renderer))
                {
                    allRenderers.Add(renderer);
                }
            }
        }

        foreach (var renderer in allRenderers)
        {
            if (renderer == null) continue;

            var materials = renderer.sharedMaterials;
            var nonSilhouetteMats = new List<Material>();
            bool hasSilhouette = false;

            foreach (var mat in materials)
            {
                if (IsSilhouetteMaterial(mat))
                {
                    hasSilhouette = true;
                }
                else
                {
                    nonSilhouetteMats.Add(mat);
                }
            }

            if (hasSilhouette)
            {
                originalMaterials[renderer] = materials;
                materialsWithoutSilhouette[renderer] = nonSilhouetteMats.ToArray();
            }
        }

        if (originalMaterials.Count == 0)
        {
            CacheSilhouetteMaterialsFromOcclusion();
        }

        Debug.Log($"Found {originalMaterials.Count} renderers with Silhouette material");
    }

    private bool IsSilhouetteMaterial(Material material)
    {
        if (material == null || silhouetteMaterial == null) return false;
        if (material == silhouetteMaterial) return true;
        return material.shader == silhouetteMaterial.shader &&
               material.name.StartsWith(silhouetteMaterial.name);
    }

    private void HandleToggleValueChanged(bool value)
    {
        isSilhouetteEnabled = value;
        if (originalMaterials == null || originalMaterials.Count == 0)
        {
            FindSilhouetteMaterials();
        }
        ApplyState();
    }

    private void ApplyState()
    {
        if (occlusionSilhouette == null) FindOrCacheSilhouetteReference();

        if (occlusionSilhouette != null)
        {
            occlusionSilhouette.forceSilhouette = false;
            occlusionSilhouette.enabled = isSilhouetteEnabled;
        }

        SetSilhouetteMaterialsEnabled(isSilhouetteEnabled);
    }

    private void SetSilhouetteMaterialsEnabled(bool enabled)
    {
        if (originalMaterials == null || originalMaterials.Count == 0)
        {
            FindSilhouetteMaterials();
            if (originalMaterials == null || originalMaterials.Count == 0) return;
        }

        int appliedCount = 0;
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.sharedMaterials = enabled ? kvp.Value : materialsWithoutSilhouette[kvp.Key];
                appliedCount++;
            }
        }

        Debug.Log($"Applied silhouette state ({enabled}) to {appliedCount} renderers");
    }

    public void SetSilhouetteEnabled(bool enabled)
    {
        isSilhouetteEnabled = enabled;
        if (toggle != null) toggle.SetIsOnWithoutNotify(isSilhouetteEnabled);
        ApplyState();
    }

    private void CacheSilhouetteMaterialsFromOcclusion()
    {
        if (occlusionSilhouette == null) return;

        if (silhouetteMaterial == null)
        {
            silhouetteMaterial = occlusionSilhouette.silhouetteMaterial;
        }

        if (silhouetteMaterial == null) return;

        if (occlusionSilhouette.targetRenderers == null) return;

        foreach (var renderer in occlusionSilhouette.targetRenderers)
        {
            if (renderer == null) continue;

            var baseMaterials = renderer.sharedMaterials;
            if (baseMaterials == null || baseMaterials.Length == 0) continue;

            var baseCopy = new Material[baseMaterials.Length];
            baseMaterials.CopyTo(baseCopy, 0);

            var combined = occlusionSilhouette.CreateCombinedMaterialsFor(renderer);
            if (combined == null || combined.Length == 0) continue;

            originalMaterials[renderer] = combined;
            materialsWithoutSilhouette[renderer] = baseCopy;
        }
    }
}
