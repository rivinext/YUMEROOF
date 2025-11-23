using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class PlayerProximityInteractor : MonoBehaviour
{
    private const string InteractableLayerName = "Interactable";

    [Header("Detection")]
    [SerializeField] private string interactableTag = "Interactable";
    [SerializeField] private bool filterByTag = false;

    [Header("UI Hooks")]
    [SerializeField] private InteractionUIController interactionUIController;

    private readonly Dictionary<IInteractable, int> overlapCounts = new();
    private readonly Dictionary<IInteractable, float> interactableDistances = new();
    private readonly Dictionary<IInteractable, Collider> lastKnownCollider = new();

    private IInteractable currentTarget;
    private IFocusableInteractable currentFocusable;
    private RayOutlineHighlighter highlighter;
    private PlayerController playerController;
    private bool skipHideOnce;
    private int interactableLayer = -1;

    public event Action<IInteractable> TargetChanged;

    public InteractionUIController InteractionUI => interactionUIController;

    private void Awake()
    {
        interactableLayer = LayerMask.NameToLayer(InteractableLayerName);
        highlighter = GetComponent<RayOutlineHighlighter>();
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        EnsureTriggerCollider();
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindInteractionUIController();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (playerController != null && playerController.IsSitting)
        {
            ClearCurrentTarget();
            return;
        }

        EvaluateTarget();

        if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
        {
            currentTarget.Interact();
        }
    }

    private void BindInteractionUIController()
    {
        if (interactionUIController == null)
        {
            interactionUIController = FindFirstObjectByType<InteractionUIController>();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindInteractionUIController();
    }

    private void EnsureTriggerCollider()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)trigger).radius = 1.5f;
        }

        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrackInteractable(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTrackInteractable(other, false);
    }

    private void OnTriggerExit(Collider other)
    {
        var interactable = GetInteractable(other);
        if (interactable == null)
            return;

        if (!overlapCounts.ContainsKey(interactable))
            return;

        overlapCounts[interactable] = Mathf.Max(0, overlapCounts[interactable] - 1);
        if (overlapCounts[interactable] == 0)
        {
            overlapCounts.Remove(interactable);
            interactableDistances.Remove(interactable);
            lastKnownCollider.Remove(interactable);
            EvaluateTarget();
        }
    }

    private void TryTrackInteractable(Collider other, bool isEnter)
    {
        var interactable = GetInteractable(other);
        if (interactable == null)
            return;

        if (isEnter)
        {
            if (!overlapCounts.ContainsKey(interactable))
            {
                overlapCounts[interactable] = 0;
            }

            overlapCounts[interactable]++;
        }
        else if (!overlapCounts.ContainsKey(interactable))
        {
            overlapCounts[interactable] = 1;
        }
        UpdateDistance(interactable, other);

        if (isEnter)
        {
            EvaluateTarget();
        }
    }

    private IInteractable GetInteractable(Collider collider)
    {
        if (collider == null)
            return null;

        if (filterByTag && !collider.CompareTag(interactableTag))
            return null;

        if (interactableLayer >= 0 && collider.gameObject.layer != interactableLayer)
            return null;

        return collider.GetComponentInParent<IInteractable>();
    }

    private void UpdateDistance(IInteractable interactable, Collider collider)
    {
        if (interactable == null || collider == null)
            return;

        Vector3 origin = transform.position;
        float distance = Vector3.Distance(origin, collider.ClosestPoint(origin));
        interactableDistances[interactable] = distance;
        lastKnownCollider[interactable] = collider;
    }

    private void EvaluateTarget()
    {
        if (interactableDistances.Count == 0)
        {
            ClearCurrentTarget();
            return;
        }

        IInteractable best = null;
        float bestDistance = float.MaxValue;
        foreach (var pair in interactableDistances)
        {
            if (pair.Value < bestDistance)
            {
                bestDistance = pair.Value;
                best = pair.Key;
            }
        }

        if (best != currentTarget)
        {
            SetCurrentTarget(best);
        }
        else if (best != null && lastKnownCollider.TryGetValue(best, out var collider))
        {
            UpdateDistance(best, collider);
        }
    }

    private void SetCurrentTarget(IInteractable newTarget)
    {
        if (currentTarget != null)
        {
            skipHideOnce = false;
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
                RemoveCandidate(dropTarget);
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
            currentFocusable = null;
        }

        if (interactionUIController != null)
        {
            interactionUIController.HandleTargetChanged(currentTarget);
        }

        TargetChanged?.Invoke(currentTarget);
    }

    private void ClearCurrentTarget()
    {
        if (currentTarget == null)
            return;

        SetHighlight(currentTarget, false);
        NotifyBlur(currentTarget);
        currentTarget = null;
        currentFocusable = null;
        interactionUIController?.HandleTargetChanged(null);
        TargetChanged?.Invoke(null);
    }

    private void RemoveCandidate(IInteractable target)
    {
        if (target == null)
            return;

        overlapCounts.Remove(target);
        interactableDistances.Remove(target);
        lastKnownCollider.Remove(target);
    }

    private void SetHighlight(IInteractable target, bool enabled)
    {
        var mb = target as MonoBehaviour;

        if (highlighter != null)
        {
            if (enabled && mb != null)
                highlighter.Highlight(mb.gameObject);
            else if (!skipHideOnce)
                highlighter.Clear();
        }

        skipHideOnce = false;

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
            skipHideOnce = true;
            SetHighlight(currentTarget, false);
        }
    }

    public void ClearFocusIfCurrent(IInteractable target)
    {
        if (currentTarget != target)
            return;

        ClearCurrentTarget();
    }

    private void NotifyFocus(IInteractable target)
    {
        if (target is IFocusableInteractable focusable)
        {
            currentFocusable = focusable;
            focusable.OnFocus(this);
        }
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
}
