using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmationPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Image backdropImage;
    [SerializeField] private Sprite backdropSprite;
    [SerializeField] private RectTransform panelRectTransform;

    private Action onYes;
    private Button backdropButton;
    private Canvas parentCanvas;
    private bool isOpen;
    private Vector2 onScreenPosition;

    private void Awake()
    {
        // Ensure required UI references exist even if they were not set in the inspector
        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>(true);

        if (yesButton == null || noButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0 && yesButton == null)
                yesButton = buttons[0];
            if (buttons.Length > 1 && noButton == null)
                noButton = buttons[1];
        }

        if (yesButton != null)
            yesButton.onClick.AddListener(HandleYes);
        if (noButton != null)
            noButton.onClick.AddListener(HandleNo);

        if (panelRectTransform == null)
        {
            panelRectTransform = GetComponent<RectTransform>();
        }

        if (panelRectTransform != null)
        {
            onScreenPosition = panelRectTransform.anchoredPosition;
            panelRectTransform.anchoredPosition = onScreenPosition;
        }

        if (backdropImage != null)
        {
            backdropButton = backdropImage.GetComponent<Button>();
            parentCanvas = backdropImage.canvas;
            if (backdropSprite != null)
                backdropImage.sprite = backdropSprite;
            SetBackdropActive(false);
        }
        else
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
    }

    private void OnDestroy()
    {
        if (yesButton != null)
            yesButton.onClick.RemoveListener(HandleYes);
        if (noButton != null)
            noButton.onClick.RemoveListener(HandleNo);
        if (backdropButton != null)
            backdropButton.onClick.RemoveListener(HandleBackdropClicked);
    }

    public void Open(string message, Action onYes)
    {
        isOpen = true;
        gameObject.SetActive(true);
        if (panelRectTransform != null)
            panelRectTransform.anchoredPosition = onScreenPosition;
        if (messageText != null)
            messageText.text = message;
        this.onYes = onYes;
        if (backdropSprite != null && backdropImage != null)
            backdropImage.sprite = backdropSprite;
        SetBackdropActive(true);
        RegisterBackdropListener();
    }

    private void HandleYes()
    {
        if (!isOpen)
            return;
        onYes?.Invoke();
        Close();
    }

    private void HandleNo()
    {
        if (!isOpen)
            return;
        Close();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        UnregisterBackdropListener();
        SetBackdropActive(false);

        gameObject.SetActive(false);
        onYes = null;
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleNo();
            return;
        }

        if (backdropButton != null)
            return;

        if (panelRectTransform == null)
            return;

        bool pointerDown = false;
        Vector2 pointerPosition = Vector2.zero;

        if (Input.GetMouseButtonDown(0))
        {
            pointerDown = true;
            pointerPosition = Input.mousePosition;
        }
        else if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                pointerDown = true;
                pointerPosition = touch.position;
            }
        }

        if (!pointerDown)
            return;

        Camera uiCamera = parentCanvas != null ? parentCanvas.worldCamera : null;
        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRectTransform, pointerPosition, uiCamera))
            HandleNo();
    }

    private void RegisterBackdropListener()
    {
        if (backdropButton == null)
            return;
        backdropButton.onClick.RemoveListener(HandleBackdropClicked);
        backdropButton.onClick.AddListener(HandleBackdropClicked);
    }

    private void UnregisterBackdropListener()
    {
        if (backdropButton == null)
            return;
        backdropButton.onClick.RemoveListener(HandleBackdropClicked);
    }

    private void HandleBackdropClicked()
    {
        HandleNo();
    }

    private void SetBackdropActive(bool isActive)
    {
        if (backdropImage == null)
            return;

        if (backdropImage.gameObject.activeSelf != isActive)
        {
            backdropImage.gameObject.SetActive(isActive);
        }
        else
        {
            backdropImage.enabled = isActive;
        }
    }
}
