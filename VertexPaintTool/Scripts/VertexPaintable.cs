using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//--------------------------------------------------------------------------------------------------------------------------------------------------------------------
//Component for meshes that want to use vertex color painting.
//--------------------------------------------------------------------------------------------------------------------------------------------------------------------
//Restrictions on VertexPaintable use:
//The mesh must abide by a 1024 vertex limit.
//The mesh must not be marked as Static.
//The mesh will not be paintable if it lacks a collider, although its colors can still be applied.
//--------------------------------------------------------------------------------------------------------------------------------------------------------------------
public class VertexPaintable : MonoBehaviour, ISerializationCallbackReceiver
{
    //Internal variables.
    private Dictionary<Vector3Int, Color> _colorMap = new Dictionary<Vector3Int, Color>();
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private bool showVertexDebugging;

    //Serialized values.
    [HideInInspector]
    [SerializeField]
    private List<VertexColorMapping> _serializedMap = new List<VertexColorMapping>();
    [HideInInspector]
    [SerializeField]
    private Color defaultColor = new Color(0, 0, 0, 0);
    private Color emptyColor = new Color(0, 0, 0, 0);

    //Internal tunables.
    private const int decimalPlacePrecision = 3;

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Methods.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Main methods for accessing/editing the color map.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public void SetRawVertexColor(Vector3 position, Color color)
    {
        if (meshFilter == null || meshRenderer == null) AssignReferences();
        if (_colorMap == null) _colorMap = new Dictionary<Vector3Int, Color>();

        var key = GetCanonicalPosition(position);
        _colorMap[key] = color;
        ApplyVertexColors();
        EditorUtility.SetDirty(this);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Raw vertex color is the color stored directly in the map, before accounting for the ambient color.
    public Color GetRawVertexColor(Vector3Int canonicalPos)
    {
        if (meshFilter == null || meshRenderer == null) AssignReferences();
        if (_colorMap == null) _colorMap = new Dictionary<Vector3Int, Color>();

        if (_colorMap.ContainsKey(canonicalPos)) return _colorMap[canonicalPos];
        return emptyColor;
    }

    public Color GetRawVertexColor(Vector3 position)
    {
        var canonicalPos = GetCanonicalPosition(position);
        return GetRawVertexColor(canonicalPos);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Commits the color map into the mesh via the MaterialPropertyBlock.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public void ApplyVertexColors()
    {
        if (meshRenderer == null || meshFilter == null) AssignReferences();
        if (_colorMap == null) _colorMap = new Dictionary<Vector3Int, Color>();

        var verts = meshFilter.sharedMesh.vertices;
        //var vectors = new Vector4[verts.Length];
        var vectors = new Vector4[1023];

        for (int i = 0; i < verts.Length; i++)
        {
            var canonicalPos = GetCanonicalPosition(verts[i]);
            var storedColor = GetRawVertexColor(canonicalPos);
            vectors[i] = Color.Lerp(defaultColor, storedColor, storedColor.a);
        }

        Ark.Material.SetPropertyBlockVectorArray(meshRenderer, Ark.Material.vertexColorArrayPropName, vectors);
    }

    public void ClearVertexColors()
    {
        _colorMap.Clear();
        _serializedMap.Clear();
        Ark.Material.ClearPropertyBlock(meshRenderer);
        ApplyVertexColors();
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Refresh is meant to get paintables back to a working state after a reimport.
    //Reimport will break mesh collider references, which prevents vertex painting.
    //It also breaks the indexing assumption of vertices, which will temporarily cause incorrect rendering.
    //Simply reapplying the colors will re-index the array correctly.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    [ExecuteInEditMode]
    public void Refresh()
    {
        var collider = gameObject.GetComponent<MeshCollider>();
        if (collider != null && meshFilter != null)
        {
            DestroyImmediate(collider);
            gameObject.AddComponent<MeshCollider>();
        }
        ApplyVertexColors();
    }

    public void SetDefaultColor(Color color) { defaultColor = color; }
    private void AssignReferences()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //MonoBehaviour lifetime management hooks.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    void Awake() { AssignReferences(); }
    private void OnValidate() { ApplyVertexColors(); }
    private void OnEnable()
    {
        AssignReferences();
        ApplyVertexColors();
        enabled = false;
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //ISerializationCallbackReceiver implementation.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public void OnBeforeSerialize()
    {
        _serializedMap = new List<VertexColorMapping>();
        foreach (KeyValuePair<Vector3Int, Color> kvp in _colorMap)
        {
            _serializedMap.Add(new VertexColorMapping(kvp.Key, kvp.Value));
        }
    }

    public void OnAfterDeserialize()
    {
        _colorMap = new Dictionary<Vector3Int, Color>();
        foreach (VertexColorMapping vcm in _serializedMap)
        {
            _colorMap[vcm.canonicalVertPos] = vcm.color;
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Canonical position functions.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public static Vector3Int GetCanonicalPosition(Vector3 position)
    {
        float multiplier = Mathf.Pow(10, decimalPlacePrecision);
        int x = (int)System.Math.Truncate(position.x * multiplier);
        int y = (int)System.Math.Truncate(position.y * multiplier);
        int z = (int)System.Math.Truncate(position.z * multiplier);

        return new Vector3Int(x, y, z);
    }

    public static Vector3 ConvertCanonicalPosition(Vector3Int canonicalPos)
    {
        float divisor = Mathf.Pow(10, decimalPlacePrecision);
        Vector3 vector = new Vector3(canonicalPos.x, canonicalPos.y, canonicalPos.z);
        return (vector / divisor);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //UI for debug.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        if (!showVertexDebugging) return;

        var mesh = meshFilter.sharedMesh;
        var indices = mesh.GetIndices(0);
        var vertexPositions = new Dictionary<Vector3Int, HashSet<int>>();

        for (int i = 0; i < indices.Length; i++)
        {
            var pos = mesh.vertices[indices[i]];
            var key = GetCanonicalPosition(pos);
            if (!vertexPositions.ContainsKey(key)) vertexPositions.Add(key, new HashSet<int> { indices[i] });
            else vertexPositions[key].Add(indices[i]);
        }

        foreach (Vector3Int key in vertexPositions.Keys)
        {
            var style = new GUIStyle();
            style.fontSize = 20;
            style.alignment = TextAnchor.MiddleCenter;
            /*var label = "Indices ";
            var vertIndices = vertexPositions[key];
            foreach (int index in vertIndices) label += (index + ",");*/
            var displayPos = ConvertCanonicalPosition(key);
            var label = "" + transform.TransformPoint(displayPos);
            Handles.Label(transform.TransformPoint(displayPos), label, style);
        }
    }

    public void SetDebugColors()
    {
        var mesh = meshFilter.sharedMesh;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            SetRawVertexColor(mesh.vertices[i], RandomDebugColor());
        }
        ApplyVertexColors();
    }

    private Color RandomDebugColor()
    {
        float rng = Random.Range(0, 4);
        if (rng < 1) return Color.red;
        else if (rng < 2) return Color.blue;
        else if (rng < 3) return Color.green;
        else return Color.white;
    }

    public void ToggleDebugVertexOutput() { showVertexDebugging = !showVertexDebugging; }
}

/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class VertexPaintable : MonoBehaviour
{
    [SerializeField]
    private List<VertexColorMapping> _vertexColors = new List<VertexColorMapping>();

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private bool showVertexDebugging;

    private Color defaultColor = new Color(0, 0, 0, 0);

    private const string arrayPropName = "_VertexColorArray";

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Methods.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void AssignReferences()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
    }
    
    public void SetVertexColor(Vector3 position, Color color)
    {
        if (meshFilter == null || meshRenderer == null) AssignReferences();

        var indices = GetVertIndicesForPosition(position);
        var posKey = new Vector3Key(position);
        bool foundMatch = false;
        for (int i = 0; i < _vertexColors.Count; i++)
        {
            if (_vertexColors[i].vertPos.Equals(posKey))
            {
                foundMatch = true;
                var newData = _vertexColors[i];
                newData.color = color;
                newData.vertIndices = indices;
                _vertexColors[i] = newData;
            }
        }
        if (!foundMatch)
        {
            _vertexColors.Add(new VertexColorMapping(posKey, color, indices));
        }
    }

    private List<int> GetVertIndicesForPosition(Vector3 position)
    {
        var verts = meshFilter.sharedMesh.vertices;
        var indices = new List<int>();
        for (int i = 0; i < verts.Length; i++)
        {
            var lhs = new Vector3Key(verts[i]);
            var rhs = new Vector3Key(position);
            if (lhs.Equals(rhs)) indices.Add(i);
        }
        return indices;
    }

    public void ApplyVertexColors()
    {
        if (meshRenderer == null || meshFilter == null) AssignReferences();

        var verts = meshFilter.sharedMesh.vertices;
        var vectors = new Vector4[verts.Length];
        foreach (var mapping in _vertexColors)
        {
            foreach (int vertIndex in mapping.vertIndices)
            {
                vectors[vertIndex] = mapping.color;
            }
        }
        var propBlock = Ark.Material.CreatePropertyBlock(arrayPropName, vectors);
        Ark.Material.SetPropertyBlock(meshRenderer, propBlock);
    }

    public void ClearVertexColors()
    {
        _vertexColors = new List<VertexColorMapping>();
        Ark.Material.ClearPropertyBlock(meshRenderer);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //MonoBehaviour lifetime management hooks.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    void Awake() { AssignReferences(); }
    private void OnValidate() { ApplyVertexColors(); }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //UI for debug.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        if (!showVertexDebugging) return;

        var mesh = meshFilter.sharedMesh;
        var indices = mesh.GetIndices(0);
        var vertexPositions = new Dictionary<Vector3Key, HashSet<int>>();

        for (int i = 0; i < indices.Length; i++)
        {
            var pos = mesh.vertices[indices[i]];
            var key = new Vector3Key(pos);
            if (!vertexPositions.ContainsKey(key)) vertexPositions.Add(key, new HashSet<int> { indices[i] });
            else vertexPositions[key].Add(indices[i]);
        }

        foreach (Vector3Key key in vertexPositions.Keys)
        {
            var style = new GUIStyle();
            style.fontSize = 20;
            style.alignment = TextAnchor.MiddleCenter;
            var label = "Indices ";
            var vertIndices = vertexPositions[key];
            foreach (int index in vertIndices) label += (index + ",");
            Handles.Label(transform.TransformPoint(key.vector), label, style);
        }
    }

    public void SetDebugColors()
    {
        var mesh = meshFilter.sharedMesh;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            SetVertexColor(mesh.vertices[i], RandomDebugColor());
        }
        ApplyVertexColors();
    }

    private Color RandomDebugColor()
    {
        float rng = Random.Range(0, 4);
        if (rng < 1) return Color.red;
        else if (rng < 2) return Color.blue;
        else if (rng < 3) return Color.green;
        else return Color.white;
    }

    public void ToggleDebugVertexOutput() { showVertexDebugging = !showVertexDebugging; }
}
*/