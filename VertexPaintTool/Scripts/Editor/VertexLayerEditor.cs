using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VertexPaintLayer))]
public class VertexPaintLayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Set default color to ambient"))
        {
            var layer = target as VertexPaintLayer;
            layer.SetAmbientColorForAll();
        }
        if (GUILayout.Button("Clear default color"))
        {
            var layer = target as VertexPaintLayer;
            layer.ClearDefaultColorForAll();
        }

        if (GUILayout.Button("Refresh all"))
        {
            var layer = target as VertexPaintLayer;
            layer.RefreshAll();
        }
        if (GUILayout.Button("Reapply vertex colors for all"))
        {
            var layer = target as VertexPaintLayer;
            layer.ReapplyVertexColorsForAll();
        }
        if (GUILayout.Button("Clear vertex colors for all"))
        {
            var layer = target as VertexPaintLayer;
            bool confirm = EditorUtility.DisplayDialog("Clear vertex colors?", "This will clear vertex colors for all paintable meshes in this layer. This cannot be undone. Are you sure?", "Yes", "No");
            if (confirm) layer.ClearVertexColorsForAll();
        }
    }
}
