using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class VertexPaintableAutoAttach
{
    static VertexPaintableAutoAttach()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged()
    {
        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (go.GetComponent<MeshRenderer>() != null && go.GetComponent<VertexPaintable>() == null)
            {
                go.AddComponent<VertexPaintable>();
            }
        }
    }
}
