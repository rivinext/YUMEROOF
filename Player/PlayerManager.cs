using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager instance;

    void Awake()
    {
        // シングルトンパターンでプレイヤーを管理
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // 既にプレイヤーが存在する場合は新しい方を削除
            Debug.Log("Duplicate player found, destroying new instance");
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Start()
    {
        // タグの確認
        if (!CompareTag("Player"))
        {
            Debug.LogWarning("Player object doesn't have 'Player' tag!");
            tag = "Player";
        }
    }

    [SerializeField]
    private bool showDebugPosition = false;

    // デバッグ用：現在の位置を表示
    void OnGUI()
    {
        if (!showDebugPosition)
        {
            return;
        }

        GUI.Label(new Rect(10, 10, 300, 20), $"Player Pos: {transform.position}");
    }

    [System.Serializable]
    public class PlayerData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    /// <summary>
    /// 現在のプレイヤーの位置と回転を収集し、セーブデータとして返す。
    /// </summary>
    public PlayerData GetSaveData()
    {
        return new PlayerData
        {
            position = transform.position,
            rotation = transform.rotation
        };
    }

    /// <summary>
    /// セーブデータに保存されている位置と回転をプレイヤーに適用する。
    /// </summary>
    public void ApplySaveData(PlayerData data)
    {
        if (data == null) return;
        transform.position = data.position;
        transform.rotation = data.rotation;
    }
}
