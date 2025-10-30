using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WardrobePreviewController : MonoBehaviour
{
    private const int InvalidPointerId = -1;

    [SerializeField] private Transform previewPlayerRoot;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RawImage previewTargetImage;
    [SerializeField] private float previewRotateSpeed = 0.25f;
    [SerializeField] private bool previewUseDeltaTime = false;
    [SerializeField] private float previewZoomSpeed = 0.25f;
    [SerializeField] private float previewMinZoomDistance = 0.5f;
    [SerializeField] private float previewMaxZoomDistance = 3f;

    private bool isDraggingPreview;
    private int activePointerId = InvalidPointerId;
    private Vector2 lastPointerPosition;
    private Quaternion previewInitialRotation;
    private bool previewInitialRotationCaptured;
    private Vector3 previewCameraZoomDirection = Vector3.back;
    private float previewInitialCameraDistance;
    private float previewCurrentZoomDistance;
    private bool previewInitialCameraPositionCaptured;

    public Transform PreviewPlayerRoot => previewPlayerRoot;

    public void InitializePreviewTarget()
    {
        if (previewTargetImage != null && previewCamera != null)
        {
            previewTargetImage.texture = previewCamera.targetTexture;
        }
    }

    public void UpdatePreviewActivation(bool visible)
    {
        if (previewPlayerRoot != null)
        {
            previewPlayerRoot.gameObject.SetActive(visible);
        }

        if (previewCamera != null)
        {
            previewCamera.gameObject.SetActive(visible);
        }

        if (visible)
        {
            InitializePreviewTarget();
            CapturePreviewInitialRotation();
            if (!previewInitialCameraPositionCaptured)
            {
                CapturePreviewInitialZoom();
            }
            else
            {
                ApplyPreviewZoom(previewCurrentZoomDistance);
            }
        }
        else
        {
            ResetPreviewInteractionState();
            RestorePreviewRotation();
            RestorePreviewZoom();
        }
    }

    public void CapturePreviewInitialRotation()
    {
        if (previewPlayerRoot == null)
        {
            return;
        }

        previewInitialRotation = previewPlayerRoot.rotation;
        previewInitialRotationCaptured = true;
    }

    public void CapturePreviewInitialZoom()
    {
        if (previewCamera == null)
        {
            return;
        }

        Transform cameraTransform = previewCamera.transform;
        Vector3 initialLocalPosition = cameraTransform.localPosition;
        previewInitialCameraDistance = initialLocalPosition.magnitude;

        if (previewInitialCameraDistance > Mathf.Epsilon)
        {
            previewCameraZoomDirection = initialLocalPosition / previewInitialCameraDistance;
        }
        else
        {
            previewInitialCameraDistance = Mathf.Max(previewInitialCameraDistance, 0.01f);
            previewCameraZoomDirection = Vector3.back;
        }

        previewInitialCameraPositionCaptured = true;
        previewCurrentZoomDistance = Mathf.Clamp(previewInitialCameraDistance, previewMinZoomDistance, previewMaxZoomDistance);
        ApplyPreviewZoom(previewCurrentZoomDistance);
    }

    public void SetupPreviewEventTrigger()
    {
        if (previewTargetImage == null)
        {
            return;
        }

        EventTrigger eventTrigger = previewTargetImage.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = previewTargetImage.gameObject.AddComponent<EventTrigger>();
        }

        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        AddEventTriggerListener(eventTrigger, EventTriggerType.PointerDown, OnPreviewPointerDown);
        AddEventTriggerListener(eventTrigger, EventTriggerType.Drag, OnPreviewDrag);
        AddEventTriggerListener(eventTrigger, EventTriggerType.PointerUp, OnPreviewPointerUp);
        AddEventTriggerListener(eventTrigger, EventTriggerType.PointerExit, OnPreviewPointerExit);
        AddEventTriggerListener(eventTrigger, EventTriggerType.Cancel, OnPreviewPointerCancel);
        AddEventTriggerListener(eventTrigger, EventTriggerType.Scroll, OnPreviewScroll);
    }

    private void ApplyPreviewZoom(float distance)
    {
        if (!previewInitialCameraPositionCaptured || previewCamera == null)
        {
            return;
        }

        float clampedDistance = Mathf.Clamp(distance, previewMinZoomDistance, previewMaxZoomDistance);
        Transform cameraTransform = previewCamera.transform;
        cameraTransform.localPosition = previewCameraZoomDirection * clampedDistance;
        previewCurrentZoomDistance = clampedDistance;
    }

    private void RestorePreviewRotation()
    {
        if (!previewInitialRotationCaptured || previewPlayerRoot == null)
        {
            return;
        }

        previewPlayerRoot.rotation = previewInitialRotation;
    }

    private void RestorePreviewZoom()
    {
        if (!previewInitialCameraPositionCaptured)
        {
            return;
        }

        ApplyPreviewZoom(previewInitialCameraDistance);
    }

    private void ResetPreviewInteractionState()
    {
        isDraggingPreview = false;
        lastPointerPosition = Vector2.zero;
        activePointerId = InvalidPointerId;
    }

    private void AddEventTriggerListener(EventTrigger trigger, EventTriggerType eventType, Action<PointerEventData> callback)
    {
        if (trigger == null || callback == null)
        {
            return;
        }

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(data =>
        {
            PointerEventData pointerEventData = data as PointerEventData;
            if (pointerEventData != null)
            {
                callback(pointerEventData);
            }
        });
        trigger.triggers.Add(entry);
    }

    private void OnPreviewPointerDown(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        isDraggingPreview = true;
        activePointerId = eventData.pointerId;
        lastPointerPosition = eventData.position;
    }

    private void OnPreviewPointerUp(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        ResetPreviewInteractionState();
    }

    private void OnPreviewPointerExit(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId == activePointerId)
        {
            lastPointerPosition = eventData.position;
        }
    }

    private void OnPreviewPointerCancel(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        ResetPreviewInteractionState();
    }

    private void OnPreviewDrag(PointerEventData eventData)
    {
        if (!isDraggingPreview || previewPlayerRoot == null || eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        Vector2 delta = eventData.position - lastPointerPosition;
        lastPointerPosition = eventData.position;

        float yaw = -delta.x * previewRotateSpeed;
        if (previewUseDeltaTime)
        {
            yaw *= Time.deltaTime;
        }

        previewPlayerRoot.Rotate(0f, yaw, 0f, Space.World);
    }

    private void OnPreviewScroll(PointerEventData eventData)
    {
        if (eventData == null || !previewInitialCameraPositionCaptured || previewCamera == null)
        {
            return;
        }

        float scrollDelta = eventData.scrollDelta.y * previewZoomSpeed;
        if (Mathf.Approximately(scrollDelta, 0f))
        {
            return;
        }

        float targetDistance = previewCurrentZoomDistance - scrollDelta;
        ApplyPreviewZoom(targetDistance);
    }
}
