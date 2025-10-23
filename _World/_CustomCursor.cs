using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    static CustomCursor instance;

    [SerializeField] Texture2D cursorTex;
    // クリック位置（画像のどのピクセルが“先端”か）
    // 原点は画像の左上。中央にしたいなら (width/2, height/2)
    [SerializeField] Vector2 hotspot = Vector2.zero;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyCursor();
    }

    void Start()
    {
        // シーンロード時に Awake でカーソルを設定済みだが、
        // 既存シーンで追加された場合にも確実に適用する。
        ApplyCursor();
    }

    void ApplyCursor()
    {
        if (cursorTex == null)
        {
            return;
        }

        Cursor.SetCursor(cursorTex, hotspot, CursorMode.Auto);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
