using System;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Graph;
using NyaForge.Rendering;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Owns the preview texture and material for a committed base-color image.</summary>
    sealed class BaseColorSurface : IDisposable
    {
        readonly Texture2D texture;
        readonly Texture2D normalTexture;
        readonly Texture2D metallicGlossTexture;
        Texture2D preview;
        public Material Material { get; }
        public bool HasPreview=>preview!=null;
        public BaseColorSurface(PaintImage image, Material template, MaterialParameters parameters=null)
        {
            texture = image==null ? null : PaintTextureAdapter.Create(image);
            Texture2D decodedNormal = null;
            Texture2D decodedMetallicGloss = null;
            try
            {
                Material=parameters==null ? new Material(template) { color=Color.white } : StandardMaterialAdapter.Create(parameters,true);
                Material.mainTexture=texture!=null ? texture : Texture2D.whiteTexture;
                if (parameters != null && parameters.Textures != null)
                {
                    if (parameters.Textures.Normal != null)
                    {
                        var slot = parameters.Textures.Normal;
                        decodedNormal = DecodeImage(slot, Material.name + " Normal", true);
                        Material.SetTexture("_BumpMap", decodedNormal);
                        Material.SetFloat("_BumpScale", slot.NormalScale);
                        Material.SetFloat("_BumpTexCoord", slot.TexCoord);
                        Material.EnableKeyword("_NORMALMAP");
                    }
                    if (parameters.Textures.MetallicRoughness != null)
                    {
                        var slot = parameters.Textures.MetallicRoughness;
                        decodedMetallicGloss = DecodeMetallicGloss(slot, Material.name + " MetallicGloss");
                        Material.SetTexture("_MetallicGlossMap", decodedMetallicGloss);
                        Material.SetFloat("_MetallicGlossTexCoord", slot.TexCoord);
                        Material.EnableKeyword("_METALLICGLOSSMAP");
                    }
                }
                normalTexture = decodedNormal;
                metallicGlossTexture = decodedMetallicGloss;
            }
            catch { UnityEngine.Object.Destroy(Material);UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(decodedNormal);UnityEngine.Object.Destroy(decodedMetallicGloss); throw; }
        }
        public void ShowPreview(PaintImage image)
        {
            var next=image==null ? null : PaintTextureAdapter.Create(image);
            SetPreview(next);
        }
        internal void SetPreview(Texture2D next)
        {
            Material.mainTexture=next!=null ? next : texture!=null ? texture : Texture2D.whiteTexture;
            if(preview!=null) UnityEngine.Object.Destroy(preview);preview=next;
        }
        public void Dispose() { UnityEngine.Object.Destroy(Material); UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(normalTexture);UnityEngine.Object.Destroy(metallicGlossTexture);if(preview!=null) UnityEngine.Object.Destroy(preview); }

        static Texture2D DecodeImage(MaterialTextureSlot slot, string name, bool linear)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear) { name = name };
            try
            {
                if (!texture.LoadImage(slot.CopyEncodedBytes(), false)) throw new InvalidOperationException("Semantic texture image could not be decoded.");
                ApplySampler(texture, slot.Sampler);
                return texture;
            }
            catch { UnityEngine.Object.Destroy(texture); throw; }
        }

        static Texture2D DecodeMetallicGloss(MaterialTextureSlot slot, string name)
        {
            Texture2D source = null;
            Texture2D output = null;
            bool transferred = false;
            try
            {
                source = DecodeImage(slot, name + " source", true);
                var pixels = source.GetPixels32();
                output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true) { name = name };
                var converted = new Color32[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    var pixel = pixels[i];
                    converted[i] = new Color32(pixel.b, 0, 0, (byte)(255 - pixel.g));
                }
                output.SetPixels32(converted);
                output.Apply(false, false);
                ApplySampler(output, slot.Sampler);
                transferred = true;
                return output;
            }
            finally
            {
                if (source != null) UnityEngine.Object.Destroy(source);
                // Ownership transfers to BaseColorSurface only on a successful return.
                if (!transferred && output != null) UnityEngine.Object.Destroy(output);
            }
        }

        static void ApplySampler(Texture2D texture, MaterialTextureSampler sampler)
        {
            if (sampler == null) return;
            texture.wrapMode = WrapMode(sampler.WrapS);
            texture.filterMode = Filter(sampler.MinFilter, sampler.MagFilter);
        }

        static TextureWrapMode WrapMode(int value)
        {
            switch (value)
            {
                case 33071: return TextureWrapMode.Clamp;
                case 33648: return TextureWrapMode.Mirror;
                default: return TextureWrapMode.Repeat;
            }
        }

        static FilterMode Filter(int minFilter, int magFilter)
        {
            if (magFilter == 9728 || minFilter == 9728 || minFilter == 9984) return FilterMode.Point;
            if (minFilter == 9986 || minFilter == 9987) return FilterMode.Trilinear;
            return FilterMode.Bilinear;
        }
    }
}
