using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRayInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactionDistance = 2f;
    public LayerMask interactionLayers = ~0;
    public Vector3 originOffset = Vector3.up;
    public float horizontalFanAngle = 45f;
    public int horizontalRayCount = 5;
    public float verticalFanAngle = 45f;
    public int verticalRayCount = 3;
    [Tooltip("When enabled, trigger colliders will also be considered when casting for interactables.")]
    public bool includeTriggerColliders = true;
    [SerializeField, HideInInspector] private bool triggerInit;
    [SerializeField] private string interactableTriggerLayerName = "InteractableTrigger";

    public enum CastMode { Ray, Sphere, Box }
    public CastMode castMode = CastMode.Ray;
    public float sphereRadius = 0.5f;
    public Vector3 boxHalfExtents = Vector3.one * 0.5f;

    private IInteractable currentTarget;
    private IFocusableInteractable currentFocusable;
    private RayOutlineHighlighter highlighter;
    private bool skipHideOnce;
    private PlayerController playerController;
    [Header("UI Hooks")]
    [SerializeField] private InteractionUIController interactionUIController;
    private bool hasLoggedMissingUI;
    private bool hasLoggedMissingInteractableLayer;

    public event Action<IInteractable> TargetChanged;

    public InteractionUIController InteractionUI => interactionUIController;

    void Awake()
    {
        EnsureTriggerInitialized();
        highlighter = GetComponent<RayOutlineHighlighter>();
        EnsureOutlineLayerIncluded();
        EnsureInteractableLayerIncluded();
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        SceneManager.sceneLoaded += OnSceneLoaded;
        TryFindInteractionUIController();

    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureTriggerInitialized();
        EnsureOutlineLayerIncluded();
        EnsureInteractableLayerIncluded();
    }
