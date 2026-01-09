using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StoreToInventoryButton : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject buttonPrefab;            // ボタンのプレハブ
    public Vector3 offset = new Vector3(50, 50, 0); // オブジェクトからのオフセット
    public float buttonSize = 40f;             // ボタンのサイズ

    [Header("Button Sprites")]
    public Sprite normalSprite;                // 通常時のアイコン
    public Sprite hoverSprite;                 // ホバー時のアイコン
    public Sprite pressedSprite;               // クリック時のアイコン

    private PlacedFurniture targetFurniture;   // 対象の家具
    private GameObject buttonInstance;         // ボタンのインスタンス
    private Canvas uiCanvas;                   // UIキャンバス
    private Camera mainCamera;                 // メインカメラ
    private Button button;                     // ボタンコンポーネント
    private Image buttonImage;                 // ボタン画像

    void Start()
    {
        // コンポーネント取得
        targetFurniture = GetComponent<PlacedFurniture>();
        mainCamera = Camera.main;

        // UIキャンバスを取得または作成
        uiCanvas = FindFirstObjectByType<Canvas>();
        if (uiCanvas == null)
        {
            CreateUICanvas();
        }

        // ボタンを作成
        CreateStoreButton();

        // 初期は非表示
        if (buttonInstance != null)
            buttonInstance.SetActive(false);
    }

    // UIキャンバスを作成
    void CreateUICanvas()
    {
        GameObject canvasObj = new GameObject("StoreButtonCanvas");
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
    }

    // 収納ボタンを作成
    void CreateStoreButton()
    {
        // プレハブがある場合はそれを使用
        if (buttonPrefab != null)
        {
            buttonInstance = Instantiate(buttonPrefab, uiCanvas.transform);
        }
        else
        {
            // プレハブがない場合は動的に作成
            buttonInstance = new GameObject("StoreButton");
            buttonInstance.transform.SetParent(uiCanvas.transform, false);

            // ボタンコンポーネント追加
            buttonImage = buttonInstance.AddComponent<Image>();
            button = buttonInstance.AddComponent<Button>();

            // サイズ設定
            RectTransform rect = buttonInstance.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(buttonSize, buttonSize);

            // アイコン設定
            if (normalSprite != null)
                buttonImage.sprite = normalSprite;
        }

        // ボタンコンポーネント取得
        if (button == null)
            button = buttonInstance.GetComponent<Button>();
        if (buttonImage == null)
            buttonImage = buttonInstance.GetComponent<Image>();

        // クリックイベント設定
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(StoreToInventory);
        }

        // ボタンの状態変更設定
        SetupButtonStates();
    }

    // ボタンの状態変更設定
    void SetupButtonStates()
    {
        if (buttonInstance == null) return;

        // 既存のハンドラーを削除
        StoreButtonStateHandler oldHandler = buttonInstance.GetComponent<StoreButtonStateHandler>();
        if (oldHandler != null)
            Destroy(oldHandler);

        // 新しいハンドラーを追加
        StoreButtonStateHandler handler = buttonInstance.AddComponent<StoreButtonStateHandler>();
        handler.normalSprite = normalSprite;
        handler.hoverSprite = hoverSprite;
        handler.pressedSprite = pressedSprite;
        handler.buttonImage = buttonImage;
    }

    void Update()
    {
        if (targetFurniture == null || buttonInstance == null || mainCamera == null) return;

        // 選択状態に応じて表示/非表示
        buttonInstance.SetActive(targetFurniture.isSelected);

        if (targetFurniture.isSelected)
        {
            UpdateButtonPosition();
        }
    }

    // ボタン位置を更新
    void UpdateButtonPosition()
    {
        if (targetFurniture == null || buttonInstance == null) return;

        // オブジェクトの境界を取得
        Renderer renderer = targetFurniture.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        // オブジェクトの右上の位置を計算
        Vector3 worldPos = renderer.bounds.max;
        worldPos.y = renderer.bounds.max.y; // 上端

        // ワールド座標をスクリーン座標に変換
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // オフセットを適用
        screenPos += offset;

        // ボタン位置を設定
        RectTransform rect = buttonInstance.GetComponent<RectTransform>();
        rect.position = screenPos;

        // 画面外に出ないように制限
        ClampToScreen(rect);
    }

    // 画面内に収める
    void ClampToScreen(RectTransform rect)
    {
        Vector3 pos = rect.position;
        Vector2 size = rect.sizeDelta;

        // 画面サイズ取得
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // 位置を制限
        pos.x = Mathf.Clamp(pos.x, size.x / 2, screenWidth - size.x / 2);
        pos.y = Mathf.Clamp(pos.y, size.y / 2, screenHeight - size.y / 2);

        rect.position = pos;
    }

    // インベントリに収納
    void StoreToInventory()
    {
        if (targetFurniture == null || targetFurniture.furnitureData == null) return;

        // 配置システムから削除
        targetFurniture.StoreToInventory();

        // このコンポーネントも削除
        Destroy(buttonInstance);
        Destroy(this);
    }

    void OnDestroy()
    {
        // ボタンを削除
        if (buttonInstance != null)
            Destroy(buttonInstance);
    }
}

// ボタンの状態変更ハンドラー
public class StoreButtonStateHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite pressedSprite;
    public Image buttonImage;

    private bool isPressed = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isPressed && buttonImage != null && hoverSprite != null)
        {
            buttonImage.sprite = hoverSprite;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPressed && buttonImage != null && normalSprite != null)
        {
            buttonImage.sprite = normalSprite;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        if (buttonImage != null && pressedSprite != null)
        {
            buttonImage.sprite = pressedSprite;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        if (buttonImage != null && normalSprite != null)
        {
            buttonImage.sprite = normalSprite;
        }
    }
}
