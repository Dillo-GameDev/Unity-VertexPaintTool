## Introduction
This tool was written to address my own specific needs in development. Thus, there are many limitations that the typical user will probably find too restrictive. I hope that you find it valuable nonetheless, but in its current state, it will likely be more useful as a foundation for your own tools, rather than a plug-and-play solution. So I will try to explain the main points of its implementation clearly, so that you can easily re-fit them to your own needs. :)

## Purpose
Vertex colors are a powerful tool commonly used as a lighting hack in retro PSX-era games. Vertex painting tools of various forms already exist in most 3D software, and within Unity. However, none of them seemed amenable to a modern modular workflow, because the vertex colors were always painted on the source mesh. Thus, if I had a row of three stone arches all using the same mesh asset, there was no clear way to do custom vertex-painting on one arch without affecting the other three. Band-aid solutions, like exporting your modular construction to Blender and merging it to do painting, were much too cumbersome in a true development setting. Therefore, if I could develop a tool to paint lighting directly into scenes via vertex colors, while still preserving the benefits of modular scene construction, much work could be saved.

I used vertex colors in the context of implementing PSX-style lighting. However, there are many possible applications for vertex colors, so you may find it useful for other things as well.

## How it works: High level
At its core, what this tool does is:
- Save a mapping of canonical vertex positions to colors in a component
- Transforms that mapping into an array of colors indexed by vertex
- Assigns the color array to a MaterialPropertyBlock, which can be accessed by the shader.

## Classes
- [VertexPaintable component](VertexPaintable.md)
- [VertexPaintLayer component](VertexPaintLayer.md)
- [VertexBrushWindow editor window](VertexBrushWindow.md)