#endif

    private void EnsureTriggerInitialized()
    {
        if (!triggerInit)
        {
            includeTriggerColliders = true;
            triggerInit = true;
        }
    }

    private void EnsureOutlineLayerIncluded()
    {
        if (highlighter == null)
            highlighter = GetComponent<RayOutlineHighlighter>();

        if (highlighter != null && highlighter.TryGetOutlineLayer(out int outlineLayerIndex))
        {
            int outlineMask = 1 << outlineLayerIndex;
            if ((interactionLayers.value & outlineMask) == 0)
            {
                interactionLayers |= outlineMask;
            }
        }
    }

    private void EnsureInteractableLayerIncluded()
    {
        if (string.IsNullOrEmpty(interactableTriggerLayerName))
        {
            return;
        }

        int layerIndex = LayerMask.NameToLayer(interactableTriggerLayerName);
        if (layerIndex < 0)
        {
            if (!hasLoggedMissingInteractableLayer)
            {
                Debug.LogWarning($"[PlayerRayInteractor] Layer '{interactableTriggerLayerName}' not found. Interaction trigger detection may fail.");
                hasLoggedMissingInteractableLayer = true;
            }
            return;
        }

        int layerMask = 1 << layerIndex;
        if ((interactionLayers.value & layerMask) == 0)
        {
            interactionLayers |= layerMask;
        }
    }

    void Update()
    {
        if (playerController != null && playerController.IsSitting)
        {
            if (currentTarget != null)
            {
                SetHighlight(currentTarget, false);
                NotifyBlur(currentTarget);
                currentTarget = null;
                NotifyTargetChanged(null);
                TargetChanged?.Invoke(null);
            }
            return;
        }

        IInteractable newTarget = FindBestTarget();
        if (newTarget != currentTarget)
        {
            bool newIsGhost = false;
            var mbNew = newTarget as MonoBehaviour;
            if (mbNew != null && mbNew.GetComponent<BuildingGhostInteractable>() != null)
                newIsGhost = true;

            if (currentTarget != null)
            {
                skipHideOnce = newIsGhost;
                SetHighlight(currentTarget, false);
                NotifyBlur(currentTarget);
            }

            currentTarget = newTarget;
            if (currentTarget != null)
            {
                if (currentTarget is DropMaterial dropTarget)
                {
                    SetHighlight(dropTarget, false);
                    dropTarget.Interact();
                    currentTarget = null;
                }
                else
                {
                    SetHighlight(currentTarget, true);
                    NotifyFocus(currentTarget);
                }
            }
            else
            {
                skipHideOnce = false;
                currentFocusable = null;
            }

            NotifyTargetChanged(currentTarget);

            TargetChanged?.Invoke(currentTarget);
        }

        if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
        {
            currentTarget.Interact();
        }
    }

    private IInteractable FindBestTarget()
    {
        IInteractable best = null;
        float bestDist = float.MaxValue;

        Vector3 origin = transform.position + originOffset;
        Vector3 forward = transform.forward;

        float hStep = horizontalRayCount > 1 ? horizontalFanAngle * 2f / (horizontalRayCount - 1) : 0f;
        float vStep = verticalRayCount > 1 ? verticalFanAngle * 2f / (verticalRayCount - 1) : 0f;
        for (int hi = 0; hi < horizontalRayCount; hi++)
        {
            float hAngle = horizontalRayCount > 1 ? -horizontalFanAngle + hStep * hi : 0f;
            Vector3 horizDir = Quaternion.AngleAxis(hAngle, transform.up) * forward;
            for (int vi = 0; vi < verticalRayCount; vi++)
            {
                float vAngle = verticalRayCount > 1 ? -verticalFanAngle + vStep * vi : 0f;
                Vector3 dir = Quaternion.AngleAxis(vAngle, transform.right) * horizDir;
                if (Cast(origin, dir, out RaycastHit hit))
                {
                    IInteractable trig = hit.collider.GetComponentInParent<IInteractable>();
                    if (trig != null && hit.distance < bestDist)
                    {
                        bestDist = hit.distance;
                        best = trig;
                    }
                }
            }
        }

        return best;
    }

    private void SetHighlight(IInteractable target, bool enabled)
    {
        var mb = target as MonoBehaviour;

        if (highlighter != null)
        {
            if (enabled && mb != null)
                highlighter.Highlight(mb.gameObject);
            else
                highlighter.Clear();
        }

        if (mb == null)
            return;

        var bed = mb.GetComponent<BedTrigger>();
        if (bed != null)
            bed.isPlayerNearby = enabled;

    }

    public void ReleaseHighlightIfCurrent(IInteractable target)
    {
        if (currentTarget == target)
        {
            SetHighlight(currentTarget, false);
        }
    }

    public void ClearFocusIfCurrent(IInteractable target)
    {
        if (currentTarget != target)
            return;

        SetHighlight(currentTarget, false);
        NotifyBlur(currentTarget);
        currentTarget = null;
        currentFocusable = null;
        NotifyTargetChanged(null);
        TargetChanged?.Invoke(null);
    }

    private void NotifyFocus(IInteractable target)
    {
        if (target is IFocusableInteractable focusable)
        {
            currentFocusable = focusable;
            focusable.OnFocus(this);
        }
    }

    public void SetInteractionUIController(InteractionUIController controller)
    {
        interactionUIController = controller;
        if (interactionUIController == null)
        {
            LogMissingUI();
        }
        else
        {
            hasLoggedMissingUI = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryFindInteractionUIController();
    }

    private void TryFindInteractionUIController()
    {
        if (interactionUIController != null)
            return;

        var found = FindFirstObjectByType<InteractionUIController>();
        if (found != null)
        {
            SetInteractionUIController(found);
        }
        else
        {
            LogMissingUI();
        }
    }

    private void NotifyTargetChanged(IInteractable target)
    {
        if (interactionUIController != null)
        {
            interactionUIController.HandleTargetChanged(target);
        }
        else
        {
            LogMissingUI();
        }
    }

    private void LogMissingUI()
    {
        if (hasLoggedMissingUI)
            return;

        Debug.LogWarning("[PlayerRayInteractor] InteractionUIController not found. Interaction UI updates will be skipped until assigned.");
        hasLoggedMissingUI = true;
    }

    private void NotifyBlur(IInteractable target)
    {
        if (target is IFocusableInteractable focusable)
        {
            focusable.OnBlur(this);
            if (currentFocusable == focusable)
            {
                currentFocusable = null;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + originOffset;
        Vector3 forward = transform.forward;
        float hStep = horizontalRayCount > 1 ? horizontalFanAngle * 2f / (horizontalRayCount - 1) : 0f;
        float vStep = verticalRayCount > 1 ? verticalFanAngle * 2f / (verticalRayCount - 1) : 0f;

        Gizmos.color = Color.yellow;
        for (int hi = 0; hi < horizontalRayCount; hi++)
        {
            float hAngle = horizontalRayCount > 1 ? -horizontalFanAngle + hStep * hi : 0f;
            Vector3 horizDir = Quaternion.AngleAxis(hAngle, transform.up) * forward;
            for (int vi = 0; vi < verticalRayCount; vi++)
            {
                float vAngle = verticalRayCount > 1 ? -verticalFanAngle + vStep * vi : 0f;
                Vector3 dir = Quaternion.AngleAxis(vAngle, transform.right) * horizDir;
                switch (castMode)
                {
                    case CastMode.Ray:
                        Gizmos.DrawRay(origin, dir * interactionDistance);
                        break;
                    case CastMode.Sphere:
                        Gizmos.DrawRay(origin, dir * interactionDistance);
                        Gizmos.DrawWireSphere(origin + dir * interactionDistance, sphereRadius);
                        break;
                    case CastMode.Box:
                        Gizmos.DrawRay(origin, dir * interactionDistance);
                        Matrix4x4 oldMatrix = Gizmos.matrix;
                        Gizmos.matrix = Matrix4x4.TRS(origin + dir * interactionDistance, transform.rotation, Vector3.one);
                        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);
                        Gizmos.matrix = oldMatrix;
                        break;
                }
            }
        }
    }

    private bool Cast(Vector3 origin, Vector3 direction, out RaycastHit hit)
    {
        QueryTriggerInteraction triggerInteraction = includeTriggerColliders
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        switch (castMode)
        {
            case CastMode.Sphere:
                return Physics.SphereCast(origin, sphereRadius, direction, out hit, interactionDistance, interactionLayers, triggerInteraction);
            case CastMode.Box:
                return Physics.BoxCast(origin, boxHalfExtents, direction, out hit, transform.rotation, interactionDistance, interactionLayers, triggerInteraction);
            default:
                return Physics.Raycast(origin, direction, out hit, interactionDistance, interactionLayers, triggerInteraction);
        }
    }
}
