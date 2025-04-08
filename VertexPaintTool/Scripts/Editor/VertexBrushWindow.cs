using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using Ark;

//--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
//Editor window for the vertex color brush.
//--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
public class VertexBrushWindow : EditorWindow
{
    //Brush properties.
    public VertexPaintLayer activeLayer;
    public float radius = 50f;
    public Color brushColor = Color.black;
    public float brushOpacity = 1f;
    public float brushHardness = 0f;
    public bool showTextures = true;
    public bool showLighting = true;
    public bool showVertexColors = true;
    public bool paintSelectedMeshOnly = false;
    private bool brushIsEnabled = false;

    //Internal variables.
    private bool isPainting;
    private bool paintOccluded;
    private int undoGroup = -1;
    private Vector2 lastPaintPosition;
    private Color labelColor;
    private Sys.Stack<Color> recentColors;
    private List<LayerScopeItem> layerScope; //Cached list of vertices in the layer that can be painted over, mapped to the VertexPaintable that owns them.

    private SerializedObject serializedObject;
    private SerializedProperty colorListProp;
    private GUIStyle defaultLabelStyle;

    //Internal tunables.
    private float cursorMovePaintThreshold = 20; //Min distance, in pixels, that the cursor must move while click-dragging before we call the paint operation again.
    private float sizeAdjIncrement = 10;
    private float width = 1440;
    private float minBrushSize = 1f;
    private float maxBrushSize = 500f;
    private float minBrushStrength = 0.01f;
    private float maxBrushStrength = 1f;
    private float minBrushFalloff = 0f;
    private float maxBrushFalloff = 1f;

