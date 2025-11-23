using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TiledTextureScroller : MonoBehaviour
{
    [SerializeField]
    private Graphic targetGraphic;

    [SerializeField]
    private Renderer targetRenderer;

    [SerializeField]
    private Vector2 scrollSpeed = Vector2.zero;

    [SerializeField]
    private Vector2 tile = Vector2.one;

    [SerializeField]
    private bool duplicateMaterial = false;

    [SerializeField]
    private bool useUnscaledTime = false;

    private Vector2 currentOffset;
    private Material runtimeMaterial;

    private void Awake()
    {
        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (duplicateMaterial)
        {
            CreateRuntimeMaterial();
        }

        ApplyTextureTransform();
    }

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        currentOffset += scrollSpeed * deltaTime;
        currentOffset.x = Mathf.Repeat(currentOffset.x, 1f);
        currentOffset.y = Mathf.Repeat(currentOffset.y, 1f);

        ApplyTextureTransform();
    }

    private void CreateRuntimeMaterial()
    {
        Material sourceMaterial = null;

        if (targetGraphic != null)
        {
            sourceMaterial = targetGraphic.material;
            if (sourceMaterial != null)
            {
                runtimeMaterial = new Material(sourceMaterial);
                targetGraphic.material = runtimeMaterial;
            }
        }
        else if (targetRenderer != null)
        {
            sourceMaterial = targetRenderer.sharedMaterial;
            if (sourceMaterial == null)
            {
                sourceMaterial = targetRenderer.material;
            }

            if (sourceMaterial != null)
            {
                runtimeMaterial = new Material(sourceMaterial);
                targetRenderer.material = runtimeMaterial;
            }
        }
    }

    private void ApplyTextureTransform()
    {
        if (targetGraphic != null)
        {
            var material = duplicateMaterial ? runtimeMaterial : targetGraphic.material;
            if (material != null)
            {
                material.SetTextureOffset("_MainTex", currentOffset);
                material.SetTextureScale("_MainTex", tile);
            }
        }
        else if (targetRenderer != null)
        {
            var material = duplicateMaterial ? runtimeMaterial : targetRenderer.material;
            if (material != null)
            {
                material.mainTextureOffset = currentOffset;
                material.mainTextureScale = tile;
            }
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            if (targetGraphic != null && targetGraphic.material == runtimeMaterial)
            {
                targetGraphic.material = null;
            }

            if (targetRenderer != null && targetRenderer.material == runtimeMaterial)
            {
                targetRenderer.material = null;
            }

            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }
}
