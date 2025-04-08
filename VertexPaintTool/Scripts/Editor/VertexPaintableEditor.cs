using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VertexPaintable))]
public class VertexPaintableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var tar = target as VertexPaintable;
        if (GUILayout.Button("Refresh"))
        {
            tar.Refresh();
        }
        if (GUILayout.Button("Clear vertex colors"))
        {
            bool confirm = EditorUtility.DisplayDialog("Clear vertex colors?", "This will clear vertex colors for this object. This cannot be undone. Are you sure?", "Yes", "No");
            if (confirm) tar.ClearVertexColors();
        }
        if (GUILayout.Button("Reapply vertex colors"))
        {
            tar.ApplyVertexColors();
        }
        if (GUILayout.Button("Toggle vertex output"))
        {
            tar.ToggleDebugVertexOutput();
        }
    }
}