    //Internal constants.
    private const int colorStackCapacity = 8;
    private const float labelWidth = 96;
    private const float maxPropWidth = 512;
    private const float minWidth = 512;


    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Methods.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    [MenuItem("Tools/Vertex Brush")]
    public static void ShowWindow()
    {
        var window = GetWindow<VertexBrushWindow>("Vertex Brush");
        window.position = new Rect(100, 100, window.width, window.GetHeight());
        window.minSize = new Vector2(minWidth, window.GetHeight());
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += UpdateLabel;
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.update -= UpdateLabel;
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Main GUI methods.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnSceneGUI(SceneView sceneView)
    {
        defaultLabelStyle = new GUIStyle(EditorStyles.label);
        brushIsEnabled = Event.current.shift;
        paintOccluded = Event.current.control;
        if (brushIsEnabled && Event.current.type == EventType.Repaint)
        {
            Handles.BeginGUI();
            var uiColor = new Color(brushColor.r, brushColor.g, brushColor.b, 1);
            DrawCircleOutline(Event.current.mousePosition, radius, uiColor);
            Handles.EndGUI();
        }

        var curEvent = Event.current;
        var mousePos = curEvent.mousePosition;
        if (brushIsEnabled && curEvent.type == EventType.MouseDown && curEvent.button == 0)
        {
            StartStroke(mousePos);
            Paint(mousePos, erase: curEvent.alt);
            curEvent.Use();
        }
        else if (curEvent.type == EventType.MouseUp && curEvent.button == 0)
        {
            EndStroke();
        }
        else if (isPainting && curEvent.type == EventType.MouseDrag && curEvent.button == 0)
        {
            if (Vector2.Distance(mousePos, lastPaintPosition) > cursorMovePaintThreshold)
            {
                lastPaintPosition = mousePos;
                curEvent.Use();
                Paint(mousePos, erase: curEvent.alt);
            }
        }
        sceneView.Repaint();
    }

    void OnGUI()
    {
        minSize = new Vector2(minWidth, GetHeight());

        EditorGUILayout.Space(5);
        EditorGUIUtility.labelWidth = labelWidth;
        EditorGUI.BeginChangeCheck();
        activeLayer = (VertexPaintLayer)EditorGUILayout.ObjectField("Layer", activeLayer, typeof(VertexPaintLayer), true);
        if (EditorGUI.EndChangeCheck()) RefreshLayerScope();
        if (activeLayer != null)
        {
            if (GUILayout.Button("Refresh layer scope")) RefreshLayerScope();
            EditorGUILayout.Space(5);
        }
        DrawLine();
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        brushColor = EditorGUILayout.ColorField(new GUIContent("Brush Color"), brushColor, showEyedropper: true, showAlpha: false, hdr: false, GUILayout.MaxWidth(maxPropWidth));
        radius = EditorGUILayout.Slider(new GUIContent("Brush radius"), radius, minBrushSize, maxBrushSize);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        brushOpacity = EditorGUILayout.Slider(new GUIContent("Brush strength"), brushOpacity, minBrushStrength, maxBrushStrength);
        brushHardness = EditorGUILayout.Slider(new GUIContent("Brush hardness"), brushHardness, minBrushFalloff, maxBrushFalloff);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        bool newShowTexturesState = EditorGUILayout.Toggle(new GUIContent("Textures"), showTextures);
        if (newShowTexturesState != showTextures)
        {
            showTextures = newShowTexturesState;
            ToggleBoolPropertyForAll(Ark.Material.showTexturePropName, showTextures);
        }
        
        bool newShowVertColorsState = EditorGUILayout.Toggle(new GUIContent("Vertex colors"), showVertexColors);
        if (newShowVertColorsState != showVertexColors)
        {
            showVertexColors = newShowVertColorsState;
            ToggleBoolPropertyForAll(Ark.Material.showVertexColorPropName, showVertexColors);
        }
        bool newShowLightingState = EditorGUILayout.Toggle(new GUIContent("Lighting"), showLighting);
        if (newShowLightingState != showLighting)
        {
            showLighting = newShowLightingState;
            ToggleBoolPropertyForAll(Ark.Material.lightingScalePropName, showLighting);
        }
        EditorGUILayout.EndHorizontal();
        paintSelectedMeshOnly = EditorGUILayout.Toggle(new GUIContent("Selected only"), paintSelectedMeshOnly);

        EditorGUILayout.Space(5);
        DrawLine();
        EditorGUILayout.Space(5);

        var labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = labelColor;
        EditorGUILayout.LabelField("Hold shift and click to begin painting.", labelStyle);
        EditorGUILayout.LabelField("Hold shift + alt and click to erase.", labelStyle);
        EditorGUILayout.LabelField("Hold ctrl while painting to ignore occluded verts.", labelStyle);
        if (activeLayer != null)
        {
            EditorGUILayout.Space(5);
            DrawLine();
            EditorGUILayout.Space(5);
            DrawPaletteGUI();
            DrawLayerGUI();
        }
        BrushControlHotkeys(Event.current);
    }

    private void DrawLine()
    {
        var color = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.3f) : Color.gray;
        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(10));
        rect.height = 1;
        rect.y += 5;
        EditorGUI.DrawRect(rect, color);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Brush state tracking for paint and undo operations.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void StartStroke(Vector2 mousePosition)
    {
        Undo.IncrementCurrentGroup();
        undoGroup = Undo.GetCurrentGroup();

        isPainting = true;
        lastPaintPosition = mousePosition;

    }
    private void EndStroke()
    {
        isPainting = false;
        UpdateRecentColors();
        Undo.CollapseUndoOperations(undoGroup);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Main paint function. Called whenever you click, and also periodically called when you click and drag.
    //Paint may affect multiple vertices and meshes, and is grouped together as a single Undo operation.
    //Resultant colors are only calculated once for each unique vertex position.
    //For subsequent operations at already-calculated positions, we simply reuse the color from the first result.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void Paint(Vector2 mousePosition, bool erase)
    {
        var paintOps = GetAffectedVertices(mousePosition);
        var affected = new HashSet<VertexPaintable>();
        var results = new Dictionary<VertexPaintKey, Color>();

        foreach (VertexPaintOperation op in paintOps)
        {
            if (!affected.Contains(op.paintable)) affected.Add(op.paintable);
        }

        foreach (var paintable in affected) Undo.RecordObject(paintable, "Vertex brush stroke.");
        foreach (VertexPaintOperation op in paintOps)
        {
            var canonicalPos = VertexPaintable.GetCanonicalPosition(op.vertPosition);
            var key = new VertexPaintKey(op.paintable.transform, canonicalPos);
            if (results.TryGetValue(key, out Color existingColor))
            {
                op.paintable.SetRawVertexColor(op.vertPosition, existingColor);
            }
            else if (erase) results.Add(key, Erase(op));
            else
            {
                results.Add(key, PaintOver(op));
            }
        }
        foreach (var paintable in affected) EditorUtility.SetDirty(paintable);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Method called by a brush stroke that determines which vertices are inside the stroke and how to color them.
    //The method uses Physics raycasts, so it requires colliders to work properly.
    //It returns a list of operations with instructions for how to color the vertices.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public List<VertexPaintOperation> GetAffectedVertices(Vector2 mousePos)
    {
        var sceneView = SceneView.lastActiveSceneView;
        var sceneCamera = sceneView.camera;
        mousePos.x *= (sceneCamera.pixelWidth / sceneView.position.width);
        mousePos.y = sceneView.position.height - mousePos.y; //Invert.
        mousePos.y *= (sceneCamera.pixelHeight / sceneView.position.height);

        var paintOperations = new List<VertexPaintOperation>();
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return paintOperations;

        Collider collider = hit.collider;
        if (collider == null) return paintOperations;
        MeshFilter mf = collider.gameObject.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return paintOperations;

        var vertexScope = GetLayerScope();
        paintOccluded = !Event.current.control;

        foreach (LayerScopeItem scopeItem in vertexScope)
        {
            if (scopeItem.paintable == null) continue;
            foreach (Vector3 vertex in scopeItem.vertices)
            {
                Vector3 vertexWorldPos = scopeItem.trans.TransformPoint(vertex);
                Vector3 vertexScreenPos = sceneCamera.WorldToScreenPoint(vertexWorldPos);

                if (vertexScreenPos.z <= 0) continue; // Ignore vertices behind the camera

                float distance = Vector2.Distance(new Vector2(vertexScreenPos.x, vertexScreenPos.y), mousePos);
                if (distance <= radius && (paintOccluded || !VertexIsOccluded(vertexWorldPos)))
                {
                    if (paintSelectedMeshOnly && !IsActiveSelection(scopeItem.trans)) continue;
                    float weight = GetPaintWeight(distance);
                    var paintOp = new VertexPaintOperation(scopeItem.paintable, vertex, weight);
                    paintOperations.Add(paintOp);
                }
            }        
        }
        return paintOperations;
    }

    //Helper for GetAffectedVertices.
    private static bool VertexIsOccluded(Vector3 position)
    {
        var sceneCamera = SceneView.lastActiveSceneView.camera;
        Vector3 cameraPosition = sceneCamera.transform.position;
        Vector3 directionToPosition = (position - cameraPosition).normalized;
        Ray ray = new Ray(cameraPosition, directionToPosition);
        float distanceToTarget = Vector3.Distance(cameraPosition, position);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, distanceToTarget))
        {
            float distanceToHit = Vector3.Distance(cameraPosition, hitInfo.point);
            return distanceToHit < distanceToTarget - 0.01f;
        }
        return false;
    }

    //---------------------------------------------------------------------------------------------------------------------------------
    //Applies Photoshop brush paint logic to the vertex, and returns the resulting color it applied.
    //The brush color's alpha is completely disregarded. Only brush opacity is relevant.
    //However, the resultant opacity is still saved as color alpha.
    //---------------------------------------------------------------------------------------------------------------------------------
    private Color PaintOver(VertexPaintOperation op)
    {
        var oldColor = op.paintable.GetRawVertexColor(op.vertPosition);
        float oldAlpha = oldColor.a;
        float weight = brushOpacity * op.weight;
        var newColor = brushColor * weight + oldColor * oldAlpha * (1 - weight);
        newColor.a = weight + oldAlpha * (1 - weight);
        op.paintable.SetRawVertexColor(op.vertPosition, newColor);
        return newColor;
    }

    //---------------------------------------------------------------------------------------------------------------------------------
    //Erases from the vertex color alpha, then returns the resulting color it applied.
    //Brush opacity determines the strength of the effect.
    //---------------------------------------------------------------------------------------------------------------------------------
    private Color Erase(VertexPaintOperation op)
    {

        var newColor = op.paintable.GetRawVertexColor(op.vertPosition);
        float weight = brushOpacity * op.weight;
        newColor.a = Mathf.Clamp(newColor.a - weight, 0, 1);
        op.paintable.SetRawVertexColor(op.vertPosition, newColor);
        return newColor;
    }

