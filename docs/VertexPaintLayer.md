## VertexPaintLayer
VertexPaintLayer is a component for parent objects that group collections of VertexPaintables together. It is primarily used for organization and consistency, allowing you to enforce consistent ambient colors and palettes on VertexPaintables across the whole layer. However, it also has an important role in optimization. By breaking your scene into subgroups of VertexPaintables, we ensure the brush tool never has to care about too many at once.

The VertexPaint Layer tracks its contents in a cached list of LayerScopeItem, which is called the LayerScope. LayerScopeItem is simply a small data block struct that says "Here's a VertexPaintable, and here are all the vertices it cares about." When you assign the VertexPaintLayer in the Vertex Brush window, it automatically populates the LayerScope, and the Vertex Brush window can use that for its operations. However, it is theoretically possible for this list to get out of sync with the actual objects in the scene (for instance, by adding or deleting a VertexPaintable mesh while the brush window is open). To fix this, you can click "Refresh Layer Scope" on the Vertex Brush window.

VertexPaintLayers are required in order to use the Vertex Brush, so they should be integrated into your workflow. Use them like you would any other hierarchical organization tool. You may choose to separate things into a new VertexPaintLayer if, for instance, the lighting conditions in a room suggest a different ambient color. You might also choose to separate them for ease of painting, as the Vertex Brush will never paint outside of the selected layer. In general, a good guideline would be one VertexPaintLayer for each enclosed room.

VertexPaintLayers can also save palettes of colors. Check the VertexBrushWindow documentation for more details.

## Inspector options
- Set default color to ambient: Calls SetDefaultColor on all VertexPaintables in the layer, using the layer's ambient color.
- Clear default color: Clears the default color on all VertexPaintables in the layer.
- Refresh all: Reapplies vertex colors and resets the MeshCollider component on all VertexPaintables in the layer. This is useful if a reimport process has caused temporary breakage.
- Reapply vertex colors for all:
- Clear vertex colors for all:
