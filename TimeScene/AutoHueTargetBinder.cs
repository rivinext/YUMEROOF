using UnityEngine;

public class AutoHueTargetBinder : MonoBehaviour
{
    [SerializeField] private MaterialHueController controller;

    private void OnEnable()
    {
        BindRenderers();
    }

    private void BindRenderers()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<MaterialHueController>();
        }

        if (controller == null)
        {
            return;
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            controller.RegisterRenderer(renderer);
        }
    }
}
