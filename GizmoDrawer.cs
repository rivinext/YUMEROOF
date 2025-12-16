using UnityEngine;

[ExecuteAlways]
public class GizmoDrawer : MonoBehaviour
{
    [Header("Gizmo Shape")]
    [Min(0f)]
    public float radius = 0.2f;

    [Min(0f)]
    public Vector3 boxSize = Vector3.one * 0.25f;

    public bool drawSphere = true;
    public bool drawWireShape;
    public bool drawCube;

    [Header("Appearance")]
    public Color gizmoColor = Color.cyan;
    public Vector3 offset = Vector3.zero;
    public bool drawAxes;

    [Header("Icon")]
    public bool drawIcon;
    public string iconName = "sv_label_0";

    private void OnValidate()
    {
        radius = Mathf.Max(0f, radius);
        boxSize = new Vector3(Mathf.Max(0f, boxSize.x), Mathf.Max(0f, boxSize.y), Mathf.Max(0f, boxSize.z));
    }

    private void OnDrawGizmos()
    {
        Vector3 position = transform.TransformPoint(offset);
        Gizmos.color = gizmoColor;

        if (drawCube)
        {
            if (drawWireShape)
            {
                Gizmos.DrawWireCube(position, boxSize);
            }
            else
            {
                Gizmos.DrawCube(position, boxSize);
            }
        }
        else if (drawSphere)
        {
            if (drawWireShape)
            {
                Gizmos.DrawWireSphere(position, radius);
            }
            else
            {
                Gizmos.DrawSphere(position, radius);
            }
        }

        if (drawAxes)
        {
            DrawAxisLines(position);
        }

        if (drawIcon && !string.IsNullOrEmpty(iconName))
        {
            Gizmos.DrawIcon(position, iconName, true, gizmoColor);
        }
    }

    private void DrawAxisLines(Vector3 position)
    {
        const float axisLength = 0.5f;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + transform.right * axisLength);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(position, position + transform.up * axisLength);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(position, position + transform.forward * axisLength);
        Gizmos.color = gizmoColor;
    }
}
