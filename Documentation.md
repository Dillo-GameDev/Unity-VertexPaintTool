VERTEX PAINT DOCUMENTATION

## Introduction
This tool was written to address my own specific needs in development. Thus, there are many limitations that the typical user will probably find too restrictive. I hope that you find it valuable nonetheless, but in its current state, it will likely be more useful as a foundation for your own tools, rather than a plug-and-play solution. So I will try to explain the main points of its implementation clearly, so that you can easily re-fit them to your own needs. :)

## Purpose
Vertex colors are a powerful tool commonly used as a lighting hack in retro PSX-era games. Vertex painting tools of various forms already exist in most 3D software, and within Unity. However, none of them seemed amenable to a modern modular workflow, because the vertex colors were always painted on the source mesh. Thus, if I had a row of three stone arches all using the same mesh asset, there was no clear way to do custom vertex-painting on one arch without affecting the other three. Band-aid solutions, like exporting your modular construction to Blender and merging it to do painting, were much too cumbersome in a true development setting. Therefore, if I could develop a tool to paint lighting directly into scenes via vertex colors, while still preserving the benefits of modular scene construction, much work could be saved.

I used vertex colors in the context of implementing PSX-style lighting. However, there are many possible applications for vertex colors, so you may find it useful for other things as well.

##How it works: High level
At its core, what this tool does is:
	* Save a mapping of canonical vertex positions to colors in a component
	* Transforms that mapping into an array of colors indexed by vertex
	* Assigns the color array to a MaterialPropertyBlock, which can be accessed by the shader.

##Class API

##VertexPaintable
VertexPaintable is a component that must be assigned to every mesh object you intend to paint. It stores the color map for the object's mesh vertices as a dictionary. This is automatically serialized via the ISerializationCallbackReceiver interface, which simply converts the dictionary to and from a list of Key-Value pairs. It also stores a default color for mesh vertices that have not been painted.

The stored ("raw") color in the VertexPaintable map is not necessarily the final color that will be used for vertex colors. The final color is determined by interpolating between the raw color and the "default" color, based on the raw color's alpha. Also, the final color's alpha will always be 1. Thus, with a default color of black and a raw color of (1,1,1,0.5), the final color will be grey. If the raw color's alpha is 1, then the raw color will be used completely for the final color. Think of it like having two Photoshop layers, with the raw color "layer" on top of the default color "layer".

A vertex's stored position is its *local position relative to the mesh origin*, converted to an int to remove floating point imprecision. This is considered its "canonical" position. VertexPaintable.GetCanonicalPosition() and VertexPaintable.ConvertCanonicalPosition() can be used to transform a vertex position between its raw (float) or canonical (int) value. Since these positions are used as key values in the color map dictionary, this means vertices that share a position only get one color mapping between them, and must all have the same color. This is good, since having disjoint vertex colors at the same vertex position produces incorrect behavior.

If desired, you can enable VertexPaintableAutoAttach, which automatically adds VertexPaintable to every new mesh object placed in scene.

