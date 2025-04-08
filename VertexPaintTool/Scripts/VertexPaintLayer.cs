using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using Ark;

public class VertexPaintLayer : MonoBehaviour
{
    [SerializeField]
    private Color _ambientColor = Color.white;
    [SerializeField]
    private List<Color> _palette = new List<Color>();

    [HideInInspector]
    [SerializeField]
    private List<LayerScopeItem> _layerScope = new List<LayerScopeItem>();

    private Color defaultColor = new Color(0, 0, 0, 0);

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------
    //METHODS.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void SetDefaultColorForAll(Color color)
    {
        var allPaintables = GetAllPaintable();
        foreach (VertexPaintable vp in allPaintables)
        {
            vp.SetDefaultColor(color);
            vp.ApplyVertexColors();
        }
    }
    public void ClearVertexColorsForAll()
    {
        var allPaintables = GetAllPaintable();
        foreach (VertexPaintable vp in allPaintables) vp.ClearVertexColors();
    }
    public void SetAmbientColorForAll() { SetDefaultColorForAll(_ambientColor); }
    public void ClearDefaultColorForAll() { SetDefaultColorForAll(defaultColor); }
    public void ReapplyVertexColorsForAll()
    {
        foreach (VertexPaintable vp in Ark.Obj.GetComponentsInChildren<VertexPaintable>(gameObject))
        {
            vp.ApplyVertexColors();
        }
    }
    public void RefreshAll()
    {
        foreach (VertexPaintable vp in Ark.Obj.GetComponentsInChildren<VertexPaintable>(gameObject))
        {
            vp.Refresh();
        }
    }

    private VertexPaintable[] GetAllPaintable() { return Ark.Obj.GetComponentsInChildren<VertexPaintable>(gameObject); }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Palette methods.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------
    public void AddToPalette(Color color)
    {
        if (_palette == null) _palette = new List<Color>();
        if (!PaletteContains(color)) _palette.Add(color);
    }

    public bool PaletteContains(Color color)
    {
        if (_palette == null) _palette = new List<Color>();
        foreach (Color other in _palette)
        {
            if (other == color) return true; //==operator for color is approximate and accounts for floatingpoint error
        }
        return false;
    }

    public List<Color> GetAllColorsInPalette() { return new List<Color>(_palette); }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Layer scope methods.
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------
    public void SetLayerScope()
    {
        if (_layerScope == null) _layerScope = new List<LayerScopeItem>();
        var allPaintables = Ark.Obj.GetComponentsInChildren<VertexPaintable>(this.gameObject);
        var allItems = new List<LayerScopeItem>();
        foreach (VertexPaintable paintable in allPaintables)
        {
            if (paintable == null) continue;
            var meshFilter = paintable.GetComponent<MeshFilter>();
            if (Core.LogIfError(() => meshFilter == null, "GameObject " + paintable.gameObject + " has a VertexPaintable component but no MeshFilter! It will not work correctly!")) continue;
            allItems.Add(new LayerScopeItem(paintable, meshFilter, paintable.transform));
        }

        _layerScope = allItems;
    }
    public List<LayerScopeItem> GetLayerScope(){ return new List<LayerScopeItem>(_layerScope); }
}

//--------------------------------------------------------------------------------------------------------------------------------------------------------------
//Vertex paint mappings are used for editor content setup. They store mappings of local vertex positions to colors.
//This is so they can be resilient to re-exports changing the index order.
//--------------------------------------------------------------------------------------------------------------------------------------------------------------
[System.Serializable]
public struct VertexColorMapping
{
    public Vector3Int canonicalVertPos;
    public Color color;

    public VertexColorMapping(Vector3Int CanonicalVertPos, Color Color)
    {
        canonicalVertPos = CanonicalVertPos;
        color = Color;
    }
}

[System.Serializable] 
public struct LayerScopeItem
{
    public VertexPaintable paintable;
    public Vector3[] vertices;
    public Transform trans;

    public LayerScopeItem(VertexPaintable Paintable, MeshFilter MeshFilter, Transform Trans)
    {
        paintable = Paintable;
        vertices = MeshFilter.sharedMesh.vertices;
        trans = Trans;
    }
}