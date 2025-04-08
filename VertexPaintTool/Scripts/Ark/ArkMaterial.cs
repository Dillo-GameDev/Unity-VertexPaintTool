using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ark
{
    public static class Material
    {
        public static string mainTexPropName = "_MainTex";
        public static string lightingScalePropName = "_LightingScale";
        public static string showTexturePropName = "_ShowTexture";
        public static string showVertexColorPropName = "_ShowVertColors";       
        public static string vertexColorArrayPropName = "_VertexColorArray";

        public static bool HasProperty(UnityEngine.Material mat, string propName)
        {
            if (Core.LogIfError(() => mat == null, "Trying to get properties from a material but the material is null!")) return false;
            return mat.HasProperty(propName);
        }

        public static void SetTexture(UnityEngine.Material mat, string propName, Texture texture)
        {
            if (Core.LogIfError(() => mat == null, "Trying to get properties from a material but the material is null!")) return;
            if (Core.LogIfError(() => !HasProperty(mat, propName), "Trying to set texture property " + propName + " on a material but the material does not have that property!")) return;
            mat.SetTexture(propName, texture);
        }

        public static Texture GetMainTexture(UnityEngine.Material mat)
        {
            if (Core.LogIfError(() => mat == null, "Trying to get texture from a material but the material is null!")) return null;
            return mat.GetTexture(mainTexPropName);
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //Property block methods.
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static MaterialPropertyBlock CreatePropertyBlock(string propName, Vector4[] vectorArray)
        {
            var block = new MaterialPropertyBlock();
            block.SetVectorArray(propName, vectorArray);
            return block;
        }

        public static void SetPropertyBlock(MeshRenderer target, MaterialPropertyBlock block)
        {
            if (Core.LogIfError(() => target == null, "Trying set property block on a mesh renderer, but the target is is null!")) return;
            if (Core.LogIfError(() => block == null, "Trying set property block on a mesh renderer, but the property block is is null!")) return;
            target.SetPropertyBlock(block);
        }

        public static void ClearPropertyBlock(MeshRenderer target)
        {
            if (Core.LogIfError(() => target == null, "Trying clear property block on a mesh renderer, but the target is is null!")) return;
            target.SetPropertyBlock(new MaterialPropertyBlock());
        }

        public static MaterialPropertyBlock GetPropertyBlock(MeshRenderer target)
        {
            if (Core.LogIfError(() => target == null, "Trying get property block on a mesh renderer, but the target is is null!")) return new MaterialPropertyBlock();
            var block = new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            return block;
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //Specific property block setters. This will be the general interface.
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static void SetPropertyBlockVectorArray(MeshRenderer target, string propName, Vector4[] vectorArray)
        {
            if (Core.LogIfError(() => target == null, "Trying set property block on a mesh renderer, but the target is is null!")) return;
            var block = GetPropertyBlock(target);

            block.SetVectorArray(propName, vectorArray);
            SetPropertyBlock(target, block);
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //I don't think this method works. The index-based material property block methods don't seem to actually access the right property block, and get empty dud values.
        //It would be good to have this working eventually, but let's quarantine it for now.
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private static List<MaterialPropertyBlock> GetAllPropertyBlocks(MeshRenderer target)
        {
            if (Core.LogIfError(() => target == null, "Trying get property block on a mesh renderer, but the target is is null!")) return new List<MaterialPropertyBlock>();
            var allBlocks = new List<MaterialPropertyBlock>();
            var sharedMats = target.sharedMaterials;
            for (int i = 0; i < sharedMats.Length; i++)
            {
                var block = new MaterialPropertyBlock();
                target.GetPropertyBlock(block, i);
                allBlocks.Add(block);
            }
            return allBlocks;
        }
    }
}