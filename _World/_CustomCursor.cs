using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    [SerializeField] Texture2D cursorTex;
    // クリック位置（画像のどのピクセルが“先端”か）
    // 原点は画像の左上。中央にしたいなら (width/2, height/2)
    [SerializeField] Vector2 hotspot = Vector2.zero;

    void Start()
    {
        Cursor.SetCursor(cursorTex, hotspot, CursorMode.Auto);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void OnDisable()
    {
        // 元に戻す
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
