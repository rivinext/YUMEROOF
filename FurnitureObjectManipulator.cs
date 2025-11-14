using UnityEngine;

public class ObjectManipulator : MonoBehaviour
{
    private Transform selectedObjectTransform;
    private Collider selectedObjectCollider;
    public LayerMask groundLayer;
    public float rotationSpeed = 5f;

    private bool isMoving = false;
    private Vector3 originalPosition; // 新規追加: 元の位置を保存する変数

    // 移動を開始するメソッド
    public void StartMoving(Transform objTransform)
    {
        selectedObjectTransform = objTransform;
        selectedObjectCollider = selectedObjectTransform != null
            ? selectedObjectTransform.GetComponentInChildren<Collider>()
            : null;
        isMoving = true;
        originalPosition = selectedObjectTransform.position; // 移動開始時に元の位置を保存
    }

    // 選択を解除するメソッド
    public void DeselectObject()
    {
        selectedObjectTransform = null;
        selectedObjectCollider = null;
        isMoving = false;
    }

    void Update()
    {
        if (selectedObjectTransform != null && isMoving)
        {
            // オブジェクトの移動
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                Vector3 targetPosition = hit.point;

                if (selectedObjectCollider != null)
                {
                    Vector3 normal = hit.normal.normalized;
                    Vector3 absNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
                    Vector3 extents = selectedObjectCollider.bounds.extents;
                    float offsetDistance = Vector3.Dot(extents, absNormal);
                    Vector3 centerOffset = selectedObjectCollider.bounds.center - selectedObjectTransform.position;

                    targetPosition = hit.point + normal * offsetDistance - centerOffset;
                }

                selectedObjectTransform.position = targetPosition;
            }

            // オブジェクトの回転
            float scrollInput = Input.mouseScrollDelta.y;
            if (scrollInput != 0)
            {
                selectedObjectTransform.Rotate(Vector3.up, -scrollInput * rotationSpeed, Space.Self);
            }

            // Escキーで移動をキャンセル
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                selectedObjectTransform.position = originalPosition; // 元の位置に戻す
                DeselectObject(); // 選択を解除して移動を停止
            }
        }
    }
}
