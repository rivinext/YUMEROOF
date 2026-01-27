using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ImageTiledTextureScroller : MonoBehaviour
{
    [SerializeField]
    private Image targetImage;

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
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
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
        if (targetImage == null)
        {
            return;
        }

        Material sourceMaterial = targetImage.material;
        if (sourceMaterial == null)
        {
            return;
        }

        runtimeMaterial = new Material(sourceMaterial);
        targetImage.material = runtimeMaterial;
    }

    private void ApplyTextureTransform()
    {
        if (targetImage == null)
        {
            return;
        }

        var material = duplicateMaterial ? runtimeMaterial : targetImage.material;
        if (material == null)
        {
            return;
        }

        material.SetTextureOffset("_MainTex", currentOffset);
        material.SetTextureScale("_MainTex", tile);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        if (targetImage != null && targetImage.material == runtimeMaterial)
        {
            targetImage.material = null;
        }

        Destroy(runtimeMaterial);
        runtimeMaterial = null;
    }
}
