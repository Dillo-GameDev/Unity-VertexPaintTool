## VertexPaintable
VertexPaintable is a component that must be assigned to every mesh object you intend to paint. It stores the color map for the object's mesh vertices as a dictionary. This is automatically serialized via the ISerializationCallbackReceiver interface, which simply converts the dictionary to and from a list of Key-Value pairs. It also stores a default color for mesh vertices that have not been painted.

The stored ("raw") color in the VertexPaintable map is not necessarily the final color that will be used for vertex colors. The final color is determined by interpolating between the raw color and the "default" color, based on the raw color's alpha. Also, the final color's alpha will always be 1. Thus, with a default color of black and a raw color of (1,1,1,0.5), the final color will be grey. If the raw color's alpha is 1, then the raw color will be used completely for the final color. Think of it like having two Photoshop layers, with the raw color "layer" on top of the default color "layer".

A vertex's stored position is its *local position relative to the mesh origin*, converted to an int to remove floating point imprecision. This is considered its "canonical" position. VertexPaintable.GetCanonicalPosition() and VertexPaintable.ConvertCanonicalPosition() can be used to transform a vertex position between its raw (float) or canonical (int) value. Since these positions are used as key values in the color map dictionary, this means vertices that share a position only get one color mapping between them, and must all have the same color. This is good, since having disjoint vertex colors at the same vertex position produces incorrect behavior.

If desired, you can enable VertexPaintableAutoAttach, which automatically adds VertexPaintable to every new mesh object placed in scene.

## Methods
- public void SetRawVertexColor(Vector3 position, Color color)
	- Takes a vertex position, converts it to its canonical integer position, and updates the color map with that value.
	- This is the main way that clients should interact with the VertexPaintable.
	- This does not apply the vertex colors, so you will not see any visual updates just by calling this method alone.
   
- public Color GetRawVertexColor(Vector3 position)
	- Returns the color stored in the color map for the vertex position, or the empty color (0,0,0,0) if it does not exist.
	- Remember that this is not necessarily the same as the color that will be set on the MaterialPropertyBlock.
   
- public void ApplyVertexColors()
	- Uses the stored color map to construct a Vector4[] of colors indexed by vertex, and injects it via a MaterialPropertyBlock.
	- The array has a fixed size of 1023, the maximum array size Unity allows. Vertices with an index higher than this will be ignored. Sorry! :(
	- This function essentially "commits" your changes to the object, so you should see visual changes after calling it.
	- ApplyVertexColors is called automatically in OnEnable() (for game runtime) and OnValidate() (for editor use).
   
- public void SetDefaultColor(Color color)
	- Sets the default color for this object. The final color for a given vertex will blend between the default color and the GetRawVertexColor() for that vertex, based on the alpha of the GetRawVertexColor().
	- Generally, you will want default colors to be consistent across groups. The VertexPaintLayer allows you to set the default color for every VertexPaintable it contains at once, which makes this easy.

## Inspector options
- Refresh: Reapplies vertex colors and resets the MeshCollider component (by removing and re-adding it). Refresh is useful if a mesh has been re-imported, and attempts to handle the common points of breakage from reimport.
- Clear vertex colors: 
- Reapply vertex colors:
- Toggle vertex output
