using System;
using NyaForge.Authoring.Graph;
using UnityEngine;
using UnityEngine.Rendering;

namespace NyaForge.Rendering
{
    public static class StandardMaterialAdapter
    {
        public static Shader RequireShader(MaterialParameters parameters)
        {
            if(parameters==null) throw new ArgumentNullException(nameof(parameters));
            if(QualitySettings.activeColorSpace!=ColorSpace.Linear || GraphicsSettings.currentRenderPipeline!=null)
                throw new InvalidOperationException("Standard material preview requires the Linear Built-In render pipeline.");
            bool blend=parameters.AlphaMode==MaterialAlphaMode.Blend;
            var shader=Resources.Load<Shader>(blend ? "AuthoringPbrBlend" : "AuthoringPbrOpaque");
            if(!shader || !shader.isSupported) throw new InvalidOperationException("Authoring PBR shader is unavailable.");
            return shader;
        }
        public static Material Create(MaterialParameters parameters,bool preserveTargetAlpha=false)
        {
            var shader=RequireShader(parameters);bool blend=parameters.AlphaMode==MaterialAlphaMode.Blend;
            var material=new Material(shader) { name="NyaForge owned standard material" };
            try
            {
                var tint=parameters.BaseColor;var emission=parameters.Emission;
                material.SetVector("_TintLinear",new Vector4(tint.X,tint.Y,tint.Z,tint.W));
                material.SetVector("_EmissionLinear",new Vector4(emission.X,emission.Y,emission.Z,0));
                material.SetFloat("_Metallic",parameters.Metallic);material.SetFloat("_Smoothness",1-parameters.Roughness);
                if(blend) material.SetInt("_ColorMask",preserveTargetAlpha ? 7 : 15);
                if(!blend)
                {
                    bool cutout=parameters.AlphaMode==MaterialAlphaMode.Cutout;
                    material.SetFloat("_Cutoff",cutout ? parameters.AlphaCutoff : 0);
                    material.SetOverrideTag("RenderType",cutout ? "TransparentCutout" : "Opaque");
                    material.renderQueue=cutout ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
                }
                return material;
            }
            catch { UnityEngine.Object.Destroy(material);throw; }
        }
    }
}