* Methods
	* SetRawVertexColor(Vector3 position, Color color)
		* Takes a vertex position, converts it to its canonical integer position, and updates the color map with that value.
		* This is the main way that clients should interact with the VertexPaintable.
		* This does not apply the vertex colors, so you will not see any visual updates just by calling this method alone.
	* GetRawVertexColor(Vector3Int canonicalPos)
	* GetRawVertexColor(Vector3 position
		* Returns the color stored in the color map for the vertex position, or the empty color (0,0,0,0) if it does not exist.
		* Remember that this is not necessarily the same as the color that will be set on the MaterialPropertyBlock.
	* ApplyVertexColors()
		* Uses the stored color map to construct a Vector4[] of colors indexed by vertex, and injects it via a MaterialPropertyBlock.
		* The array has a fixed size of 1023, the maximum array size Unity allows. Vertices with an index higher than this will be ignored. Sorry! :(
		* This function essentially "commits" your changes to the object, so you should see visual changes after calling it.
		* ApplyVertexColors is called automatically in OnEnable() (for game runtime) and OnValidate() (for editor use).
	* SetDefaultColor(Color color)
		* Sets the default color for this object. The final color for a given vertex will blend between the default color and the GetRawVertexColor() for that vertex, based on the alpha of the GetRawVertexColor().
		* Generally, you will want default colors to be consistent across groups. The VertexPaintLayer allows you to set the default color for every VertexPaintable it contains at once, which makes this easy.
		
* Inspector options
	* Refresh: Reapplies vertex colors and resets the MeshCollider component (by removing and re-adding it). Refresh is useful if a mesh has been re-imported, and attempts to handle the common points of breakage from reimport.
	* Clear vertex colors: 
	* Reapply vertex colors:
	* Toggle vertex output
	
## VertexPaintLayer
VertexPaintLayer is a component for parent objects that group collections of VertexPaintables together. It is primarily used for organization and consistency, allowing you to enforce consistent ambient colors and palettes on VertexPaintables across the whole layer. However, it also has an important role in optimization. By breaking your scene into subgroups of VertexPaintables, we ensure the brush tool never has to care about too many at once.

The VertexPaint Layer tracks its contents in a cached list of LayerScopeItem, which is called the LayerScope. LayerScopeItem is simply a small data block struct that says "Here's a VertexPaintable, and here are all the vertices it cares about." When you assign the VertexPaintLayer in the Vertex Brush window, it automatically populates the LayerScope, and the Vertex Brush window can use that for its operations. However, it is theoretically possible for this list to get out of sync with the actual objects in the scene (for instance, by adding or deleting a VertexPaintable mesh while the brush window is open). To fix this, you can click "Refresh Layer Scope" on the Vertex Brush window.

VertexPaintLayers are required in order to use the Vertex Brush, so they should be integrated into your workflow. Use them like you would any other hierarchical organization tool. You may choose to separate things into a new VertexPaintLayer if, for instance, the lighting conditions in a room suggest a different ambient color. You might also choose to separate them for ease of painting, as the Vertex Brush will never paint outside of the selected layer. In general, a good guideline would be one VertexPaintLayer for each enclosed room.

VertexPaintLayers can also save palettes of colors. Check the Vertex Brush window documentation for more details.

* Inspector options
	* Set default color to ambient: Calls SetDefaultColor on all VertexPaintables in the layer, using the layer's ambient color.
	* Clear default color: Clears the default color on all VertexPaintables in the layer.
	* Refresh all: Reapplies vertex colors and resets the MeshCollider component on all VertexPaintables in the layer. This is useful if a reimport process has caused temporary breakage.
	* Reapply vertex colors for all:
	* Clear vertex colors for all:
	
## VertexBrushWindow
VertexBrushWindow is the main UI interface for assigning vertex color map values to the VertexPaintable. While it's open, holding Shift will convert the cursor to circular brush. Clicking (and/or dragging) while holding Shift will paint color values to the vertices within the brush radius. Holding Shift+Alt will erase from those values instead.

When you click to paint, the general order of operations that ensues is
	* VertexBrushWindow.StartStroke(): Begins a new Undo grouping and puts the window in "isPainting" mode. Occurs on mouse down.
	* VertexBrushWindow.Paint(): Executes a list of VertexPaintOperations to sets the vertex colors for vertices under the brush.
		* VertexBrushWindow.GetAffectedVertices(): Used by Paint() to determine which verts to affect and produce a list of VertexPaintOperations to execute. See VertexPaintOperation section for more detail.
		* VertexPaintable.SetRawVertexColor(): Uses the vertex & color values from the VertexPaintOperation to update the color map. Called once for each affected vertex in a Paint() call.
		* VertexPaintable.ApplyVertexColors(): Uses the newly updated color map to set the _VertexColorArray in the MaterialPropertyBlock for this object.
	* ...Repeat Paint() periodically as long as you remain in "isPainting" mode - i.e., as long as the mouse button is held...
	* VertexBrushWindow.EndStroke(): Close the Undo grouping and toggle "isPainting" mode off. Occurs on mouse up.

* Methods
I won't go over every piece of UI boilerplate in here, but I do want to cover the most complex functions.
	* private void Paint(Vector2 mousePosition, bool erase)
		* This function produces a set of VertexPaintOperations based on your mouse position and the brush radius, and then executes them.
		* If you click once, Paint is called once.
		* If you click-drag, Paint is called repeatedly once you move a sufficient distance from the last position that Paint was called.
		* In the case of click-drag, Undo will undo all Paint operations that occurred while the mouse was held.
		* StartStroke() (on mouse down) and EndStroke() (on mouse up) are what determine the grouping of operations for Undo. StartStroke() starts a new Undo grouping. EndStroke() collapses the group.
	* private List<VertexPaintOperation> GetAffectedVertices(Vector2 mousePos)
		* Most of the code here attempts to solve the problem "Which vertices are visually within the radius of my brush cursor in the Scene View"? There may be better solutions to this problem, but this is what I came up with. :p
		* It uses the Layer Scope of the current VertexPaintLayer to filter down to which vertices it should bother testing.
		* For each vertex, if it passes all checks - within radius, not occluded, etc. - we generate a new VertexPaintOperation.
		* The VertexPaintOperation is constructed with a weight determined by GetPaintWeight(). Weight factors in distance from brush center and brush hardness, in a manner similar to Photoshop.
	* private void PaintOver(VertexPaintOperation op)
		* This is the function called by Paint() if you are not erasing. It's called once per VertexPaintOperation (VPO), so Paint() may call it numerous times.
		* First, we get the existing raw vertex color from the VertexPaintable at the vertex of the VPO.
		* We deduce a new color based on the brush color, the VPO's weight, and the existing color.
			* The formula for deducing the new color is similar to Photoshop brush math. Bear in mind that it's not strictly additive. A 50% opacity brush stroke painted over an existing 50% opacity color will yield a 75% opacity color, not 100%.
		* Lastly, we tell the VertexPaintable to set the raw vertex color to our new color.
	* private void Erase(VertexPaintOperation op)
		* This is the function called by Paint() if you are erasing. It's called once per VertexPaintOperation (VPO), so Paint() may call it numerous times.
		* First, we get the existing raw vertex color from the VertexPaintable at the vertex of the VPO.
		* We deduce a new color by subtracting (brushOpacity * weight) from the existing color's alpha.
		* Lastly, we tell the VertexPaintable to set the raw vertex color to our new color.

## VertexPaintOperation
VertexPaintOperations are data bricks for instructions that are executed during a single call of VertexBrushWindow.Paint(). They store
	* A VertexPaintable to affect
	* A Vector3 position, which is the vertex that will be painted
	* A weight, which will combine with the brush color and opacity to determine the resultant color.

The only functions that care about VertexPaintOperations are
	* VertexBrushWindow.Paint()
	* VertexBrushWindow.GetAffectedVertices()
	* VertexBrushWindow.PaintOver()
	* VertexBrushWindow.Erase()
These functions know what to do with the data contained within a VertexPaintOperation.