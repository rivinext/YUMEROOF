using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InteractionPromptBillboard : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    private float offsetY;

    private readonly List<Renderer> targetRenderers = new List<Renderer>();
    private readonly List<Collider> targetColliders = new List<Collider>();

    public Transform Target
    {
        get => target;
        set
        {
            if (target == value)
            {
                return;
            }

            target = value;
            RefreshTargetComponents();
        }
    }

    public float OffsetY
    {
        get => offsetY;
        set => offsetY = value;
    }

    private void Awake()
    {
        RefreshTargetComponents();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (!TryGetCombinedBounds(out Bounds combinedBounds))
        {
            return;
        }

        Vector3 boundsCenter = combinedBounds.center;
        Vector3 newPosition = new Vector3(boundsCenter.x, combinedBounds.min.y + offsetY, boundsCenter.z);
        transform.position = newPosition;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Transform cameraTransform = mainCamera.transform;
        transform.forward = cameraTransform.forward;
    }

    private void RefreshTargetComponents()
    {
        targetRenderers.Clear();
        targetColliders.Clear();

        if (target == null)
        {
            return;
        }

        targetRenderers.AddRange(target.GetComponentsInChildren<Renderer>());
        targetColliders.AddRange(target.GetComponentsInChildren<Collider>());
    }

    private bool TryGetCombinedBounds(out Bounds combinedBounds)
    {
        combinedBounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        foreach (Collider collider in targetColliders)
        {
            if (collider == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        RefreshTargetComponents();
    }
#endif
}
