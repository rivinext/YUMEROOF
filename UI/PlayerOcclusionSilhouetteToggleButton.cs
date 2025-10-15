using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerOcclusionSilhouetteToggleButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Toggle toggle;
    [SerializeField] private PlayerOcclusionSilhouette occlusionSilhouette;

    [Header("Settings")]
    [SerializeField] private Material silhouetteMaterial;

    private bool isSilhouetteEnabled;
    private System.Collections.Generic.Dictionary<Renderer, Material[]> originalMaterials;
    private System.Collections.Generic.Dictionary<Renderer, Material[]> materialsWithoutSilhouette;

    private Material EffectiveSilhouetteMaterial => silhouetteMaterial != null
        ? silhouetteMaterial
        : occlusionSilhouette != null
            ? occlusionSilhouette.silhouetteMaterial
            : null;

    private void Awake()
    {
        CacheToggleReference();
        CacheSilhouetteReference();
        RefreshSilhouetteMaterialCache();
        InitializeState();
    }

    private void OnEnable()
    {
        CacheSilhouetteReference();
        RefreshSilhouetteMaterialCache();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RegisterToggleCallback();
        UpdateToggleValue();
        ApplyState();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnregisterToggleCallback();
    }

    private void CacheToggleReference()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
    }

    private void CacheSilhouetteReference()
    {
        if (occlusionSilhouette == null)
        {
            occlusionSilhouette = FindObjectOfType<PlayerOcclusionSilhouette>();
        }
    }

    private void RefreshSilhouetteMaterialCache()
    {
        CacheSilhouetteReference();

        if (occlusionSilhouette != null)
        {
            occlusionSilhouette.RebuildSilhouetteSetup();
        }

        FindSilhouetteMaterials();
    }

    private void FindSilhouetteMaterials()
    {
        originalMaterials = new System.Collections.Generic.Dictionary<Renderer, Material[]>();
        materialsWithoutSilhouette = new System.Collections.Generic.Dictionary<Renderer, Material[]>();

        bool cachedFromOcclusion = TryCacheFromOcclusionSilhouette();

        if (cachedFromOcclusion)
        {
            Debug.Log($"Found {originalMaterials.Count} renderers with Silhouette material via PlayerOcclusionSilhouette");
            return;
        }

        var targetSilhouetteMaterial = EffectiveSilhouetteMaterial;
        if (targetSilhouetteMaterial == null)
        {
            Debug.LogWarning("Silhouette material is not assigned!");
            return;
        }

        // 通常のシーンオブジェクトとDontDestroyOnLoad内のオブジェクトの両方を取得
        var allRenderers = new System.Collections.Generic.List<Renderer>();

        // 通常のシーンオブジェクト
        allRenderers.AddRange(FindObjectsOfType<Renderer>(true));

        // DontDestroyOnLoad内のオブジェクトを明示的に検索
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene.name == "DontDestroyOnLoad" || go.hideFlags == HideFlags.None)
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
            var hasSilhouette = false;
            var nonSilhouetteMats = new System.Collections.Generic.List<Material>();

            foreach (var mat in materials)
            {
                if (mat == targetSilhouetteMaterial)
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

        Debug.Log($"Found {originalMaterials.Count} renderers with Silhouette material");
    }

    private void InitializeState()
    {
        // ToggleのIsOnの状態を取得
        if (toggle != null)
        {
            isSilhouetteEnabled = toggle.isOn;
        }
        else
        {
            isSilhouetteEnabled = false;
        }
    }

    private void RegisterToggleCallback()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(HandleToggleValueChanged);
        }
    }

    private void UnregisterToggleCallback()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }
    }

    private void HandleToggleValueChanged(bool value)
    {
        isSilhouetteEnabled = value;
        ApplyState();
    }

    private void UpdateToggleValue()
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(isSilhouetteEnabled);
        }
    }

    private void ApplyState()
    {
        CacheSilhouetteReference();
        // トグルがONの時はシルエットをON、OFFの時はシルエットをOFF
        if (occlusionSilhouette != null)
        {
            occlusionSilhouette.forceSilhouette = false;
            occlusionSilhouette.enabled = isSilhouetteEnabled;
        }

        // Silhouetteマテリアルの有効/無効を切り替え
        SetSilhouetteMaterialsEnabled(isSilhouetteEnabled);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSilhouetteMaterialCache();
        ApplyState();
    }

    private bool TryCacheFromOcclusionSilhouette()
    {
        if (occlusionSilhouette == null || occlusionSilhouette.targetRenderers == null)
        {
            return false;
        }

        int cachedCount = 0;
        foreach (var renderer in occlusionSilhouette.targetRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!occlusionSilhouette.TryGetMaterialSets(renderer, out var withSilhouette, out var withoutSilhouette))
            {
                continue;
            }

            originalMaterials[renderer] = withSilhouette;
            materialsWithoutSilhouette[renderer] = withoutSilhouette;
            cachedCount++;
        }

        return cachedCount > 0;
    }

    private void SetSilhouetteMaterialsEnabled(bool enabled)
    {
        if (originalMaterials == null) return;

        int appliedCount = 0;
        foreach (var kvp in originalMaterials)
        {
            var renderer = kvp.Key;
            if (renderer != null)
            {
                if (enabled)
                {
                    // 有効時：元のマテリアル配列（Silhouetteを含む）を復元
                    renderer.sharedMaterials = kvp.Value;
                }
                else
                {
                    // 無効時：Silhouetteを除外したマテリアル配列を設定
                    if (materialsWithoutSilhouette.TryGetValue(renderer, out var withoutSilhouette))
                    {
                        renderer.sharedMaterials = withoutSilhouette;
                    }
                }
                appliedCount++;
            }
        }

        Debug.Log($"Applied silhouette state ({enabled}) to {appliedCount} renderers");
    }

    public void SetSilhouetteEnabled(bool enabled)
    {
        isSilhouetteEnabled = enabled;
        UpdateToggleValue();
        ApplyState();
    }
}
