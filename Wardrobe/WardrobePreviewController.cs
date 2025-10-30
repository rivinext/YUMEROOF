using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WardrobePreviewController : MonoBehaviour
{
    private const int InvalidPointerId = -1;

    [Header("Preview Objects")]
    [SerializeField] private Transform previewPlayerRoot;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RawImage previewTargetImage;

    [Header("Anchor Settings")]
    [SerializeField] private Transform focusAnchor;
    [SerializeField] private Vector3 anchorOffset = Vector3.zero;
    [SerializeField] private bool offsetIsLocal = true;

    [Header("Orbit Settings")]
    [SerializeField] private float dragSensitivity = 0.3f;
    [SerializeField] private bool invertHorizontalDrag = false;
    [SerializeField] private bool invertVerticalDrag = false;
    [SerializeField, Range(0f, 89.5f)] private float minVerticalAngle = 0f;
    [SerializeField, Range(0f, 89.5f)] private float maxVerticalAngle = 85f;

    private bool isDragging;
    private int activePointerId = InvalidPointerId;
    private Vector2 lastPointerPosition;

    private float yaw;
    private float pitch;
    private float initialYaw;
    private float initialPitch;
    private float orbitRadius = 2f;
    private bool orbitInitialized;

    private bool previewActive;

    private Quaternion initialPlayerRotation;
    private bool initialPlayerRotationCaptured;

    public Transform PreviewPlayerRoot => previewPlayerRoot;

    public Transform FocusAnchor
    {
        get => focusAnchor;
        set
        {
            if (focusAnchor == value)
            {
                return;
            }

            focusAnchor = value;
            HandleAnchorConfigurationChanged();
        }
    }

    private void HandleAnchorConfigurationChanged()
    {
        if (previewActive)
        {
            CaptureOrbitFromCurrentCamera();
            RestoreInitialOrbit();
        }
        else
        {
            orbitInitialized = false;
        }
    }

    private void OnValidate()
    {
        dragSensitivity = Mathf.Max(0f, dragSensitivity);
        maxVerticalAngle = Mathf.Clamp(maxVerticalAngle, 0f, 89.5f);
        minVerticalAngle = Mathf.Clamp(minVerticalAngle, 0f, maxVerticalAngle);
    }

    private void LateUpdate()
    {
        if (!previewActive || !orbitInitialized)
        {
            return;
        }

        ApplyOrbitToCamera();
    }

    public void InitializePreviewTarget()
    {
        if (previewTargetImage != null && previewCamera != null)
        {
            previewTargetImage.texture = previewCamera.targetTexture;
        }
    }

    public void CapturePreviewInitialRotation()
    {
        if (previewPlayerRoot == null)
        {
            initialPlayerRotationCaptured = false;
            return;
        }

        initialPlayerRotation = previewPlayerRoot.rotation;
        initialPlayerRotationCaptured = true;
    }

    public void CapturePreviewInitialZoom()
    {
        CaptureOrbitFromCurrentCamera();
        RestoreInitialOrbit();
    }

    public void UpdatePreviewActivation(bool visible)
    {
        previewActive = visible;

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

            if (!orbitInitialized)
            {
                CaptureOrbitFromCurrentCamera();
            }

            RestoreInitialOrbit();
        }
        else
        {
            ResetDragState();
            RestoreInitialPlayerRotation();
        }
    }

    public void SetupPreviewEventTrigger()
    {
        if (previewTargetImage == null)
        {
            return;
        }

        EventTrigger trigger = previewTargetImage.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = previewTargetImage.gameObject.AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers = new List<EventTrigger.Entry>();
        }
        else
        {
            trigger.triggers.Clear();
        }

        AddEventTriggerListener(trigger, EventTriggerType.PointerDown, OnPreviewPointerDown);
        AddEventTriggerListener(trigger, EventTriggerType.PointerUp, OnPreviewPointerUp);
        AddEventTriggerListener(trigger, EventTriggerType.PointerExit, OnPreviewPointerExit);
        AddEventTriggerListener(trigger, EventTriggerType.PointerCancel, OnPreviewPointerCancel);
        AddEventTriggerListener(trigger, EventTriggerType.Drag, OnPreviewDrag);
    }

    public void SetFocusAnchor(Transform anchor, Vector3? offsetOverride = null, bool? useLocalOffset = null)
    {
        bool offsetChanged = false;

        if (offsetOverride.HasValue)
        {
            anchorOffset = offsetOverride.Value;
            offsetChanged = true;
        }

        if (useLocalOffset.HasValue)
        {
            offsetIsLocal = useLocalOffset.Value;
            offsetChanged = true;
        }

        bool anchorChanged = focusAnchor != anchor;
        if (anchorChanged)
        {
            FocusAnchor = anchor;
        }
        else if (offsetChanged)
        {
            HandleAnchorConfigurationChanged();
        }
    }

    private void CaptureOrbitFromCurrentCamera()
    {
        if (previewCamera == null)
        {
            orbitInitialized = false;
            return;
        }

        Vector3 anchorPosition = GetAnchorPosition();
        Transform cameraTransform = previewCamera.transform;
        Vector3 direction = cameraTransform.position - anchorPosition;

        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            direction = -cameraTransform.forward;
        }

        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            direction = Vector3.forward;
        }

        orbitRadius = direction.magnitude;
        if (orbitRadius < 0.01f)
        {
            orbitRadius = 0.01f;
        }

        direction.Normalize();

        yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;

        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        yaw = NormalizeAngle(yaw);

        initialYaw = yaw;
        initialPitch = pitch;

        orbitInitialized = true;
    }

    private void RestoreInitialOrbit()
    {
        if (!orbitInitialized)
        {
            return;
        }

        yaw = initialYaw;
        pitch = initialPitch;
        ApplyOrbitToCamera();
    }

    private void ApplyOrbitToCamera()
    {
        if (!orbitInitialized || previewCamera == null)
        {
            return;
        }

        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        yaw = NormalizeAngle(yaw);

        Vector3 anchorPosition = GetAnchorPosition();
        Vector3 direction = GetDirectionFromAngles(yaw, pitch);

        Transform cameraTransform = previewCamera.transform;
        cameraTransform.position = anchorPosition + direction * orbitRadius;

        Vector3 up = Vector3.up;
        float alignment = Mathf.Abs(Vector3.Dot(direction, up));
        if (alignment > 0.999f && previewPlayerRoot != null)
        {
            up = previewPlayerRoot.up;
        }

        cameraTransform.rotation = Quaternion.LookRotation(-direction, up);
    }

    private void RestoreInitialPlayerRotation()
    {
        if (!initialPlayerRotationCaptured || previewPlayerRoot == null)
        {
            return;
        }

        previewPlayerRoot.rotation = initialPlayerRotation;
    }

    private void ResetDragState()
    {
        isDragging = false;
        activePointerId = InvalidPointerId;
        lastPointerPosition = Vector2.zero;
    }

    private Vector3 GetAnchorPosition()
    {
        Transform anchor = focusAnchor != null ? focusAnchor : previewPlayerRoot;
        if (anchor == null)
        {
            return anchorOffset;
        }

        return offsetIsLocal ? anchor.TransformPoint(anchorOffset) : anchor.position + anchorOffset;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f)
        {
            angle += 360f;
        }

        return angle;
    }

    private static Vector3 GetDirectionFromAngles(float yawDegrees, float pitchDegrees)
    {
        float yawRadians = yawDegrees * Mathf.Deg2Rad;
        float pitchRadians = pitchDegrees * Mathf.Deg2Rad;
        float cosPitch = Mathf.Cos(pitchRadians);

        return new Vector3(
            Mathf.Sin(yawRadians) * cosPitch,
            Mathf.Sin(pitchRadians),
            Mathf.Cos(yawRadians) * cosPitch
        );
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

        isDragging = true;
        activePointerId = eventData.pointerId;
        lastPointerPosition = eventData.position;
    }

    private void OnPreviewPointerUp(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        ResetDragState();
    }

    private void OnPreviewPointerExit(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        lastPointerPosition = eventData.position;
    }

    private void OnPreviewPointerCancel(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        ResetDragState();
    }

    private void OnPreviewDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        Vector2 delta = eventData.position - lastPointerPosition;
        lastPointerPosition = eventData.position;

        float horizontalSign = invertHorizontalDrag ? -1f : 1f;
        float verticalSign = invertVerticalDrag ? -1f : 1f;

        yaw += delta.x * dragSensitivity * horizontalSign;
        pitch += delta.y * dragSensitivity * verticalSign;

        ApplyOrbitToCamera();
    }
}