    //Helper for paintover/erase.
    private float GetPaintWeight(float distFromBrushCenter)
    {
        float normalizedDistance = distFromBrushCenter / radius;

        float weight;
        if (brushHardness >= 1.0f) weight = 1.0f;
        else if (brushHardness <= 0.0f) weight = 1.0f - normalizedDistance;
        else
        {
            float falloffExponent = Mathf.Lerp(1.0f, 3.0f, brushHardness);
            weight = Mathf.Pow(1.0f - normalizedDistance, falloffExponent);
        }
        return weight;
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Brush controls.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void BrushControlHotkeys(Event currentEvent)
    {
        if (currentEvent == null) return;
        if (currentEvent.type == EventType.KeyDown)
        {
            if (currentEvent.keyCode == KeyCode.Equals)
            {
                AlterBrushSize(sizeAdjIncrement);
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.keyCode == KeyCode.Minus)
            {
                AlterBrushSize(-sizeAdjIncrement);
                currentEvent.Use();
                Repaint();
            }
        }
    }

    private void AlterBrushSize(float amount) { radius = Mathf.Clamp(radius + amount, minBrushSize, maxBrushSize); }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Palette color GUI.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void DrawPaletteGUI()
    {
        if (activeLayer == null) return;
        serializedObject = new SerializedObject(activeLayer);
        colorListProp = serializedObject.FindProperty("_palette");
        serializedObject.Update();

        EditorGUILayout.Space(10);
        var palette = activeLayer.GetAllColorsInPalette();
        if (palette.Count == 0 && recentColors != null && recentColors.Length > 0)
        {
            EditorGUILayout.LabelField("Press + on recent color to add to palette.");
            return;
        }

        var reorderableList = new ReorderableList(serializedObject, colorListProp, true, true, true, true);
        reorderableList.drawElementCallback = DrawPaletteEntry;
        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Layer palette");
        };
        reorderableList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPaletteEntry(Rect rect, int index, bool isActive, bool isFocused)
    {
        var colorProperty = colorListProp.GetArrayElementAtIndex(index);
        var width = rect.width - 60;
        var height = EditorGUIUtility.singleLineHeight;
        EditorGUI.ColorField(new Rect(rect.x, rect.y + 2, width, height), GUIContent.none, colorProperty.colorValue, false, false, false);
        if (GUI.Button(new Rect(rect.x + width + 5, rect.y, 50, rect.height), "Use"))
        {
            brushColor = colorProperty.colorValue;
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Recent color GUI.
    //Recent color is just a recording of the colors you have painted with, with the option to save them into your layer palette.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void DrawLayerGUI()
    {
        if (recentColors == null) return;
        if (recentColors.Length == 0) return;
        EditorGUILayout.LabelField("Recent colors");
        for (int i = 0; i < recentColors.Length; i++) DrawRecentColorGUI(i);
    }

    private void DrawRecentColorGUI(int index)
    {
        PrepRecentColors();
        float iconSize = EditorGUIUtility.singleLineHeight;
        float iconWidth = 50;
        Color color = recentColors.GetAt(index);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ColorField(new GUIContent(""), color, showEyedropper: false, showAlpha: false, hdr: false);
        if (GUILayout.Button("Store", GUILayout.Width(iconWidth), GUILayout.Height(iconSize)))
        {
            activeLayer.AddToPalette(recentColors.GetAt(index));
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void UpdateRecentColors()
    {
        PrepRecentColors();
        var top = recentColors.Peek();
        if (brushColor == top) return;
        recentColors.Push(brushColor);
    }

    private void PrepRecentColors()
    {
        if (recentColors == null) recentColors = new Ark.Sys.Stack<Color>(colorStackCapacity);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Layer scope handling.
    //The layer scope is a cached collection of all the mesh vertices in the layer, and the transform & paintable that owns them.
    //When you do free-paint, it uses this collection of vertices to determine what it can paint.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void RefreshLayerScope()
    {
        if (activeLayer == null) return;
        activeLayer.SetLayerScope(); //Lazy initialization ensures it's up to date every time.
        layerScope = activeLayer.GetLayerScope();
    }

    private List<LayerScopeItem> GetLayerScope()
    {
        if (activeLayer == null) return new List<LayerScopeItem>();
        if (layerScope == null) RefreshLayerScope();
        return layerScope;
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Display options.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void ToggleBoolPropertyForAll(string propertyName, bool enable)
    {
        var allRenderers = Ark.Obj.GetAllComponentsOfType<MeshRenderer>();
        foreach (MeshRenderer mr in allRenderers)
        {
            for (int i = 0; i < mr.sharedMaterials.Length; i++)
            {
                var block = Ark.Material.GetPropertyBlock(mr);
                block.SetFloat(propertyName, enable ? 1 : 0);
                mr.SetPropertyBlock(block);
            }
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Misc. GUI helpers.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private float GetHeight()
    {
        int numLines = 10;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float height = numLines * lineHeight;
        if (activeLayer != null) height += (activeLayer.GetAllColorsInPalette().Count * lineHeight);
        if (recentColors != null) height += (recentColors.Length * lineHeight);
        return height;
    }

    private void UpdateLabel()
    {
        if (defaultLabelStyle == null) return;
        labelColor = isPainting ? Color.grey : defaultLabelStyle.normal.textColor;
        Repaint();
    }

    private void DrawCircleOutline(Vector2 center, float radius, Color color)
    {
        if (color.r <= 0.01f && color.g <= 0.01f && color.b <= 0.01f) color = Color.grey; //lighten if you're working with black.
        Handles.color = color;
        Handles.DrawWireDisc(center, Vector3.forward, radius, thickness: 2f);
    }

    private bool IsActiveSelection(Transform trans)
    {
        return Selection.activeGameObject != null && Selection.activeGameObject.transform == trans;
    }
}

/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using Ark;

//--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
//Editor window for the vertex color brush.
//--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
public class VertexBrushWindow : EditorWindow
{
    //Brush properties.
    public VertexPaintLayer activeLayer;
    public float radius = 50f;
    public Color brushColor = Color.black;
    public float brushOpacity = 1f;
    public float brushHardness = 0f;
    public bool showTextures = true;
    public bool showLighting = true;
    public bool showVertexColors = true;
    public bool paintSelectedMeshOnly = false;
    private bool brushIsEnabled = false;

    //Internal variables.
    private bool isPainting;
    private bool paintOccluded;
    private int undoGroup = -1;
    private Vector2 lastPaintPosition;
    private Color labelColor;
    private Sys.Stack<Color> recentColors;
    private List<Tuple<Transform,Vector3>> layerScope; //Cached list of vertices in the layer that can be painted over, mapped to the Transform that owns them.

    private SerializedObject serializedObject;
    private SerializedProperty colorListProp;

    //Internal tunables.
    private float cursorMovePaintThreshold = 20; //Min distance, in pixels, that the cursor must move while click-dragging before we call the paint operation again.
    private float sizeAdjIncrement = 10;
    private float width = 1440;
    private float minBrushSize = 1f;
    private float maxBrushSize = 500f;
    private float minBrushStrength = 0.01f;
    private float maxBrushStrength = 1f;
    private float minBrushFalloff = 0f;
    private float maxBrushFalloff = 1f;

    //Internal constants.
    private const int colorStackCapacity = 8;
    private const float labelWidth = 96;
    private const float maxPropWidth = 512;
    private const float minWidth = 768;

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Methods.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    [MenuItem("Tools/Vertex Brush")]
    public static void ShowWindow()
    {
        var window = GetWindow<VertexBrushWindow>("Vertex Brush");
        window.position = new Rect(100, 100, window.width, window.GetHeight());
    }

    private void OnEnable() 
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += UpdateLabel;
    }
    private void OnDisable() 
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.update -= UpdateLabel;
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Main GUI methods.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnSceneGUI(SceneView sceneView)
    {
        brushIsEnabled = Event.current.shift;
        paintOccluded = Event.current.control;
        if (brushIsEnabled && Event.current.type == EventType.Repaint)
        {
            Handles.BeginGUI();
            var uiColor = new Color(brushColor.r, brushColor.g, brushColor.b, 1);
            DrawCircleOutline(Event.current.mousePosition, radius, uiColor);
            Handles.EndGUI();
        }

        var curEvent = Event.current;
        var mousePos = curEvent.mousePosition;
        if (brushIsEnabled && curEvent.type == EventType.MouseDown && curEvent.button == 0)
        {
            StartStroke(mousePos);
            Paint(mousePos, erase: curEvent.alt);
            curEvent.Use();
        }
        else if (curEvent.type == EventType.MouseUp && curEvent.button == 0)
        {
            EndStroke();
        }
        else if (isPainting && curEvent.type == EventType.MouseDrag && curEvent.button == 0)
        {
            if (Vector2.Distance(mousePos, lastPaintPosition) > cursorMovePaintThreshold)
            {
                lastPaintPosition = mousePos;
                curEvent.Use();
                Paint(mousePos, erase: curEvent.alt);
            }
        }
        sceneView.Repaint();
    }

    void OnGUI()
    {
        minSize = new Vector2(minWidth, GetHeight());

        EditorGUIUtility.labelWidth = labelWidth;
        EditorGUI.BeginChangeCheck();
        activeLayer = (VertexPaintLayer)EditorGUILayout.ObjectField("Layer", activeLayer, typeof(VertexPaintLayer), true);
        if (EditorGUI.EndChangeCheck()) SaveLayerScope();
        
        EditorGUILayout.BeginHorizontal();
        brushColor = EditorGUILayout.ColorField(new GUIContent("Brush Color"), brushColor, showEyedropper: true, showAlpha: false, hdr: false, GUILayout.MaxWidth(maxPropWidth));
        radius = EditorGUILayout.Slider(new GUIContent("Brush radius"), radius, minBrushSize, maxBrushSize);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        brushOpacity = EditorGUILayout.Slider(new GUIContent("Brush strength"), brushOpacity, minBrushStrength, maxBrushStrength);
        brushHardness = EditorGUILayout.Slider(new GUIContent("Brush hardness"), brushHardness, minBrushFalloff, maxBrushFalloff);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        bool newShowTexturesState = EditorGUILayout.Toggle(new GUIContent("Textures"), showTextures);
        if (newShowTexturesState != showTextures) ToggleShowTexturesForAll(newShowTexturesState);
        showVertexColors = EditorGUILayout.Toggle(new GUIContent("Vertex colors"), showVertexColors); //stub
        showLighting = EditorGUILayout.Toggle(new GUIContent("Lighting"), showLighting); //stub
        EditorGUILayout.EndHorizontal();
        paintSelectedMeshOnly = EditorGUILayout.Toggle(new GUIContent("Paint selected mesh only"), paintSelectedMeshOnly);

        EditorGUILayout.Space(10);
        var labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = labelColor;
        EditorGUILayout.LabelField("Hold shift and click to begin painting.", labelStyle);
        EditorGUILayout.LabelField("Hold shift + alt and click to erase.", labelStyle);
        EditorGUILayout.LabelField("Hold ctrl while painting to affect occluded verts.", labelStyle);
        if (activeLayer != null)
        {
            DrawPaletteGUI();
            DrawLayerGUI();
        }
        BrushControlHotkeys(Event.current);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Brush state tracking for paint and undo operations.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void StartStroke(Vector2 mousePosition) 
    {
        Undo.IncrementCurrentGroup();
        undoGroup = Undo.GetCurrentGroup();

        isPainting = true;
        lastPaintPosition = mousePosition;

    }
    private void EndStroke() 
    {
        isPainting = false;
        UpdateRecentColors();
        Undo.CollapseUndoOperations(undoGroup);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Main paint function. Called whenever you click, and also periodically called when you click and drag.
    //Paint may affect multiple vertices and meshes, and is grouped together as a single Undo operation.
    //Resultant colors are only calculated once for each unique vertex position.
    //For subsequent operations at already-calculated positions, we simply reuse the color from the first result.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void Paint(Vector2 mousePosition, bool erase)
    {
        var paintOps = GetAffectedVertices(mousePosition);
        var affected = new HashSet<VertexPaintable>();
        var results = new Dictionary<VertexPaintKey, Color>();

        foreach (VertexPaintOperation op in paintOps)
        {
            if (!affected.Contains(op.paintable)) affected.Add(op.paintable);
        }

        foreach (var paintable in affected) Undo.RecordObject(paintable, "Vertex brush stroke.");
        foreach (VertexPaintOperation op in paintOps)
        {
            var canonicalPos = VertexPaintable.GetCanonicalPosition(op.vertPosition);
            var key = new VertexPaintKey(op.paintable.transform, canonicalPos);
            if (results.TryGetValue(key, out Color existingColor))
            {
                op.paintable.SetVertexColor(op.vertPosition, existingColor);
            }
            else if (erase) results.Add(key, Erase(op));
            else
            {
                results.Add(key, PaintOver(op));
            }
        }
        foreach (var paintable in affected) EditorUtility.SetDirty(paintable);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Method called by a brush stroke that determines which vertices are inside the stroke and how to color them.
    //The method uses Physics raycasts, so it requires colliders to work properly.
    //It returns a list of operations with instructions for how to color the vertices.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public List<VertexPaintOperation> GetAffectedVertices(Vector2 mousePos)
    {
        var sceneView = SceneView.lastActiveSceneView;
        var sceneCamera = sceneView.camera;
        mousePos.x *= (sceneCamera.pixelWidth / sceneView.position.width);
        mousePos.y = sceneView.position.height - mousePos.y; //Invert.
        mousePos.y *= (sceneCamera.pixelHeight / sceneView.position.height);

        var paintOperations = new List<VertexPaintOperation>();
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return paintOperations;

        Collider collider = hit.collider;
        if (collider == null) return paintOperations;
        MeshFilter mf = collider.gameObject.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return paintOperations;
        VertexPaintable paintable = collider.gameObject.GetComponent<VertexPaintable>();
        if (paintable == null) return paintOperations;

        List<Tuple<Transform, Vector3>> vertexScope = GetLayerVertexScope();
        paintOccluded = Event.current.control;

        foreach (Tuple<Transform, Vector3> vertMapping in vertexScope)
        {
            Transform trans = vertMapping.Item1;
            if (trans == null) continue;

            Vector3 vertex = vertMapping.Item2;
            Vector3 vertexWorldPos = trans.TransformPoint(vertex);
            Vector3 vertexScreenPos = sceneCamera.WorldToScreenPoint(vertexWorldPos);

            if (vertexScreenPos.z <= 0) continue; // Ignore vertices behind the camera

            float distance = Vector2.Distance(new Vector2(vertexScreenPos.x, vertexScreenPos.y), mousePos);
            if (distance <= radius && (paintOccluded || !VertexIsOccluded(vertexWorldPos)))
            {
                if (paintSelectedMeshOnly && !IsActiveSelection(trans)) continue;
                float weight = GetPaintWeight(distance);
                var vertexPaintable = trans.GetComponent<VertexPaintable>();
                if (vertexPaintable == null) continue;
                var paintOp = new VertexPaintOperation(vertexPaintable, vertex, weight);
                //var paintOp = new VertexPaintOperation(paintable, vertex, weight);
                paintOperations.Add(paintOp);
            }
        }
        return paintOperations;
    }

    //Helper for GetAffectedVertices.
    private static bool VertexIsOccluded(Vector3 position)
    {
        var sceneCamera = SceneView.lastActiveSceneView.camera;
        Vector3 cameraPosition = sceneCamera.transform.position;
        Vector3 directionToPosition = (position - cameraPosition).normalized;
        Ray ray = new Ray(cameraPosition, directionToPosition);
        float distanceToTarget = Vector3.Distance(cameraPosition, position);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, distanceToTarget))
        {
            float distanceToHit = Vector3.Distance(cameraPosition, hitInfo.point);
            return distanceToHit < distanceToTarget - 0.01f;
        }
        return false;
    }

    //---------------------------------------------------------------------------------------------------------------------------------
    //Applies Photoshop brush paint logic to the vertex, and returns the resulting color it applied.
    //The brush color's alpha is completely disregarded. Only brush opacity is relevant.
    //However, the resultant opacity is still saved as color alpha.
    //---------------------------------------------------------------------------------------------------------------------------------
    private Color PaintOver(VertexPaintOperation op)
    {
        var oldColor = op.paintable.GetRawVertexColor(op.vertPosition);
        float oldAlpha = oldColor.a;
        float weight = brushOpacity * op.weight;
        var newColor = brushColor * weight + oldColor * oldAlpha * (1 - weight);
        newColor.a = weight + oldAlpha * (1 - weight);
        op.paintable.SetVertexColor(op.vertPosition, newColor);
        return newColor;
    }

    //---------------------------------------------------------------------------------------------------------------------------------
    //Erases from the vertex color alpha, then returns the resulting color it applied.
    //Brush opacity determines the strength of the effect.
    //---------------------------------------------------------------------------------------------------------------------------------
    private Color Erase(VertexPaintOperation op)
    {

        var newColor = op.paintable.GetRawVertexColor(op.vertPosition);
        float weight = brushOpacity * op.weight;
        newColor.a = Mathf.Clamp(newColor.a - weight, 0, 1);
        op.paintable.SetVertexColor(op.vertPosition, newColor);
        return newColor;
    }

    //Helper for paintover/erase.
    private float GetPaintWeight(float distFromBrushCenter)
    {
        float normalizedDistance = distFromBrushCenter / radius;

        float weight;
        if (brushHardness >= 1.0f) weight = 1.0f;
        else if (brushHardness <= 0.0f) weight = 1.0f - normalizedDistance;
        else
        {
            float falloffExponent = Mathf.Lerp(1.0f, 3.0f, brushHardness);
            weight = Mathf.Pow(1.0f - normalizedDistance, falloffExponent);
        }
        return weight;
    }
 
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Brush controls.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void BrushControlHotkeys(Event currentEvent)
    {
        if (currentEvent == null) return;
        if (currentEvent.type == EventType.KeyDown)
        {
            if (currentEvent.keyCode == KeyCode.Equals)
            {
                AlterBrushSize(sizeAdjIncrement);
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.keyCode == KeyCode.Minus)
            {
                AlterBrushSize(-sizeAdjIncrement);
                currentEvent.Use();
                Repaint();
            }
        }
    }

    private void AlterBrushSize(float amount) { radius = Mathf.Clamp(radius + amount, minBrushSize, maxBrushSize); }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Palette color GUI.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void DrawPaletteGUI()
    {
        if (activeLayer == null) return;
        serializedObject = new SerializedObject(activeLayer);
        colorListProp = serializedObject.FindProperty("_palette");
        serializedObject.Update();

        EditorGUILayout.Space(10);
        var palette = activeLayer.GetAllColorsInPalette();
        if (palette.Count == 0)
        {
            EditorGUILayout.LabelField("Press + on recent color to add to palette.");
            return;
        }

        var reorderableList = new ReorderableList(serializedObject, colorListProp, true, true, true, true);
        reorderableList.drawElementCallback = DrawPaletteEntry;
        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Layer palette");
        };
        reorderableList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPaletteEntry(Rect rect, int index, bool isActive, bool isFocused)
    {
        var colorProperty = colorListProp.GetArrayElementAtIndex(index);
        var width = rect.width - 60;
        var height = EditorGUIUtility.singleLineHeight;
        //EditorGUI.PropertyField(new Rect(rect.x, rect.y, width, height), colorProperty, GUIContent.none);
        EditorGUI.ColorField(new Rect(rect.x, rect.y + 2, width, height), GUIContent.none, colorProperty.colorValue, false, false, false);
        if (GUI.Button(new Rect(rect.x + width + 5, rect.y, 50, rect.height), "Use"))
        {
            brushColor = colorProperty.colorValue;
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Recent color GUI.
    //Recent color is just a recording of the colors you have painted with, with the option to save them into your layer palette.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void DrawLayerGUI()
    {
        if (recentColors == null) return;
        if (recentColors.Length == 0) return;
        EditorGUILayout.LabelField("Recent colors");
        for (int i = 0; i < recentColors.Length; i++) DrawRecentColorGUI(i);
    }

    private void DrawRecentColorGUI(int index)
    {
        PrepRecentColors();
        float iconSize = EditorGUIUtility.singleLineHeight;
        float iconWidth = 50;
        Color color = recentColors.GetAt(index);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ColorField(new GUIContent(""), color, showEyedropper: false, showAlpha: false, hdr: false);
        if (GUILayout.Button("Store", GUILayout.Width(iconWidth), GUILayout.Height(iconSize)))
        {
            activeLayer.AddToPalette(recentColors.GetAt(index));
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void UpdateRecentColors()
    {
        PrepRecentColors();
        var top = recentColors.Peek();
        if (brushColor == top) return;
        recentColors.Push(brushColor);
    }

    private void PrepRecentColors()
    {
        if (recentColors == null) recentColors = new Ark.Sys.Stack<Color>(colorStackCapacity);
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Layer scope handling.
    //The layer scope is a cached collection of all the mesh vertices in the layer, and the transform that owns them.
    //When you do free-paint, it uses this collection of vertices to determine what it can paint.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void SaveLayerScope()
    {
        if (activeLayer == null) return;
        activeLayer.SetLayerMeshes(); //Lazy initialization ensures it's up to date every time.
        var layerMeshes = activeLayer.GetLayerMeshes();
        var verts = new List<Tuple<Transform, Vector3>>();
        foreach (MeshFilter m in layerMeshes)
        {
            foreach (Vector3 vertex in m.sharedMesh.vertices)
            {
                verts.Add(new Tuple<Transform, Vector3>(m.transform, vertex));
            }
        }
        layerScope = verts;
    }

    private List<Tuple<Transform, Vector3>> GetLayerVertexScope()
    {
        if (activeLayer == null) return new List<Tuple<Transform, Vector3>>();
        if (layerScope == null) SaveLayerScope();
        return layerScope;
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Display options.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void ToggleShowTexturesForAll(bool enable)
    {
        showTextures = enable;
        var allRenderers = Ark.Object.GetAllComponentsOfType<MeshRenderer>();
        foreach (MeshRenderer mr in allRenderers)
        {
            for (int i = 0; i < mr.sharedMaterials.Length; i++)
            {
                var block = Ark.Material.GetPropertyBlock(mr);
                block.SetFloat(Ark.Material.showTexturePropName, enable ? 1 : 0);
                mr.SetPropertyBlock(block);
            }
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Misc. GUI helpers.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private float GetHeight()
    {
        int numLines = 8;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float height = numLines * lineHeight;
        if (activeLayer != null) height += (activeLayer.GetAllColorsInPalette().Count * lineHeight);
        if (recentColors != null) height += (recentColors.Length * lineHeight);
        return height;
    }

    private void UpdateLabel()
    {
        var labelStyle = new GUIStyle(EditorStyles.label);
        labelColor = isPainting ? Color.grey : labelStyle.normal.textColor;
        Repaint();
    }

    private void DrawCircleOutline(Vector2 center, float radius, Color color)
    {
        Handles.color = color;
        Handles.DrawWireDisc(center, Vector3.forward, radius, thickness: 2f);
    }

    private bool IsActiveSelection(Transform trans)
    {
        return Selection.activeGameObject != null && Selection.activeGameObject.transform == trans;
    }
}*/

public struct VertexPaintOperation
{
    public VertexPaintable paintable;
    public Vector3 vertPosition;
    public float weight;

    public VertexPaintOperation(VertexPaintable Paintable, Vector3 VertPosition, float Weight)
    {
        paintable = Paintable;
        vertPosition = VertPosition;
        weight = Weight;
    }
}

public struct VertexPaintKey
{
    public Transform transform;
    public Vector3Int position;

    public VertexPaintKey(Transform Transform, Vector3Int Position)
    {
        transform = Transform;
        position = Position;
    }

    public bool Equals(VertexPaintKey other)
    {
        bool equals = transform == other.transform && position == other.position;
        return equals;
    }

    public override bool Equals(object obj)
    {
        return obj is VertexPaintKey key && Equals(key);
    }

    public override int GetHashCode()
    {
        return (transform.GetHashCode() * 397) ^ position.GetHashCode();
    }
}