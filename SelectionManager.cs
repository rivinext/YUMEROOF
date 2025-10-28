using UnityEngine;
using System.Collections.Generic;

public class SelectionManager : MonoBehaviour
{
    public LayerMask selectableLayers;
    public ObjectManipulator objectManipulator;

    private GameObject currentSelectedObject;
    private PlacementPreview currentPlacementPreview;

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayers))
            {
                if (currentSelectedObject == hit.collider.gameObject)
                {
                    return;
                }
                else
                {
                    if (currentPlacementPreview != null)
                    {
                        currentPlacementPreview.SetSelected(false);
                    }
                    if (objectManipulator != null)
                    {
                        objectManipulator.DeselectObject();
                    }

                    currentSelectedObject = hit.collider.gameObject;
                    currentPlacementPreview = currentSelectedObject.GetComponent<PlacementPreview>();

                    if (currentPlacementPreview != null)
                    {
                        currentPlacementPreview.SetSelected(true);
                    }

                    if (objectManipulator != null)
                    {
                        objectManipulator.StartMoving(currentSelectedObject.transform);
                    }
                }
            }
            else
            {
                if (currentSelectedObject != null)
                {
                    if (currentPlacementPreview != null)
                    {
                        currentPlacementPreview.SetSelected(false);
                    }
                    if (objectManipulator != null)
                    {
                        objectManipulator.DeselectObject();
                    }
                    currentSelectedObject = null;
                }
            }
        }
    }
}
