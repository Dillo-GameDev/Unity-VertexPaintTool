## VertexBrushWindow
VertexBrushWindow is the main UI interface for assigning vertex color map values to the VertexPaintable. While it's open, holding Shift will convert the cursor to circular brush. Clicking (and/or dragging) while holding Shift will paint color values to the vertices within the brush radius. Holding Shift+Alt will erase from those values instead.

When you click to paint, the general order of operations that ensues is
- VertexBrushWindow.StartStroke(): Begins a new Undo grouping and puts the window in "isPainting" mode. Occurs on mouse down.
- VertexBrushWindow.Paint(): Executes a list of VertexPaintOperations to sets the vertex colors for vertices under the brush.
	- VertexBrushWindow.GetAffectedVertices(): Used by Paint() to determine which verts to affect and produce a list of VertexPaintOperations to execute. See VertexPaintOperation section for more detail.
	- VertexPaintable.SetRawVertexColor(): Uses the vertex & color values from the VertexPaintOperation to update the color map. Called once for each affected vertex in a Paint() call.
	- VertexPaintable.ApplyVertexColors(): Uses the newly updated color map to set the _VertexColorArray in the MaterialPropertyBlock for this object.
- ...Repeat Paint() periodically as long as you remain in "isPainting" mode - i.e., as long as the mouse button is held...
- VertexBrushWindow.EndStroke(): Close the Undo grouping and toggle "isPainting" mode off. Occurs on mouse up.

## Methods
I won't go over every piece of UI boilerplate in here, but I do want to cover the most complex functions.
- private void Paint(Vector2 mousePosition, bool erase)
	- This function produces a set of VertexPaintOperations based on your mouse position and the brush radius, and then executes them.
	- If you click once, Paint is called once.
	- If you click-drag, Paint is called repeatedly once you move a sufficient distance from the last position that Paint was called.
	- In the case of click-drag, Undo will undo all Paint operations that occurred while the mouse was held.
	- StartStroke() (on mouse down) and EndStroke() (on mouse up) are what determine the grouping of operations for Undo. StartStroke() starts a new Undo grouping. EndStroke() collapses the group.
   
- private List<VertexPaintOperation> GetAffectedVertices(Vector2 mousePos)
	- Most of the code here attempts to solve the problem "Which vertices are visually within the radius of my brush cursor in the Scene View"? There may be better solutions to this problem, but this is what I came up with. :p
	- It uses the Layer Scope of the current VertexPaintLayer to filter down to which vertices it should bother testing.
	- For each vertex, if it passes all checks - within radius, not occluded, etc. - we generate a new VertexPaintOperation.
	- The VertexPaintOperation is constructed with a weight determined by GetPaintWeight(). Weight factors in distance from brush center and brush hardness, in a manner similar to Photoshop.
   
- private void PaintOver(VertexPaintOperation op)
	- This is the function called by Paint() if you are not erasing. It's called once per VertexPaintOperation (VPO), so Paint() may call it numerous times.
	- First, we get the existing raw vertex color from the VertexPaintable at the vertex of the VPO.
	- We deduce a new color based on the brush color, the VPO's weight, and the existing color.
	- The formula for deducing the new color is similar to Photoshop brush math. Bear in mind that it's not strictly additive. A 50% opacity brush stroke painted over an existing 50% opacity color will yield a 75% opacity color, not 100%.
	- Lastly, we tell the VertexPaintable to set the raw vertex color to our new color.
   
- private void Erase(VertexPaintOperation op)
	- This is the function called by Paint() if you are erasing. It's called once per VertexPaintOperation (VPO), so Paint() may call it numerous times.
	- First, we get the existing raw vertex color from the VertexPaintable at the vertex of the VPO.
	- We deduce a new color by subtracting (brushOpacity * weight) from the existing color's alpha.
	- Lastly, we tell the VertexPaintable to set the raw vertex color to our new color.

## VertexPaintOperation
VertexPaintOperations are little data bricks for instructions that are executed during a single call of VertexBrushWindow.Paint(). They store
- A VertexPaintable to affect
- A Vector3 position, which is the vertex that will be painted
- A weight, which will combine with the brush color and opacity to determine the resultant color.

The only functions that care about VertexPaintOperations are

- VertexBrushWindow.Paint()
- VertexBrushWindow.GetAffectedVertices()
- VertexBrushWindow.PaintOver()
- VertexBrushWindow.Erase()

These functions know what to do with the data contained within a VertexPaintOperation.
