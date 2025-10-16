using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SceneArea : MonoBehaviour
{
    [Header("Area Settings")]
    public string targetSceneName;      // 遷移先シーン名
    public string spawnPointName;       // 遷移先のスポーンポイント名

    [Header("Trigger Settings")]
    public bool requireKeyPress = false; // Eキーが必要か
    public GameObject interactionPrompt; // Eキー表示UI（requireKeyPress=trueの場合）

    private bool isPlayerInArea = false;
    private BoxCollider areaCollider;

    void Start()
    {
        // コライダー設定
        areaCollider = GetComponent<BoxCollider>();
        areaCollider.isTrigger = true;

        // インタラクションUIを初期非表示
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        // エリアを半透明で表示（デバッグ用）
        SetupDebugVisual();
    }

    void SetupDebugVisual()
    {
        if (Application.isPlaying) return;
        // エディタでのみ表示される半透明のキューブ
        #if UNITY_EDITOR
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one;

            renderer = visual.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0, 1, 0, 0.3f); // 緑色半透明
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;

            // コライダーは削除（親のBoxColliderを使用）
            Destroy(visual.GetComponent<BoxCollider>());
        }
        #endif
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInArea = true;

            if (requireKeyPress)
            {
                // Eキーが必要な場合はUIを表示
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(true);
            }
            else
            {
                // 即座に遷移
                if (!SceneTransitionManager.Instance.IsTransitioning)
                {
                    SceneTransitionManager.Instance.TransitionToSceneInstant(targetSceneName, spawnPointName);
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInArea = false;

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }
    }

    void Update()
    {
        // Eキーが必要な場合の処理
        if (requireKeyPress && isPlayerInArea && Input.GetKeyDown(KeyCode.E))
        {
            if (!SceneTransitionManager.Instance.IsTransitioning)
            {
                SceneTransitionManager.Instance.TransitionToScene(targetSceneName, spawnPointName, false);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        // エディタでエリア範囲を可視化
        if (areaCollider == null)
            areaCollider = GetComponent<BoxCollider>();

        if (areaCollider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            Gizmos.DrawCube(areaCollider.center, areaCollider.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(areaCollider.center, areaCollider.size);
            Gizmos.matrix = oldMatrix;
        }
    }
}
