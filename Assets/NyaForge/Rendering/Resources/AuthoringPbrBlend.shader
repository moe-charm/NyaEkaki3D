Shader "NyaForge/AuthoringPbrBlend"
{
    Properties
    {
        _MainTex ("Base color", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal scale", Range(0,8)) = 1
        [HideInInspector] _BumpTexCoord ("Normal UV", Float) = 0
        _MetallicGlossMap ("Metallic roughness", 2D) = "black" {}
        [HideInInspector] _MetallicGlossTexCoord ("Metallic roughness UV", Float) = 0
        _TintLinear ("Linear tint", Vector) = (1,1,1,1)
        _EmissionLinear ("Linear emission", Vector) = (0,0,0,0)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        [HideInInspector] _ColorMask ("Target channels", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        // Preview preserves its composited background alpha; imported materials write RGBA.
        ColorMask [_ColorMask]
        CGPROGRAM
        #pragma target 3.0
        #pragma shader_feature _NORMALMAP
        #pragma shader_feature _METALLICGLOSSMAP
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #include "AuthoringPbr.cginc"
        ENDCG
    }
    Fallback Off
}
